using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;

namespace RequestBuilder
{
    public class HttpRequest
    {
        public const string ContentTypeFormUrlEncoded = "application/x-www-form-urlencoded";
        public const string ContentTypeJson = "application/json";
        public const string ContentTypeMultipartFormData = "multipart/form-data";
        private Dictionary<HttpRequestHeader, string> RequestHeaders;
        private Dictionary<string, string> CustomRequestHeaders;
        private ICollection<KeyValuePair<string, string>> _UrlParams;
        private ICollection<KeyValuePair<string, string>> _PostParams;
        private ICollection<IMultipartParameter> _MultipartPostParams;
        private ICollection<Cookie> _Cookies;
        private HttpVerb? _HttpVerb;
        private bool? allowAutoRedirect;

        /// <summary>
        /// The actual url of the resource to query. May contain QueryString
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// Querystring parameters. Will be appended even if the source url contains
        /// a querystring already.
        /// </summary>
        public ICollection<KeyValuePair<string, string>> UrlParams
        {
            get { return _UrlParams = _UrlParams ?? new List<KeyValuePair<string, string>>(); }
            set { _UrlParams = value; }
        }
        /// <summary>
        /// A property for populating application/x-www-form-urlencoded Suitable for
        /// sending standard key=value pairs
        /// <para>
        /// Properties PostParams, MultipartPostParams, PostBody and PostBinaryBody are mutually exclusive
        /// use only one of them.
        /// </para>
        /// </summary>
        public ICollection<KeyValuePair<string, string>> PostParams
        {
            get { return _PostParams = _PostParams ?? new List<KeyValuePair<string, string>>(); }
            set { _PostParams = value; }
        }
        /// <summary>
        /// <para>
        /// A property for populating multipart/form-data Suitalbe for 
        /// sending files over http. Use <see cref="RequestBuilder.PostFileInfo"/> for file parameters and 
        /// <see cref="RequestBuilder.PostValueParam"/> for standard string parameters
        /// </para>
        /// <para>
        /// Properties PostParams, MultipartPostParams, PostBody and PostBinaryBody are mutually exclusive
        /// use only one of them.
        /// </para>
        /// </summary>

        public ICollection<IMultipartParameter> MultipartPostParams
        {
            get { return _MultipartPostParams = _MultipartPostParams ?? new List<IMultipartParameter>(); }
            set { _MultipartPostParams = value; }
        }
        /// <summary>
        /// A property to place a raw string to the request body. Use for posting radom data
        /// like JSON or XML
        /// <para>
        /// Properties PostParams, MultipartPostParams, PostBody and PostBinaryBody are mutually exclusive
        /// use only one of them.
        /// </para>
        /// </summary>
        public string PostBody { get; set; }
        /// <summary>
        /// If you attempt to send a random binary body, use this property
        /// <para>
        /// Properties PostParams, MultipartPostParams, PostBody and PostBinaryBody are mutually exclusive
        /// use only one of them.
        /// </para>
        /// </summary>
        public byte[] PostBinaryBody { get; set; }


        public ICollection<Cookie> Cookies
        {
            get { return _Cookies = _Cookies ?? new List<Cookie>(); }
            set { _Cookies = value; }
        }
        /// <summary>
        /// Set this property if you want to specify a custom UserAgent
        /// </summary>
        public string UserAgent { get; set; }
        /// <summary>
        /// If set true, the request will add a X-Requested-With: XMLHttpRequest header
        /// just like any browser making an Ajax request.
        /// </summary>
        public bool EmulateAjax { get; set; }
        /// <summary>
        /// If set true, the response will be returned in any status code
        /// including 500 or 404. Default is false, i.e. will throw error on any
        /// status code that does not mean a success.
        /// </summary>
        public bool ProceedOnError { get; set; }
        public HttpVerb HttpVerb
        {
            get { return (_HttpVerb = _HttpVerb ?? HttpVerb.Get).Value; }
            set { _HttpVerb = value; }
        }
        /// <summary>
        /// If set it will be used for Authorization: Basic header 
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// If set it will be used for Authorization: Basic header 
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// If set it will be used for Authorization Bearer
        /// </summary>
        public string BearerToken { get; set; }
        /// <summary>
        /// If set false redirect URL's will not be queried automatically, but return the raw 
        /// response of the redirect with the target location. Default is true.
        /// </summary>
        public bool AllowAutoRedirect
        {
            get
            {
                if (!allowAutoRedirect.HasValue)
                    allowAutoRedirect = true;
                return allowAutoRedirect.Value;
            }
            set => allowAutoRedirect = value;
        }

        /// <summary>
        /// Certificate to include for mTLS requests
        /// </summary>
        public X509Certificate2 ClientCertificate { get; set; }

        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback { get; set; }

        public TimeSpan? Timeout { get; set; }
        #region [ Convenience properties ]
        public string ContentType
        {
            get { return GetHeader(HttpRequestHeader.ContentType); }
            set { SetHeader(HttpRequestHeader.ContentType, value); }
        }

        #endregion
        public static string CombinePath(string baseUrl, params string[] folders)
        {
            Guard.PropertyNotNullOrEmpty(baseUrl, "baseUrl");
            var sb = new StringBuilder(baseUrl);
            for (var i = 0; i < folders.Length; i++)
            {
                if (sb[sb.Length - 1] != '/')
                    sb.Append('/');

                sb.Append(folders[i].Substring(folders[i].StartsWith("/") ? 1 : 0));
            }
            return sb.ToString();
        }

