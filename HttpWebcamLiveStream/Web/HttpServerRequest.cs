using System;
using System.Text.RegularExpressions;
using Windows.Data.Json;

namespace HttpWebcamLiveStream.Web
{
    public class HttpServerRequest
    {
        public string Request { get; private set; }
        public JsonObject Body { get; private set; }
        public string Url { get; private set; }
        public bool Error { get; private set; }

        public HttpServerRequest(string request, bool error)
        {
            request = request ?? string.Empty;

            Request = request;
            Error = error;

            var urlRegex = new Regex(".*GET(.*)HTTP.*", RegexOptions.IgnoreCase);
            var urlGroups = urlRegex.Match(request).Groups;
            Url = urlGroups.Count >= 2 ? urlGroups[1].Value.Trim() : string.Empty;

            var bodyRegex = new Regex("<RequestBody>(.*)</RequestBody>", RegexOptions.IgnoreCase);
            var bodyGroups = bodyRegex.Match(Uri.UnescapeDataString(Url)).Groups;
            var body = bodyGroups.Count >= 2 ? bodyGroups[1].Value.Trim() : null;
            if (body != null)
            {
                JsonObject bodyJson = null;
                if (JsonObject.TryParse(body, out bodyJson))
                {
                    Body = bodyJson;
                }
            }
        }
    }
}
