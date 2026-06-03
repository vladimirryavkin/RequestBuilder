using System;

namespace RequestBuilder.ViewModels
{
    public class RequestHistoryItem
    {
        public string Url { get; set; }
        public HttpVerb HttpVerb { get; set; }
        public string Headers { get; set; }
        public string Body { get; set; }
        public string ResponseString { get; set; }
        public string ResponseHeaders { get; set; }
        public string Status { get; set; }
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; }

        public string ShortUrl =>
            string.IsNullOrEmpty(Url) ? "(empty)" :
            Url.Length > 38 ? Url.Substring(0, 35) + "..." : Url;

        public RequestHistoryItem() { }

        public RequestHistoryItem(string url, HttpVerb httpVerb, string headers, string body,
            string responseString, string responseHeaders, string status, int statusCode)
        {
            Url = url;
            HttpVerb = httpVerb;
            Headers = headers;
            Body = body;
            ResponseString = responseString;
            ResponseHeaders = responseHeaders;
            Status = status;
            StatusCode = statusCode;
            Timestamp = DateTime.Now;
        }
    }
}
