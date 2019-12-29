using HttpWebcamLiveStream.Devices;
using HttpWebcamLiveStream.Helper;
using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace HttpWebcamLiveStream.Web
{
    public enum OpCode
    {
        Undefined = -1,
        Text = 129,
        Binary = 130
    }

    public class WebSocket
    {
        private IInputStream _inputStream;
        private IOutputStream _outputStream;
        private HttpServerRequest _httpServerRequest;
        private byte[] _alreadySendFrame = null;
        private const uint BUFFER_SIZE = 3024;
        private const int NEW_FRAME_AVAILABLE_CHECK_DURATION_MS = 5;

        //Dependencies
        private Camera _camera;

        public WebSocket(StreamSocket socket,
                         HttpServerRequest httpServerRequest,
                         Camera camera)
        {
            _inputStream = socket.InputStream;
            _outputStream = socket.OutputStream;
            _httpServerRequest = httpServerRequest;
            _camera = camera;
        }

        public async Task Start()
        {
            if (await CheckWebSocketVersionSupport())
            {
                await ReadFrames();
            }
        }

        private async Task<bool> CheckWebSocketVersionSupport()
        {
            var webSocketVersion = new Regex("Sec-WebSocket-Version:(.*)", RegexOptions.IgnoreCase).Match(_httpServerRequest.Request).Groups[1].Value.Trim();
            if (webSocketVersion != "13")
            {
                await WriteUpgradeRequired();

                return false;
            }
            else
            {
                await WriteHandshake();

                return true;
            }
        }

        private async Task WriteUpgradeRequired()
        {
            var response = Encoding.UTF8.GetBytes("HTTP/1.1 426 Upgrade Required" + Environment.NewLine
                + "Connection: Upgrade" + Environment.NewLine
                + "Upgrade: websocket" + Environment.NewLine
                + "Sec-WebSocket-Version: 13" + Environment.NewLine
                + Environment.NewLine);

            await _outputStream.WriteAsync(response.AsBuffer());
            await _outputStream.FlushAsync();
        }

        private async Task WriteHandshake()
        {
            var response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                + "Connection: Upgrade" + Environment.NewLine
                + "Upgrade: websocket" + Environment.NewLine
                + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                    SHA1.Create().ComputeHash(
                        Encoding.UTF8.GetBytes(
                            new Regex("Sec-WebSocket-Key:(.*)", RegexOptions.IgnoreCase).Match(_httpServerRequest.Request).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                        )
                    )
                ) + Environment.NewLine
                + Environment.NewLine);

            await _outputStream.WriteAsync(response.AsBuffer());
            await _outputStream.FlushAsync();
        }

        private async Task ProcessFrame(string frameContent)
        {
            var content = JsonObject.Parse(frameContent);
            var command = content["command"].GetString();

            switch (command)
            {
                case "VideoFrame":

                    // Wait frames available
                    SpinWait.SpinUntil(() => { return _camera != null && _camera.Frame != null; });

                    // Check new frame available
                    while (_alreadySendFrame == _camera.Frame)
                    {
                        await Task.Delay(NEW_FRAME_AVAILABLE_CHECK_DURATION_MS);
                    }

                    _alreadySendFrame = _camera.Frame;

                    var cameraFrame = _alreadySendFrame.ToArray();
                    await WriteFrame(cameraFrame, OpCode.Binary);

                    break;
            }
        }

        private async Task WriteFrame(string data)
        {
            var dataBytes = Encoding.UTF8.GetBytes(data);
            await WriteFrame(dataBytes, OpCode.Text);
        }

        private async Task WriteFrame(byte[] data, OpCode opCode)
        {
            byte[] header = new byte[2];

            if (opCode == OpCode.Text)
            {
                header[0] = 129;
            }
            else if (opCode == OpCode.Binary)
            {
                header[0] = 130;
            }

            if (data.Length <= 125)
            {
                header[1] = (byte)data.Length;
            }
            else if (data.Length >= 126 && data.Length <= 65535)
            {
                header[1] = 126;

                var length = Convert.ToUInt16(data.Length);
                var lengthBytes = BitConverter.GetBytes(length);
                Array.Reverse(lengthBytes, 0, lengthBytes.Length);

                header = header.Concat(lengthBytes).ToArray();
            }
            else
            {
                header[1] = 127;

                var length = Convert.ToUInt64(data.Length);
                var lengthBytes = BitConverter.GetBytes(length);
                Array.Reverse(lengthBytes, 0, lengthBytes.Length);

                header = header.Concat(lengthBytes).ToArray();
            }

            var headerData = header.Concat(data).ToArray();

            await _outputStream.WriteAsync(headerData.AsBuffer());
            await _outputStream.FlushAsync();
        }

        private async Task ReadFrames()
        {
            var data = new byte[BUFFER_SIZE];
            var buffer = data.AsBuffer();
            var frameData = Array.Empty<byte>();

            while (true)
            {
                var readBytesTask = _inputStream.ReadAsync(buffer, BUFFER_SIZE, InputStreamOptions.Partial);
                var timeout = TimeSpan.FromMilliseconds(5000);
                await TaskHelper.WithTimeoutAfterStart(ct => readBytesTask.AsTask(ct), timeout);

                var readBytes = readBytesTask.AsTask().Result;

                var readBytesLength = (int)readBytes.Length;

                if (readBytesLength >= 2)
                {
                    var newData = data.Take(readBytesLength);
                    frameData = frameData.Concat(newData).ToArray();

                    readBytesLength = frameData.Length;

                    var opCode = OpCode.Undefined;

                    if (frameData[0] == 136) //Close frame was send
                    {
                        var closeFrame = new byte[] { 136, 0 };
                        await _outputStream.WriteAsync(closeFrame.AsBuffer());
                        await _outputStream.FlushAsync();
                        return;
                    }
                    else if (frameData[0] == 129)
                    {
                        opCode = OpCode.Text;
                    }
                    else if (frameData[0] == 130)
                    {
                        opCode = OpCode.Binary;
                    }

                    var contentLength = (long)(frameData[1] & 127);

                    var indexFirstMask = 2;

                    if (contentLength == 126)
                    {
                        if (readBytesLength < 4)
                            continue;

                        Array.Reverse(frameData, 2, 2);

                        contentLength = BitConverter.ToInt16(frameData, 2);
                        indexFirstMask = 4;
                    }
                    else if (contentLength == 127)
                    {
                        if (readBytesLength < 10)
                            continue;

                        Array.Reverse(frameData, 2, 8);

                        contentLength = BitConverter.ToInt64(frameData, 2);

                        indexFirstMask = 10;
                    }

                    var maskLength = 4;
                    var indexFirstDataByte = indexFirstMask + maskLength;

                    var frameLength = contentLength + indexFirstDataByte;

                    if (readBytesLength < frameLength) //Is complete frame read?
                        continue;

                    var masks = frameData.Skip(indexFirstMask).Take(maskLength).ToArray();

                    byte[] decoded = new byte[contentLength];

                    for (int i = indexFirstDataByte, j = 0; i < frameLength; i++, j++)
                    {
                        decoded[j] = (byte)(frameData[i] ^ masks.ElementAt(j % 4));
                    }

                    if (frameData.Length > frameLength)
                    {
                        frameData = frameData.Skip(frameData.Length).ToArray();
                    }
                    else
                    {
                        frameData = Array.Empty<byte>();
                    }

                    data = new byte[BUFFER_SIZE];
                    buffer = data.AsBuffer();

                    if (opCode == OpCode.Text)
                    {
                        await ProcessFrame(Encoding.UTF8.GetString(decoded, 0, decoded.Length));
                    }
                }
            }
        }
    }
}