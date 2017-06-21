using HttpWebcamLiveStream.Configuration;
using HttpWebcamLiveStream.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace HttpWebcamLiveStream.Devices
{
    /// <summary>
    /// Camera: ELP 2.8mm wide angle lens 1080p HD USB Camera Module (ELP-USBFHD01M-L28)
    /// </summary>
    public class Camera
    {
        public byte[] Frame { get; set; }
        
        private MediaCapture _mediaCapture;
        private MediaFrameReader _mediaFrameReader;
        
        private BitmapPropertySet _imageQuality;

        private int _threadsCount = 0;
        private int _stoppedThreads = 0;
        private bool _stopThreads = false;

        private volatile Stopwatch _lastFrameAdded = new Stopwatch();
        private volatile object _lastFrameAddedLock = new object();

        public async Task Initialize(VideoSetting videoSetting)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAndAwaitAsync(CoreDispatcherPriority.Normal, async () =>
            {
                _threadsCount = videoSetting.UsedThreads;
                _stoppedThreads = videoSetting.UsedThreads;

                _lastFrameAdded.Start();

                _imageQuality = new BitmapPropertySet();
                var imageQualityValue = new BitmapTypedValue(videoSetting.VideoQuality, Windows.Foundation.PropertyType.Single);
                _imageQuality.Add("ImageQuality", imageQualityValue);

                _mediaCapture = new MediaCapture();

                var frameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();

                var settings = new MediaCaptureInitializationSettings()
                {
                    SharingMode = MediaCaptureSharingMode.ExclusiveControl,

                    //With CPU the results contain always SoftwareBitmaps, otherwise with GPU
                    //they preferring D3DSurface
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu,

                    //Capture only video, no audio
                    StreamingCaptureMode = StreamingCaptureMode.Video
                };

                await _mediaCapture.InitializeAsync(settings);

                var mediaFrameSource = _mediaCapture.FrameSources.First().Value;
                var videoDeviceController = mediaFrameSource.Controller.VideoDeviceController;

                videoDeviceController.DesiredOptimization = Windows.Media.Devices.MediaCaptureOptimization.Quality;
                videoDeviceController.PrimaryUse = Windows.Media.Devices.CaptureUse.Video;
                
                //Set exposure (auto light adjustment)
                if (_mediaCapture.VideoDeviceController.Exposure.Capabilities.Supported
                    && _mediaCapture.VideoDeviceController.Exposure.Capabilities.AutoModeSupported)
                {
                    _mediaCapture.VideoDeviceController.Exposure.TrySetAuto(true);
                }

                var videoResolutionWidthHeight = VideoResolutionWidthHeight.Get(videoSetting.VideoResolution);
                var videoSubType = VideoSubtypeHelper.Get(videoSetting.VideoSubtype);

                //Set resolution, frame rate and video subtyp
                var videoFormat = mediaFrameSource.SupportedFormats.Where(sf => sf.VideoFormat.Width == videoResolutionWidthHeight.Width
                                                                                && sf.VideoFormat.Height == videoResolutionWidthHeight.Height
                                                                                && sf.Subtype == videoSubType)
                                                                    .OrderByDescending(m => m.FrameRate.Numerator / m.FrameRate.Denominator)
                                                                    .First();

                await mediaFrameSource.SetFormatAsync(videoFormat);

                _mediaFrameReader = await _mediaCapture.CreateFrameReaderAsync(mediaFrameSource);
                await _mediaFrameReader.StartAsync();
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GarbageCollectorCanWorkHere() { }

        private void ProcessFrames()
        {
            _stoppedThreads--;

            while (_stopThreads == false)
            {
                try
                {
                    GarbageCollectorCanWorkHere();
                    
                    var frame = _mediaFrameReader.TryAcquireLatestFrame();

                    var frameDuration = new Stopwatch();
                    frameDuration.Start();

                    if (frame == null
                        || frame.VideoMediaFrame == null
                        || frame.VideoMediaFrame.SoftwareBitmap == null)
                        continue;

                    using (var stream = new InMemoryRandomAccessStream())
                    {
                        using (var bitmap = SoftwareBitmap.Convert(frame.VideoMediaFrame.SoftwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore))
                        {
                            var imageTask = BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream, _imageQuality).AsTask();
                            imageTask.Wait();
                            var encoder = imageTask.Result;
                            encoder.SetSoftwareBitmap(bitmap);

                            ////Rotate image 180 degrees
                            //var transform = encoder.BitmapTransform;
                            //transform.Rotation = BitmapRotation.Clockwise180Degrees;

                            var flushTask = encoder.FlushAsync().AsTask();
                            flushTask.Wait();

                            using (var asStream = stream.AsStream())
                            {
                                asStream.Position = 0;

                                var image = new byte[asStream.Length];
                                asStream.Read(image, 0, image.Length);

                                lock (_lastFrameAddedLock)
                                {
                                    if (_lastFrameAdded.Elapsed.Subtract(frameDuration.Elapsed) > TimeSpan.Zero)
                                    {
                                        Frame = image;

                                        _lastFrameAdded = frameDuration;
                                    }
                                }

                                encoder = null;
                            }
                        }
                    }
                }
                catch (ObjectDisposedException) { }
            }

            _stoppedThreads++;
        }

        public void Start()
        {
            for (int workerNumber = 0; workerNumber < _threadsCount; workerNumber++)
            {
                Task.Factory.StartNew(() =>
                {
                    ProcessFrames();

                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                .AsAsyncAction()
                .AsTask();
            }
        }

        public async Task Stop()
        {
            _stopThreads = true;
            
            SpinWait.SpinUntil(() => { return _threadsCount == _stoppedThreads; });

            await _mediaFrameReader.StopAsync();

            _stopThreads = false;
        }

        public async Task<List<MediaFrameFormat>> GetMediaFrameFormatsAsync()
        {
            var mediaFrameFormats = new List<MediaFrameFormat>();

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAndAwaitAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var mediaCapture = new MediaCapture();

                var settings = new MediaCaptureInitializationSettings()
                {
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                    StreamingCaptureMode = StreamingCaptureMode.Video
                };

                await mediaCapture.InitializeAsync(settings);

                var mediaFrameSource = mediaCapture.FrameSources.First().Value;

                mediaFrameFormats = mediaFrameSource.SupportedFormats.ToList();

                mediaCapture.Dispose();
            });

            return mediaFrameFormats;
        }
    }
}