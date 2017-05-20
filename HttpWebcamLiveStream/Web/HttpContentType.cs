using System;

namespace HttpWebcamLiveStream.Web
{
    public enum HttpContentType
    {
        Html,
        JavaScript,
        Css,
        Text,
        Json,
        Jpeg,
        Png
    }

    public static class MimeTypeHelper
    {
        public static string GetHttpContentType(HttpContentType httpContentType)
        {
            switch (httpContentType)
            {
                case HttpContentType.Html:
                    return "text/html; charset=UTF-8";
                case HttpContentType.JavaScript:
                    return "text/javascript; charset=UTF-8";
                case HttpContentType.Css:
                    return "text/css; charset=UTF-8";
                case HttpContentType.Text:
                    return "text/plain; charset=UTF-8";
                case HttpContentType.Json:
                    return "application/json; charset=UTF-8";
                case HttpContentType.Jpeg:
                    return "image/jpeg";
                case HttpContentType.Png:
                    return "image/png";
                default:
                    throw new Exception($"Could not get mime type for http header for {httpContentType}");
            }
        }
    }
}
