using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace RequestBuilder.ViewModels
{
    public enum BodyMode { Raw, FormUrlEncoded, Multipart }

    public class RequestSessionViewModel : BaseViewModel
    {
        private string url;
        private HttpVerb httpVerb;
        private string headers;
        private string body;
        private string responseHeaders;
        private string responseString;
        private string status;
        private bool isLoading;
        private bool isResponseExpanded;
        private Dispatcher dispatcher;
        private ObservableCollection<string> urls;
        private BodyMode bodyMode = BodyMode.Raw;
        private ObservableCollection<FormParamViewModel> formParams;
        private ObservableCollection<MultipartParamViewModel> multipartParams;
        private string boundary;
        private bool isFormBodyUnparsable;
        private bool isMultipartBodyUnparsable;

        public RequestSessionViewModel(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            HttpVerb = HttpVerb.Get;
        }

        public bool IsLoading
        {
            get => isLoading;
            set { isLoading = value; OnPropertyChanged(); }
        }

        public bool IsResponseExpanded
        {
            get => isResponseExpanded;
            set { isResponseExpanded = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => status;
            set { status = value; OnPropertyChanged(); }
        }

        public string Url
        {
            get => url;
            set { url = value; SetupUrls(); OnPropertyChanged(); }
        }

        public ObservableCollection<string> Urls
        {
            get { return urls ??= []; }
        }

        public HttpVerb HttpVerb
        {
            get => httpVerb;
            set { httpVerb = value; OnPropertyChanged(); }
        }

        public string Headers
        {
            get => headers;
            set { headers = value; OnPropertyChanged(); }
        }

        public string Body
        {
            get => body;
            set { body = value; OnPropertyChanged(); }
        }

        public string ResponseHeaders
        {
            get => responseHeaders;
            set { responseHeaders = value; OnPropertyChanged(); }
        }

        public string ResponseString
        {
            get => responseString;
            set { responseString = value; OnPropertyChanged(); }
        }

        public BodyMode BodyMode
        {
            get => bodyMode;
            set
            {
                if (bodyMode == value) return;
                var oldMode = bodyMode;
                bodyMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BodyModeIndex));
                SwitchBodyMode(oldMode, value);
            }
        }

        public int BodyModeIndex
        {
            get => (int)bodyMode;
            set => BodyMode = (BodyMode)value;
        }

        public ObservableCollection<FormParamViewModel> FormParams
        {
            get => formParams ??= new ObservableCollection<FormParamViewModel>();
        }

        public ObservableCollection<MultipartParamViewModel> MultipartParams
        {
            get => multipartParams ??= new ObservableCollection<MultipartParamViewModel>();
        }

        public string Boundary => boundary ??= GenerateBoundary();

        /// <summary>
        /// True when the current Raw body could not be parsed as application/x-www-form-urlencoded.
        /// While true, the FormParams grid is not shown; the UI should offer "Reset body" instead.
        /// </summary>
        public bool IsFormBodyUnparsable
        {
            get => isFormBodyUnparsable;
            private set { isFormBodyUnparsable = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsFormBodyParsable)); }
        }

        /// <summary>Inverse of <see cref="IsFormBodyUnparsable"/>, for Visibility bindings.</summary>
        public bool IsFormBodyParsable => !IsFormBodyUnparsable;

        /// <summary>
        /// True when the current Raw body could not be parsed as multipart/form-data.
        /// While true, the MultipartParams grid is not shown; the UI should offer "Reset body" instead.
        /// </summary>
        public bool IsMultipartBodyUnparsable
        {
            get => isMultipartBodyUnparsable;
            private set { isMultipartBodyUnparsable = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsMultipartBodyParsable)); }
        }

        /// <summary>Inverse of <see cref="IsMultipartBodyUnparsable"/>, for Visibility bindings.</summary>
        public bool IsMultipartBodyParsable => !IsMultipartBodyUnparsable;

        private static string GenerateBoundary() => $"----FormBoundary{Guid.NewGuid():N}";

        // The Raw tab is the primary/source-of-truth representation of the body. The other tabs are
        // convenience editors layered on top of it: they only become editable when the current Raw body
        // can be parsed (or is empty) in that format. If it can't, we never guess/mangle it - instead the
        // tab shows a "Reset body" affordance that clears the Raw body and starts a fresh, editable form.
        private void SwitchBodyMode(BodyMode oldMode, BodyMode newMode)
        {
            if (oldMode == BodyMode.FormUrlEncoded && !IsFormBodyUnparsable)
                SyncFormParamsToBody();
            else if (oldMode == BodyMode.Multipart && !IsMultipartBodyUnparsable)
                SyncMultipartParamsToBody();

            if (newMode == BodyMode.FormUrlEncoded)
                TrySetupFormMode();
            else if (newMode == BodyMode.Multipart)
                TrySetupMultipartMode();
        }

        private void TrySetupFormMode()
        {
            FormParams.Clear();
            var raw = Body ?? "";
            if (!TryParseFormUrlEncoded(raw, out var pairs))
            {
                IsFormBodyUnparsable = true;
                return;
            }
            IsFormBodyUnparsable = false;
            foreach (var pair in pairs)
                CreateFormParam(pair.Key, pair.Value);
            SetHeader("Content-Type: application/x-www-form-urlencoded");
        }

        public Command ResetFormBodyCommand => new Command(() =>
        {
            FormParams.Clear();
            Body = "";
            IsFormBodyUnparsable = false;
            SetHeader("Content-Type: application/x-www-form-urlencoded");
        });

        private static bool TryParseFormUrlEncoded(string raw, out List<(string Key, string Value)> pairs)
        {
            pairs = new List<(string, string)>();
            if (string.IsNullOrWhiteSpace(raw)) return true;
            if (raw.Contains('\r') || raw.Contains('\n')) return false;

            var trimmed = raw.Trim();
            if (trimmed.StartsWith('{') || trimmed.StartsWith('[') || trimmed.StartsWith('<') || trimmed.StartsWith("--"))
                return false;

            foreach (var part in raw.Split('&'))
            {
                if (part.Length == 0) return false;
                var eqIdx = part.IndexOf('=');
                var key = eqIdx == -1 ? part : part[..eqIdx];
                var value = eqIdx == -1 ? "" : part[(eqIdx + 1)..];
                if (!IsValidUrlEncodedToken(key) || !IsValidUrlEncodedToken(value))
                    return false;
                var decodedKey = WebUtility.UrlDecode(key);
                if (string.IsNullOrEmpty(decodedKey))
                    return false;
                pairs.Add((decodedKey, WebUtility.UrlDecode(value)));
            }
            return true;
        }

        // Matches exactly what WebUtility.UrlEncode (used by SyncFormParamsToBody) produces, so anything
        // the form grid itself writes always round-trips back into a parseable body.
        private static bool IsValidUrlEncodedToken(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c == '%')
                {
                    if (i + 2 >= s.Length || !Uri.IsHexDigit(s[i + 1]) || !Uri.IsHexDigit(s[i + 2]))
                        return false;
                    i += 2;
                }
                else if (!char.IsLetterOrDigit(c) && c != '-' && c != '_' && c != '.' && c != '~' && c != '+')
                {
                    return false;
                }
            }
            return true;
        }

        private void CreateFormParam(string key = "", string value = "")
        {
            var param = new FormParamViewModel { Key = key, Value = value };
            param.Changed += SyncFormParamsToBody;
            FormParams.Add(param);
        }

        private void SyncFormParamsToBody()
        {
            var parts = FormParams
                .Where(p => !string.IsNullOrEmpty(p.Key))
                .Select(p => $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value ?? "")}");
            Body = string.Join("&", parts);
        }

        private void TrySetupMultipartMode()
        {
            MultipartParams.Clear();
            var raw = Body ?? "";
            if (!TryParseMultipartBody(raw, out var parsedBoundary, out var items))
            {
                IsMultipartBodyUnparsable = true;
                return;
            }
            IsMultipartBodyUnparsable = false;
            boundary = parsedBoundary ?? GenerateBoundary();
            foreach (var item in items)
            {
                if (item.IsFile)
                    CreateMultipartFileParam(item.Key, item.FilePath);
                else
                    CreateMultipartTextParam(item.Key, item.Value);
            }
            SetHeader($"Content-Type: multipart/form-data; boundary={boundary}");
        }

        public Command ResetMultipartBodyCommand => new Command(() =>
        {
            MultipartParams.Clear();
            Body = "";
            IsMultipartBodyUnparsable = false;
            boundary = GenerateBoundary();
            SetHeader($"Content-Type: multipart/form-data; boundary={boundary}");
        });

        private static bool TryParseMultipartBody(string raw, out string parsedBoundary,
            out List<(string Key, bool IsFile, string Value, string FilePath)> items)
        {
            parsedBoundary = null;
            items = new List<(string, bool, string, string)>();
            if (string.IsNullOrWhiteSpace(raw)) return true;

            var newlineIdx = raw.IndexOfAny(new[] { '\r', '\n' });
            var firstLine = (newlineIdx == -1 ? raw : raw[..newlineIdx]).Trim();
            if (!firstLine.StartsWith("--")) return false;
            var bound = firstLine[2..];
            if (string.IsNullOrEmpty(bound)) return false;

            var closing = $"--{bound}--";
            if (!raw.TrimEnd().EndsWith(closing, StringComparison.Ordinal)) return false;

            var segments = raw.Split(new[] { $"--{bound}" }, StringSplitOptions.None);
            if (segments.Length < 3) return false;
            if (!string.IsNullOrWhiteSpace(segments[0])) return false;
            if (segments[^1].TrimEnd() != "--") return false;

            foreach (var segment in segments[1..^1])
            {
                var trimmed = segment.TrimStart('\r', '\n');
                if (trimmed.Length == 0) return false;

                var lines = trimmed.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                var headerEnd = Array.IndexOf(lines, "");
                if (headerEnd <= 0) return false;

                var partHeaders = lines.Take(headerEnd).ToArray();
                var partBodyLines = lines.Skip(headerEnd + 1).ToArray();
                while (partBodyLines.Length > 0 && string.IsNullOrEmpty(partBodyLines[^1]))
                    partBodyLines = partBodyLines[..^1];
                var partBody = string.Join("\r\n", partBodyLines);

                var disposition = partHeaders.FirstOrDefault(h =>
                    h.StartsWith("Content-Disposition:", StringComparison.OrdinalIgnoreCase));
                if (disposition == null) return false;

                var nameMatch = Regex.Match(disposition, @"name=""([^""]*)""");
                if (!nameMatch.Success || string.IsNullOrEmpty(nameMatch.Groups[1].Value)) return false;
                var name = nameMatch.Groups[1].Value;

                var fileNameMatch = Regex.Match(disposition, @"filename=""([^""]*)""");
                if (fileNameMatch.Success)
                {
                    var fileMatch = Regex.Match(partBody, @"^\(file<([^>]*)>\)$");
                    if (!fileMatch.Success) return false;
                    items.Add((name, true, "", fileMatch.Groups[1].Value));
                }
                else
                {
                    items.Add((name, false, partBody, ""));
                }
            }

            parsedBoundary = bound;
            return true;
        }

        private void CreateMultipartTextParam(string key = "", string value = "")
        {
            var param = new MultipartParamViewModel { Key = key, TextValue = value, ParamType = MultipartParamType.Text };
            param.Changed += SyncMultipartParamsToBody;
            MultipartParams.Add(param);
        }

        private void CreateMultipartFileParam(string key = "", string filePath = "")
        {
            var param = new MultipartParamViewModel { Key = key, FilePath = filePath, ParamType = MultipartParamType.File };
            param.Changed += SyncMultipartParamsToBody;
            MultipartParams.Add(param);
        }

        private void SyncMultipartParamsToBody()
        {
            var relevant = MultipartParams.Where(p => !string.IsNullOrEmpty(p.Key)).ToList();
            if (!relevant.Any()) { Body = ""; return; }

            var sb = new StringBuilder();
            foreach (var param in relevant)
            {
                sb.Append($"--{Boundary}\r\n");
                if (param.IsFile)
                {
                    var fileName = string.IsNullOrEmpty(param.FilePath)
                        ? "file"
                        : Path.GetFileName(param.FilePath);
                    sb.Append($"Content-Disposition: form-data; name=\"{param.Key}\"; filename=\"{fileName}\"\r\n");
                    sb.Append("Content-Type: application/octet-stream\r\n");
                    sb.Append("\r\n");
                    sb.Append($"(file<{param.FilePath}>)\r\n");
                }
                else
                {
                    sb.Append($"Content-Disposition: form-data; name=\"{param.Key}\"\r\n");
                    sb.Append("\r\n");
                    sb.Append($"{param.TextValue ?? ""}\r\n");
                }
            }
            sb.Append($"--{Boundary}--");
            Body = sb.ToString();
        }

        private void SetHeader(string headerLine)
        {
            var header = SplitHeader(headerLine);
            if (header == null) return;
            var hdrs = GetHeaders();
            var idx = hdrs.FindIndex(x => header.Value.Key.Equals(x.Key, StringComparison.OrdinalIgnoreCase));
            if (idx == -1)
                hdrs.Add(header.Value);
            else
                hdrs[idx] = header.Value;
            var sb = new StringBuilder();
            foreach (var h in hdrs)
                sb.Append($"{h.Key}: {h.Value}").AppendLine();
            Headers = sb.ToString();
        }

        public Command SetHeaderCommand => new Command(obj =>
        {
            if (obj is not string str) return;
            var header = SplitHeader(str);
            if (header == null) return;
            var hdrs = GetHeaders();
            var idx = hdrs.FindIndex(x => header.Value.Key.Equals(x.Key, StringComparison.OrdinalIgnoreCase));
            if (idx == -1)
                hdrs.Add(header.Value);
            else
                hdrs[idx] = header.Value;
            var sb = new StringBuilder();
            foreach (var h in hdrs)
                sb.Append($"{h.Key}: {h.Value}").AppendLine();
            Headers = sb.ToString();
        });

        public Command AddFormParamCommand => new Command(() => CreateFormParam());

        public Command RemoveFormParamCommand => new Command(obj =>
        {
            if (obj is FormParamViewModel param)
            {
                param.Changed -= SyncFormParamsToBody;
                FormParams.Remove(param);
                SyncFormParamsToBody();
            }
        });

        public Command AddMultipartTextParamCommand => new Command(() => CreateMultipartTextParam());

        public Command AddMultipartFileParamCommand => new Command(() =>
        {
            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == true)
                CreateMultipartFileParam("", dlg.FileName);
            else
                CreateMultipartFileParam();
        });

        public Command RemoveMultipartParamCommand => new Command(obj =>
        {
            if (obj is MultipartParamViewModel param)
            {
                param.Changed -= SyncMultipartParamsToBody;
                MultipartParams.Remove(param);
                SyncMultipartParamsToBody();
            }
        });

        public Command BrowseFileCommand => new Command(obj =>
        {
            if (obj is MultipartParamViewModel param)
            {
                var dlg = new OpenFileDialog();
                if (dlg.ShowDialog() == true)
                    param.FilePath = dlg.FileName;
            }
        });

        public Command PrettyJsonCommand => new Command(() =>
        {
            try
            {
                if (string.IsNullOrEmpty(ResponseString)) return;
                var obj = JsonConvert.DeserializeObject<Dictionary<string, object>>(ResponseString);
                ResponseString = JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
            catch { }
        });

        public event Action<RequestHistoryItem>? RequestCompleted;

        public Command RunCommand => new Command(async () =>
        {
            IsLoading = true;
            var capturedUrl = Url;
            var capturedVerb = HttpVerb;
            var capturedHeaders = Headers;
            var capturedBody = Body;
            try
            {
                await DoRequest();
            }
            catch (Exception ex)
            {
                dispatcher.Invoke(() =>
                {
                    Status = ex.Message;
                    ResponseHeaders = "";
                    ResponseString = "";
                    var err = ex;
                    while (err != null)
                    {
                        responseString += err.Message + "\r\n";
                        responseString += err.StackTrace + "\r\n";
                        err = err.InnerException;
                    }
                    IsLoading = false;
                    IsResponseExpanded = true;
                    RequestCompleted?.Invoke(new RequestHistoryItem(capturedUrl, capturedVerb, capturedHeaders, capturedBody,
                        responseString, "", status, 0));
                });
            }
        });

        private async Task DoRequest()
        {
            var capturedUrl = Url;
            var capturedVerb = HttpVerb;
            var capturedHeaders = Headers;
            var capturedBody = Body;
            var capturedMode = BodyMode;
            var capturedBoundary = Boundary;
            // If the Multipart tab is currently showing "unparsable" (grid not populated), MultipartParams
            // is empty - fall back to sending the Raw body verbatim instead of an empty multipart payload.
            var capturedMultipartItems = capturedMode == BodyMode.Multipart && !IsMultipartBodyUnparsable
                ? MultipartParams
                    .Where(p => !string.IsNullOrEmpty(p.Key))
                    .Select(p => (Key: p.Key, IsFile: p.IsFile, Value: p.TextValue ?? "", FilePath: p.FilePath ?? ""))
                    .ToList()
                : null;

            var hdrs = GetHeaders();
            var agentIndex = GetUserAgentIndex(hdrs);
            var agentString = (string)null;
            if (agentIndex != -1)
            {
                agentString = hdrs[agentIndex].Value;
                hdrs.RemoveAt(agentIndex);
            }
            else
            {
                agentString = "Request Builder";
            }

            var helper = new NetworkHelper2(agentString);
            var request = new HttpRequest();
            foreach (var header in hdrs)
                request.AddHeader(header.Key, header.Value);
            request.Url = Url;
            request.HttpVerb = HttpVerb;

            if (capturedMode == BodyMode.Multipart && capturedMultipartItems != null)
            {
                var ms = new MemoryStream();
                foreach (var param in capturedMultipartItems)
                {
                    if (param.IsFile)
                    {
                        if (string.IsNullOrEmpty(param.FilePath) || !File.Exists(param.FilePath))
                            continue;
                        var fileName = Path.GetFileName(param.FilePath);
                        var fileData = File.ReadAllBytes(param.FilePath);
                        var hdrBytes = Encoding.UTF8.GetBytes(
                            $"--{capturedBoundary}\r\n" +
                            $"Content-Disposition: form-data; name=\"{param.Key}\"; filename=\"{fileName}\"\r\n" +
                            "Content-Type: application/octet-stream\r\n\r\n");
                        ms.Write(hdrBytes, 0, hdrBytes.Length);
                        ms.Write(fileData, 0, fileData.Length);
                        ms.Write(new byte[] { (byte)'\r', (byte)'\n' }, 0, 2);
                    }
                    else
                    {
                        var hdrBytes = Encoding.UTF8.GetBytes(
                            $"--{capturedBoundary}\r\n" +
                            $"Content-Disposition: form-data; name=\"{param.Key}\"\r\n\r\n" +
                            param.Value + "\r\n");
                        ms.Write(hdrBytes, 0, hdrBytes.Length);
                    }
                }
                var endBytes = Encoding.UTF8.GetBytes($"--{capturedBoundary}--\r\n");
                ms.Write(endBytes, 0, endBytes.Length);
                request.PostBinaryBody = ms.ToArray();
                request.ContentType = $"multipart/form-data; boundary={capturedBoundary}";
            }
            else if (HttpVerb == HttpVerb.Post || HttpVerb == HttpVerb.Put)
            {
                request.PostBody = capturedBody;
            }

            request.ProceedOnError = true;
            var result = await helper.MakeRemoteRequestAsync(request);
            RequestedUrlsCache.Instance.AddUrl(request.GetUrl());
            var headerSb = new StringBuilder();
            foreach (var header in result.ResponseHeaders)
                headerSb.Append($"{header.Key}: {header.Value}").AppendLine();
            var responseText = result.ResponseStream.ReadAsText();
            var statusStr = result.StatusCode.ToString();
            dispatcher.Invoke(new Action(() =>
            {
                ResponseHeaders = headerSb.ToString();
                ResponseString = responseText;
                Status = statusStr;
                IsLoading = false;
                IsResponseExpanded = true;
                RequestCompleted?.Invoke(new RequestHistoryItem(capturedUrl, capturedVerb, capturedHeaders, capturedBody,
                    responseText, headerSb.ToString(), statusStr, (int)result.StatusCode));
            }));
        }

        private static int GetUserAgentIndex(List<KeyValuePair<string, string>> headers)
        {
            return headers.FindIndex(x => "user-agent".Equals(x.Key, StringComparison.OrdinalIgnoreCase));
        }

        private List<KeyValuePair<string, string>> GetHeaders()
        {
            if (string.IsNullOrWhiteSpace(Headers))
                return new List<KeyValuePair<string, string>>();
            var result = new List<KeyValuePair<string, string>>();
            foreach (var item in Headers.SplitToLines(true))
            {
                var pair = SplitHeader(item);
                if (pair != null) result.Add(pair.Value);
            }
            return result;
        }

        private KeyValuePair<string, string>? SplitHeader(string header)
        {
            var colon = header.IndexOf(':');
            if (colon == -1) return null;
            var key = header[..colon];
            var value = header[(colon + 1)..].TrimStart();
            return new KeyValuePair<string, string>(key, value);
        }

        private void SetupUrls()
        {
            var list = RequestedUrlsCache.Instance.GetUrls(url);
            Urls.Clear();
            foreach (var u in list)
            {
                if (!Urls.Contains(u))
                    Urls.Add(u);
            }
            OnPropertyChanged(nameof(Urls));
        }
    }
}
