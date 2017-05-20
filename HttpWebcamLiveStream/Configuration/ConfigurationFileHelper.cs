using System;
using System.Collections.Generic;

namespace HttpWebcamLiveStream.Configuration
{
    public enum VideoResolution
    {
        HD1080p = 0,
        HD720p = 1,
        SD1024_768 = 2,
        SD800_600 = 3,
        SD640_480 = 4
    }

    public enum VideoSubtype
    {
        YUY2 = 0,
        MJPG = 1,
        NV12 = 2
    }

    public class VideoSubtypeHelper
    {
        public static string Get(VideoSubtype videoSubType)
        {
            if (videoSubType == VideoSubtype.YUY2)
            {
                return "YUY2";
            }
            else if (videoSubType == VideoSubtype.MJPG)
            {
                return "MJPG";
            }
            else if (videoSubType == VideoSubtype.NV12)
            {
                return "NV12";
            }

            throw new Exception("Video subtype not exists.");
        }

        public static VideoSubtype Get(string videoSubType)
        {
            if (videoSubType == "YUY2")
            {
                return VideoSubtype.YUY2;
            }
            else if (videoSubType == "MJPG")
            {
                return VideoSubtype.MJPG;
            }
            else if (videoSubType == "NV12")
            {
                return VideoSubtype.NV12;
            }

            throw new Exception("Video subtype not exists.");
        }
    }

    public class VideoResolutionWidthHeight
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public static VideoResolutionWidthHeight Get(VideoResolution videoResolution)
        {
            var videoResolutionWidthHeight = new VideoResolutionWidthHeight();

            if (videoResolution == VideoResolution.HD1080p)
            {
                videoResolutionWidthHeight.Width = 1920;
                videoResolutionWidthHeight.Height = 1080;
            }
            else if (videoResolution == VideoResolution.HD720p)
            {
                videoResolutionWidthHeight.Width = 1280;
                videoResolutionWidthHeight.Height = 720;
            }
            else if (videoResolution == VideoResolution.SD1024_768)
            {
                videoResolutionWidthHeight.Width = 1024;
                videoResolutionWidthHeight.Height = 768;
            }
            else if (videoResolution == VideoResolution.SD800_600)
            {
                videoResolutionWidthHeight.Width = 640;
                videoResolutionWidthHeight.Height = 480;
            }
            else if (videoResolution == VideoResolution.SD640_480)
            {
                videoResolutionWidthHeight.Width = 640;
                videoResolutionWidthHeight.Height = 480;
            }

            return videoResolutionWidthHeight;
        }
    }

    public class SupportetVideoResolution
    {
        public List<VideoResolution> VideoResolutions { get; set; }
        public string VideoSubtype { get; set; }
    }

    public class VideoSetting
    {
        public VideoResolution VideoResolution { get; set; }
        public VideoSubtype VideoSubtype { get; set; }
        public double VideoQuality { get; set; }
        public int UsedThreads { get; set; }
    }
}
