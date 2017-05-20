using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Media.Capture.Frames;
using Windows.Storage;

namespace HttpWebcamLiveStream.Configuration
{
    public static class ConfigurationFile
    {
        const string CONFIGURATION_FILE_NAME = "ConfigurationFile.txt";

        public static JsonArray VideoSettingsSupported { get; private set; }
        public static JsonObject VideoSetting { get; set; }

        public static void SetSupportedVideoFrameFormats(List<MediaFrameFormat> mediaFrameFormats)
        {
            VideoSettingsSupported = new JsonArray();

            var videoSettings = new List<VideoSetting>();

            for (int videoSubTypeId = 0; videoSubTypeId <= 2; videoSubTypeId++)
            {
                var videoSubType = (VideoSubtype)videoSubTypeId;

                for (int videoResolutionId = 0; videoResolutionId <= 4; videoResolutionId++)
                {
                    var videoResolution = (VideoResolution)videoResolutionId;
                    var videoResolutionWidthHeight = VideoResolutionWidthHeight.Get(videoResolution);

                    var mediaFrameFormat = mediaFrameFormats.FirstOrDefault(m => m.Subtype == VideoSubtypeHelper.Get((VideoSubtype)videoSubType)
                                                                                 && m.VideoFormat.Width == videoResolutionWidthHeight.Width
                                                                                 && m.VideoFormat.Height == videoResolutionWidthHeight.Height);

                    if (mediaFrameFormat != null)
                    {
                        videoSettings.Add(new VideoSetting
                        {
                            VideoResolution = videoResolution,
                            VideoSubtype = videoSubType
                        });
                    }
                }
            }
            
            foreach (var videoSettingGrouped in videoSettings.GroupBy(v => v.VideoSubtype))
            {
                var videoSubType = VideoSubtypeHelper.Get(videoSettingGrouped.Key);
                var videoSettingsSupported = new JsonArray();
                
                foreach (var videoSetting in videoSettingGrouped)
                {
                    videoSettingsSupported.Add(JsonValue.CreateNumberValue((int)videoSetting.VideoResolution));
                }

                VideoSettingsSupported.Add(new JsonObject
                {
                    { "VideoSubtype", JsonValue.CreateStringValue(videoSubType) },
                    { "VideoResolutions", videoSettingsSupported }
                });
            }
        }

        public static async Task Write(VideoSetting videoSetting)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var configurationFile = await localFolder.CreateFileAsync(CONFIGURATION_FILE_NAME, CreationCollisionOption.OpenIfExists);

            var configuration = new JsonObject
                        {
                            { "VideoResolution", JsonValue.CreateNumberValue((int)videoSetting.VideoResolution) },
                            { "VideoSubtype", JsonValue.CreateStringValue(VideoSubtypeHelper.Get(videoSetting.VideoSubtype)) },
                            { "VideoQuality", JsonValue.CreateNumberValue(videoSetting.VideoQuality) },
                            { "UsedThreads", JsonValue.CreateNumberValue(videoSetting.UsedThreads) }
                        };

            VideoSetting = configuration;

            await FileIO.WriteTextAsync(configurationFile, configuration.Stringify());
        }

        public static async Task<VideoSetting> Read(List<MediaFrameFormat> mediaFrameFormats)
        {
            VideoSetting videoSetting = null;
            JsonObject configuration = null;
            var configurationFileExists = await ApplicationData.Current.LocalFolder.TryGetItemAsync(CONFIGURATION_FILE_NAME);
            if(configurationFileExists != null)
            {
                var configurationFile = await ApplicationData.Current.LocalFolder.GetFileAsync(CONFIGURATION_FILE_NAME);
                configuration = JsonObject.Parse(await FileIO.ReadTextAsync(configurationFile));
            }

            if (configuration != null)
            {
                var videoResolution = (VideoResolution)configuration["VideoResolution"].GetNumber();
                var videoResolutionWidthHeight = VideoResolutionWidthHeight.Get(videoResolution);

                var videoSubType = configuration["VideoSubtype"].GetString();
                var videoQuality = configuration["VideoQuality"].GetNumber();
                var usedThreads = configuration["UsedThreads"].GetNumber();

                var mediaFrameFormat = mediaFrameFormats.Where(m => m.Subtype == videoSubType
                                                                && m.VideoFormat.Width == videoResolutionWidthHeight.Width
                                                                && m.VideoFormat.Height == videoResolutionWidthHeight.Height)
                                                    .OrderByDescending(m => m.FrameRate.Numerator / m.FrameRate.Denominator)
                                                    .FirstOrDefault();
                if (mediaFrameFormat != null)
                {
                    videoSetting = new VideoSetting
                    {
                        VideoResolution = videoResolution,
                        VideoSubtype = VideoSubtypeHelper.Get(videoSubType),
                        VideoQuality = videoQuality,
                        UsedThreads = (int)usedThreads
                    };
                }
            }
            else
            {
                for (int videoSubType = 0; videoSubType <= 2; videoSubType++)
                {
                    if (videoSetting != null)
                        break;

                    for (int videoResolutionId = 4; videoResolutionId >= 0; videoResolutionId--)
                    {
                        var videoResolutionLowWidthHeight = VideoResolutionWidthHeight.Get((VideoResolution)videoResolutionId);

                        var mediaFrameFormat = mediaFrameFormats.Where(m => m.Subtype == VideoSubtypeHelper.Get((VideoSubtype)videoSubType)
                                            && m.VideoFormat.Width == videoResolutionLowWidthHeight.Width
                                            && m.VideoFormat.Height == videoResolutionLowWidthHeight.Height)
                                .OrderByDescending(m => m.FrameRate.Numerator / m.FrameRate.Denominator)
                                .FirstOrDefault();

                        if (mediaFrameFormat != null)
                        {
                            videoSetting = new VideoSetting
                            {
                                VideoResolution = VideoResolution.SD640_480,
                                VideoSubtype = (VideoSubtype)videoSubType,
                                VideoQuality = 0.6,
                                UsedThreads = 4
                            };

                            break;
                        }
                    }
                }
            }

            if(videoSetting != null)
            {
                VideoSetting = new JsonObject
                {
                    { "VideoResolution", JsonValue.CreateNumberValue((int)videoSetting.VideoResolution) },
                    { "VideoSubtype", JsonValue.CreateStringValue(VideoSubtypeHelper.Get(videoSetting.VideoSubtype)) },
                    { "VideoQuality", JsonValue.CreateNumberValue(videoSetting.VideoQuality) },
                    { "UsedThreads", JsonValue.CreateNumberValue(videoSetting.UsedThreads) }
                };

                return videoSetting;
            }
            else
            {
                throw new Exception("Webcam not supported. Could not found correct video resolution and subtype. Please change code.");
            }
        }
    }
}
