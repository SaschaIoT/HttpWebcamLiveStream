using HttpWebcamLiveStream.Helper;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

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

        //Check if camera support resolution and subtyp before change
        private const int VIDEO_WIDTH = 640;
        private const int VIDEO_HEIGHT = 480;
        private const string VIDEO_SUBTYP = "YUY2";
        private const double IMAGE_QUALITY_PERCENT = 0.8d;
        private BitmapPropertySet _imageQuality;

        public async Task Initialize()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAndAwaitAsync(CoreDispatcherPriority.Normal, async () =>
            {
                _imageQuality = new BitmapPropertySet();
                var imageQualityValue = new BitmapTypedValue(IMAGE_QUALITY_PERCENT, Windows.Foundation.PropertyType.Single);
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

                //Set backlight compensation to min (otherwise there are problems with strong light sources)
                if (videoDeviceController.BacklightCompensation.Capabilities.Supported)
                {
                    videoDeviceController.BacklightCompensation.TrySetValue(videoDeviceController.BacklightCompensation.Capabilities.Min);
                }

                //Set exposure (auto light adjustment)
                if (_mediaCapture.VideoDeviceController.Exposure.Capabilities.Supported
                    && _mediaCapture.VideoDeviceController.Exposure.Capabilities.AutoModeSupported)
                {
                    _mediaCapture.VideoDeviceController.Exposure.TrySetAuto(true);
                }

                //Set resolution, frame rate and video subtyp
                var videoFormat = mediaFrameSource.SupportedFormats.First(sf => sf.VideoFormat.Width == VIDEO_WIDTH
                                                                                && sf.VideoFormat.Height == VIDEO_HEIGHT
                                                                                && sf.Subtype == VIDEO_SUBTYP);

                await mediaFrameSource.SetFormatAsync(videoFormat);

                _mediaFrameReader = await _mediaCapture.CreateFrameReaderAsync(mediaFrameSource);

                _mediaFrameReader.FrameArrived += FrameArrived;

                await _mediaFrameReader.StartAsync();
            });
        }

        public void FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs eventArgs)
        {
            var frame = _mediaFrameReader.TryAcquireLatestFrame();

            if (frame == null
                || frame.VideoMediaFrame == null
                || frame.VideoMediaFrame.SoftwareBitmap == null)
                return;

            using (var stream = new InMemoryRandomAccessStream())
            {
                using (var bitmap = SoftwareBitmap.Convert(frame.VideoMediaFrame.SoftwareBitmap, BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied))
                {
                    var imageTask = BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream, _imageQuality).AsTask();
                    imageTask.Wait();
                    var encoder = imageTask.Result;
                    encoder.SetSoftwareBitmap(bitmap);

                    //Rotate image 180 degrees
                    //var transform = encoder.BitmapTransform;
                    //transform.Rotation = BitmapRotation.Clockwise180Degrees;

                    var flushTask = encoder.FlushAsync().AsTask();
                    flushTask.Wait();

                    using (var asStream = stream.AsStream())
                    {
                        asStream.Position = 0;

                        var image = new byte[asStream.Length];
                        asStream.Read(image, 0, image.Length);

                        Frame = image;

                        encoder = null;
                    }
                }
            }
        }
    }
}