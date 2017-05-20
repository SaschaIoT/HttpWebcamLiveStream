using HttpWebcamLiveStream.Configuration;
using HttpWebcamLiveStream.Devices;
using HttpWebcamLiveStream.Web;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HttpWebcamLiveStream
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs eventArgs)
        {
            var camera = new Camera();
            var mediaFrameFormats = await camera.GetMediaFrameFormatsAsync();
            ConfigurationFile.SetSupportedVideoFrameFormats(mediaFrameFormats);
            var videoSetting = await ConfigurationFile.Read(mediaFrameFormats);

            await camera.Initialize(videoSetting);
            camera.Start();

            var httpServer = new HttpServer(camera);
            httpServer.Start();
        }
    }
}
