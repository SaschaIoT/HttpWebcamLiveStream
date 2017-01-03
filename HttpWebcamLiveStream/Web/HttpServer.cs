using HttpWebcamLiveStream.Devices;
using HttpWebcamLiveStream.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace HttpWebcamLiveStream.Web
{
    public sealed class HttpServer
    {
        private const uint BUFFER_SIZE = 3024;
        private readonly StreamSocketListener _listener;

        //Dependency objects
        private Camera _camera;

        public HttpServer(Camera camera)
        {
            _camera = camera;

            _listener = new StreamSocketListener();
            _listener.ConnectionReceived += ProcessRequest;
            _listener.Control.KeepAlive = false;
            _listener.Control.NoDelay = false;
            _listener.Control.QualityOfService = SocketQualityOfService.LowLatency;
        }
        
        public async void Start()
        {
            await _listener.BindServiceNameAsync(80.ToString());
        }

        private async void ProcessRequest(StreamSocketListener streamSocktetListener, StreamSocketListenerConnectionReceivedEventArgs eventArgs)
        {
            var socket = eventArgs.Socket;

            try
            {
                //Read request
                var relativeUrl = await ReadRequest(socket);

                //Write Response
                await WriteResponse(relativeUrl, socket.OutputStream);

                socket.InputStream.Dispose();
                socket.OutputStream.Dispose();
                socket.Dispose();
            }
            catch (Exception exception)
            {
                try
                {
                    HttpServerResponse.WriteResponseError(exception.Message, socket.OutputStream);
                }
                catch (Exception) { }                
            }
        }

        private async Task<string> ReadRequest(StreamSocket socket)
        {
            var request = string.Empty;
            
            using (var inputStream = socket.InputStream)
            {
                var data = new byte[BUFFER_SIZE];
                var buffer = data.AsBuffer();

                var startReadRequest = DateTime.Now;
                while (!HttpGetRequestHasUrl(request))
                {
                    if (DateTime.Now.Subtract(startReadRequest) >= TimeSpan.FromMilliseconds(5000))
                    {
                        throw new TaskCanceledException("Request timeout.");
                    }

                    var inputStreamReadTask = inputStream.ReadAsync(buffer, BUFFER_SIZE, InputStreamOptions.Partial);
                    var timeout = TimeSpan.FromMilliseconds(1000);
                    await TaskHelper.CancelTaskAfterTimeout(ct => inputStreamReadTask.AsTask(ct), timeout);

                    request += Encoding.UTF8.GetString(data, 0, data.Length);
                }
            }

            var requestMethod = request.Split('\n')[0];
            var requestParts = requestMethod.Split(' ');
            var relativeUrl = requestParts.Length > 1 ? requestParts[1] : string.Empty;

            return relativeUrl;
        }

        private async Task WriteResponse(string relativeUrl, IOutputStream outputStream)
        {
            var relativeUrlLower = relativeUrl.ToLowerInvariant();

            //Get javascript files
            if (relativeUrlLower.StartsWith("/javascript"))
            {
                await HttpServerResponse.WriteResponseFile(ToFolderPath(relativeUrl), HttpContentType.JavaScript, outputStream);
            }
            //Get css style files
            else if (relativeUrlLower.StartsWith("/styles"))
            {
                await HttpServerResponse.WriteResponseFile(ToFolderPath(relativeUrl), HttpContentType.Css, outputStream);
            }
            //Get current camera frame
            else if (relativeUrlLower.StartsWith("/videoframe"))
            {
                if (_camera.Frame != null)
                {
                    HttpServerResponse.WriteResponseFile(_camera.Frame, HttpContentType.Jpeg, outputStream);
                }
                else
                {
                    HttpServerResponse.WriteResponseError("Not camera fram available. Maybe there is an error or camera is not started.", outputStream);
                }
            }
            //Get index.html page
            else
            {
                await HttpServerResponse.WriteResponseFile(@"\Html\Index.html", HttpContentType.Html, outputStream);
            }
        }
        
        private bool HttpGetRequestHasUrl(string httpRequest)
        {
            var regex = new Regex("^.*GET.*HTTP.*\\r\\n.*$", RegexOptions.Multiline);
            return regex.IsMatch(httpRequest.ToUpper());
        }

        private string ToFolderPath(string relativeUrl)
        {
            var folderPath = relativeUrl.Replace('/', '\\');
            return folderPath;
        }
    }
}
