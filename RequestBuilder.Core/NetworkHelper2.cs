using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RequestBuilder
{
    public class NetworkHelper2
    {
        private readonly string _defaultUserAgent;

        public NetworkHelper2(string defaultUserAgent = "RequestBuilder-HttpClient")
        {
            _defaultUserAgent = defaultUserAgent;
        }

        public async Task<HttpResponse> MakeRemoteRequestAsync(string url, CancellationToken cancellationToken)
        {
            return await MakeRemoteRequestAsync(new HttpRequest
            {
                HttpVerb = HttpVerb.Get,
                Url = url
            }, cancellationToken);
        }

        public async Task<HttpResponse> MakeRemoteRequestAsync(HttpRequest info, CancellationToken cancellationToken = default)
        {
            Guard.ParamNotNull(info, nameof(info));
            info.Validate();
            var domain = info.GetDomain();
            var handler = new HttpClientHandler();
            if (info.AllowAutoRedirect == false)
            {
                handler.AllowAutoRedirect = false;
            }

            if (info.ClientCertificate != null)
            {
                handler.ClientCertificates.Add(info.ClientCertificate);
            }

            if (info.ServerCertificateCustomValidationCallback != null)
            {
                handler.ServerCertificateCustomValidationCallback = info.ServerCertificateCustomValidationCallback;
            }

            using var client = new HttpClient(handler);
            if (info.Timeout.HasValue)
            {
                client.Timeout = info.Timeout.Value;
            }

            using var request = BuildRequest(info);

            try
            {
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                var ms = new MemoryStream();
                if (response.Content != null)
                {
                    using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    await stream.CopyToAsync(ms, 81920, cancellationToken);
                }
                ms.Position = 0;

                var headers = response.Headers
                    .SelectMany(h => h.Value.Select(v => new KeyValuePair<string, string>(h.Key, v)))
                    .Concat(response.Content?.Headers.SelectMany(h => h.Value.Select(v => new KeyValuePair<string, string>(h.Key, v))) ?? Enumerable.Empty<KeyValuePair<string, string>>())
                    .ToList();

                var result = new HttpResponse(response.StatusCode, headers, ms, domain);
                var statusCodeIsError = ((int)response.StatusCode) < 200 || ((int)response.StatusCode) >= 400;
                if (statusCodeIsError && !info.ProceedOnError)
                    throw new HttpRequestException($"HTTP {response.StatusCode}: {response.ReasonPhrase}");

                result.ErrorOccurred = statusCodeIsError;
                return result;
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("Request timeout.");
            }
        }

        public HttpResponse MakeRemoteRequest(string url)
        {
            return MakeRemoteRequest(new HttpRequest
            {
                Url = url,
                HttpVerb = HttpVerb.Get
            });
        }

        public HttpResponse MakeRemoteRequest(HttpRequest info)
        {
            return MakeRemoteRequestAsync(info).GetAwaiter().GetResult();
        }

        // Request builder
        private HttpRequestMessage BuildRequest(HttpRequest info)
        {
            var request = new HttpRequestMessage(new HttpMethod(info.HttpVerb.ToString().ToUpper()), info.GetUrl());

            var content = CreateHttpContent(info);
            if (content != null)
                request.Content = content;

            request.Headers.UserAgent.Clear();
            request.Headers.TryAddWithoutValidation("User-Agent", info.UserAgent ?? _defaultUserAgent);

            if (!string.IsNullOrWhiteSpace(info.BearerToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", info.BearerToken);
            }
            else if (!string.IsNullOrWhiteSpace(info.UserName))
            {
                var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{info.UserName}:{info.Password ?? ""}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            }

            if (info.EmulateAjax)
                request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");

            if (info.Cookies?.Any() == true)
            {
                var cookieStr = string.Join("; ", info.Cookies.Select(c => $"{c.Name}={c.Value}"));
                request.Headers.TryAddWithoutValidation("Cookie", cookieStr);
            }

            foreach (var kv in info.GetAllHeaders())
            {
                var key = kv.Key is HttpRequestHeader h ? h.ToString() : kv.Key.ToString();
                request.Headers.TryAddWithoutValidation(key, kv.Value);
            }

            return request;
        }

        private static HttpContent CreateHttpContent(HttpRequest info)
        {
            if (info.MultipartPostParams?.Any() == true)
            {
                var multi = new MultipartFormDataContent();
                foreach (var p in info.MultipartPostParams)
                {
                    if (p.FileName != null)
                    {
                        var bc = new ByteArrayContent(p.Value ?? Array.Empty<byte>());
                        if (p.ContentType != null)
                            bc.Headers.ContentType = MediaTypeHeaderValue.Parse(p.ContentType);
                        multi.Add(bc, p.ParamName, p.FileName);
                    }
                    else
                    {
                        var val = p.Value != null ? Encoding.UTF8.GetString(p.Value) : "";
                        multi.Add(new StringContent(val, Encoding.UTF8), p.ParamName);
                    }
                }
                return multi;
            }

            if (info.PostBinaryBody?.Any() == true)
                return new ByteArrayContent(info.PostBinaryBody);

            if (!string.IsNullOrWhiteSpace(info.PostBody))
                return new StringContent(info.PostBody, Encoding.UTF8, info.ContentType ?? "application/x-www-form-urlencoded");

            if (info.PostParams?.Any() == true)
                return new FormUrlEncodedContent(info.PostParams);

            return null;
        }
    }
}
