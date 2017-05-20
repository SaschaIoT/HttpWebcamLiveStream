using HttpWebcamLiveStream.Configuration;
using HttpWebcamLiveStream.Devices;
using HttpWebcamLiveStream.Helper;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
            try
            {
                var socket = eventArgs.Socket;

                //Read request
                var request = await ReadRequest(socket);

                //Write Response
                await WriteResponse(request, socket);

                socket.InputStream.Dispose();
                socket.OutputStream.Dispose();
                socket.Dispose();
            }
            catch (Exception) { }
        }

        private async Task<HttpServerRequest> ReadRequest(StreamSocket socket)
        {
            var request = string.Empty;
            var error = false;

            var inputStream = socket.InputStream;

            var data = new byte[BUFFER_SIZE];
            var buffer = data.AsBuffer();

            var startReadRequest = DateTime.Now;
            while (!HttpGetRequestHasUrl(request))
            {
                if (DateTime.Now.Subtract(startReadRequest) >= TimeSpan.FromMilliseconds(5000))
                {
                    error = true;
                    return new HttpServerRequest(null, true);
                }

                var inputStreamReadTask = inputStream.ReadAsync(buffer, BUFFER_SIZE, InputStreamOptions.Partial);
                var timeout = TimeSpan.FromMilliseconds(1000);
                await TaskHelper.WithTimeoutAfterStart(ct => inputStreamReadTask.AsTask(ct), timeout);

                request += Encoding.UTF8.GetString(data, 0, (int)inputStreamReadTask.AsTask().Result.Length);
            }

            return new HttpServerRequest(request, error);
        }

        private async Task WriteResponse(HttpServerRequest request, StreamSocket socket)
        {
            var relativeUrlLower = request.Url.ToLowerInvariant();
            var outputStream = socket.OutputStream;

            //Get javascript files
            if (relativeUrlLower.StartsWith("/javascript"))
            {
                await HttpServerResponse.WriteResponseFile(ToFolderPath(request.Url), HttpContentType.JavaScript, outputStream);
            }
            //Get css style files
            else if (relativeUrlLower.StartsWith("/styles"))
            {
                await HttpServerResponse.WriteResponseFile(ToFolderPath(request.Url), HttpContentType.Css, outputStream);
            }
            //Get video setting
            else if (relativeUrlLower.StartsWith("/videosetting"))
            {
                HttpServerResponse.WriteResponseJson(ConfigurationFile.VideoSetting.Stringify(), outputStream);
            }
            //Get supported video settings
            else if (relativeUrlLower.StartsWith("/supportedvideosettings"))
            {
                HttpServerResponse.WriteResponseJson(ConfigurationFile.VideoSettingsSupported.Stringify(), outputStream);
            }
            //Set video settings
            else if (relativeUrlLower.StartsWith("/savevideosetting"))
            {
                await _camera.Stop();

                var videoSetting = new VideoSetting
                {
                    VideoSubtype = VideoSubtypeHelper.Get(request.Body["VideoSubtype"].GetString()),
                    VideoResolution = (VideoResolution)request.Body["VideoResolution"].GetNumber(),
                    VideoQuality = request.Body["VideoQuality"].GetNumber(),
                    UsedThreads = (int)request.Body["UsedThreads"].GetNumber()
                };

                await ConfigurationFile.Write(videoSetting);
                await _camera.Initialize(videoSetting);
                _camera.Start();

                HttpServerResponse.WriteResponseOk(outputStream);
            }
            //Get current camera frame
            else if (relativeUrlLower.StartsWith("/videoframe"))
            {
                if (_camera.Frame != null)
                {
                    var webSocket = new WebSocket(socket, request, _camera);
                    await webSocket.Start();
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
            var regex = new Regex("GET.*HTTP.*\r\n", RegexOptions.IgnoreCase);
            return regex.IsMatch(httpRequest.ToUpper());
        }

        private string ToFolderPath(string relativeUrl)
        {
            var folderPath = relativeUrl.Replace('/', '\\');
            return folderPath;
        }
    }
}
