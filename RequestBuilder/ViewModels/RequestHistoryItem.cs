using System;

namespace RequestBuilder.ViewModels
{
    public class RequestHistoryItem
    {
        public string Url { get; }
        public HttpVerb HttpVerb { get; }
        public string Headers { get; }
        public string Body { get; }
        public string ResponseString { get; }
        public string ResponseHeaders { get; }
        public string Status { get; }
        public DateTime Timestamp { get; }

        public string ShortUrl =>
            string.IsNullOrEmpty(Url) ? "(empty)" :
            Url.Length > 38 ? Url.Substring(0, 35) + "..." : Url;

        public RequestHistoryItem(string url, HttpVerb httpVerb, string headers, string body,
            string responseString, string responseHeaders, string status)
        {
            Url = url;
            HttpVerb = httpVerb;
            Headers = headers;
            Body = body;
            ResponseString = responseString;
            ResponseHeaders = responseHeaders;
            Status = status;
            Timestamp = DateTime.Now;
        }
    }
}