        public static bool IsAbsoluteUrl(string url)
        {
            Uri result;
            return Uri.TryCreate(url, UriKind.Absolute, out result);
        }

        public HttpRequest AddHeader(HttpRequestHeader header, string value)
        {
            SetHeader(header, value);
            return this;
        }
        public HttpRequest AddHeader(string header, string value)
        {
            if (CustomRequestHeaders == null)
                CustomRequestHeaders = new Dictionary<string, string>();
            switch (header.ToLower())
            {
                case "accept":
                    return AddHeader(HttpRequestHeader.Accept, value);
                case "accept-charset":
                    return AddHeader(HttpRequestHeader.AcceptCharset, value);
                case "accept-encoding":
                    return AddHeader(HttpRequestHeader.AcceptEncoding, value);
                case "accept-language":
                    return AddHeader(HttpRequestHeader.AcceptLanguage, value);
                case "cache-control":
                    return AddHeader(HttpRequestHeader.CacheControl, value);
                case "connection":
                    return AddHeader(HttpRequestHeader.Connection, value);
                case "cookie":
                    return AddHeader(HttpRequestHeader.Cookie, value);
                case "date":
                    return AddHeader(HttpRequestHeader.Date, value);
                case "content-md5":
                    return AddHeader(HttpRequestHeader.ContentMd5, value);
                case "content-type":
                    return AddHeader(HttpRequestHeader.ContentType, value);
                case "content-encoding":
                    return AddHeader(HttpRequestHeader.ContentEncoding, value);
                case "content-language":
                    return AddHeader(HttpRequestHeader.ContentLanguage, value);
                case "content-location":
                    return AddHeader(HttpRequestHeader.ContentLocation, value);
                case "referer":
                    return AddHeader(HttpRequestHeader.Referer, value);
                default:
                    break;
            }
            CustomRequestHeaders[header] = value;
            return this;
        }
        public string GetUrl()
        {
            if (!UrlParams.Any())
                return Url;
            var baseUrl = new StringBuilder(Url);
            foreach (var item in UrlParams)
            {
                var hasQs = baseUrl.LastIndexOf('?') > -1;
                if (hasQs && (baseUrl[baseUrl.Length - 1] == '?' || baseUrl[baseUrl.Length - 1] == '&'))
                    baseUrl.AppendFormat("{0}={1}", HttpUtility.UrlEncode(item.Key), HttpUtility.UrlEncode(item.Value));
                else if (hasQs)
                    baseUrl.AppendFormat("&{0}={1}", HttpUtility.UrlEncode(item.Key), HttpUtility.UrlEncode(item.Value));
                else
                    baseUrl.AppendFormat("?{0}={1}", HttpUtility.UrlEncode(item.Key), HttpUtility.UrlEncode(item.Value));
            }
            return baseUrl.ToString();
        }

        public string GetDomain()
        {
            var uri = new Uri(Url);
            return uri.Host;
        }

        internal Dictionary<Object, string> GetAllHeaders()
        {
            if (CustomRequestHeaders == null)
                CustomRequestHeaders = new Dictionary<string, string>();
            if (RequestHeaders == null)
                RequestHeaders = new Dictionary<HttpRequestHeader, string>();
            return CustomRequestHeaders.ToDictionary(x => (object)x.Key, x => x.Value)
                .Union(RequestHeaders.ToDictionary(x => (object)x.Key, x => x.Value))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(Url))
                throw new InvalidOperationException("Url cannot be empty");
            if ((HttpVerb == HttpVerb.Get || HttpVerb == HttpVerb.Head || HttpVerb == HttpVerb.Delete) && (
                    PostParams.Return(x => x.Any(), false) ||
                    MultipartPostParams.Return(x => x.Any(), false) ||
                    PostBody.Return(x => x.Length > 0, false) ||
                    PostBinaryBody.Return(x => x.Any(), false)
                )
            )
            {
                throw new InvalidOperationException(string.Format("Request body is not supported for {0} method", HttpVerb));
            }

            if (new[] {
                PostParams.Return(x => x.Any(), false),
                MultipartPostParams.Return(x => x.Any(), false),
                PostBody.Return(x => x.Length > 0, false),
                PostBinaryBody.Return(x => x.Any(), false)
            }.Where(x => x).Count() > 1)
            {
                throw new InvalidOperationException("Only one of the properties \"PostParams\", \"MultipartPostParams\", \"PostBody\" and \"PostBinaryBody\" must be populated with data. Others should be null or empty");
            }

            if (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password) && !string.IsNullOrWhiteSpace(BearerToken))
            {
                throw new InvalidOperationException("Bearer token and Username/Password cannot be used simultaneously");
            }
        }
        private string GetHeader(HttpRequestHeader header)
        {
            if (RequestHeaders != null && RequestHeaders.ContainsKey(header))
                return RequestHeaders[header];
            return null;
        }
        private void SetHeader(HttpRequestHeader header, string value)
        {
            if (value != null)
            {
                if (RequestHeaders == null)
                    RequestHeaders = new Dictionary<HttpRequestHeader, string>();
                RequestHeaders[header] = value;
            }
        }
    }
}