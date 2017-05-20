using System;

namespace HttpWebcamLiveStream.Web
{
    public enum HttpStatusCode
    {
        HttpCode200,
        HttpCode204,
        HttpCode500
    }

    public static class HttpStatusCodeHelper
    {
        public static string GetHttpStatusCodeForHttpHeader(HttpStatusCode httpStatusCode)
        {
            switch (httpStatusCode)
            {
                case HttpStatusCode.HttpCode200:
                    return "HTTP/1.1 200 OK";
                case HttpStatusCode.HttpCode204:
                    return "HTTP/1.1 204 No Content";
                case HttpStatusCode.HttpCode500:
                    return "HTTP/1.1 500 Internal Server Error";
                default:
                    throw new Exception($"Could not get http status code for http header for {httpStatusCode}");
            }
        }
    }
}
