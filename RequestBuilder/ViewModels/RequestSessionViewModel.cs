using System.Text;
using System.Windows.Threading;

namespace RequestBuilder.ViewModels
{
    public class RequestSessionViewModel : BaseViewModel
    {
        private string url;
        private HttpVerb httpVerb;
        private string headers;
        private string body;
        private string responseHeaders;
        private string responseString;
        private string status;
        private Dispatcher dispatcher;

        public RequestSessionViewModel(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public string Status
        {
            get => status;
            set
            {
                status = value;
                OnPropertyChanged();
            }
        }
        public string Url
        {
            get => url;
            set
            {
                url = value;
                OnPropertyChanged();
            }
        }
        public HttpVerb HttpVerb
        {
            get => httpVerb;
            set
            {
                httpVerb = value;
                OnPropertyChanged();
            }
        }
        public string Headers
        {
            get => headers;
            set
            {
                headers = value;
                OnPropertyChanged();
            }
        }
        public string Body
        {
            get => body;
            set
            {
                body = value;
                OnPropertyChanged();
            }
        }
        public string ResponseHeaders
        {
            get => responseHeaders;
            set
            {
                responseHeaders = value;
                OnPropertyChanged();
            }
        }
        public string ResponseString
        {
            get => responseString;
            set
            {
                responseString = value;
                OnPropertyChanged();
            }
        }

        public Command RunCommand => new Command(async () =>
        {
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
                });
            }
        });

        private async Task DoRequest()
        {
            var headers = GetHeaders();
            var agentIndex = headers.FindIndex(x => "user-agent".Equals(x.Key, StringComparison.OrdinalIgnoreCase));
            var agentString = (string)null;
            if (agentIndex != -1)
            {
                agentString = headers[agentIndex].Value;
                headers.RemoveAt(agentIndex);
            }
            else
            {
                agentString = "Request Builder";
            }
            var helper = new NetworkHelper2(agentString);
            var request = new HttpRequest();
            foreach (var header in headers)
            {
                request.AddHeader(header.Key, header.Value);
            }
            request.Url = Url;
            request.HttpVerb = HttpVerb;
            if (HttpVerb == HttpVerb.Post || HttpVerb == HttpVerb.Put)
            {
                request.PostBody = Body;
            }
            request.ProceedOnError = true;
            var result = await helper.MakeRemoteRequestAsync(request);
            var headerSb = new StringBuilder();
            foreach (var header in result.ResponseHeaders)
            {
                headerSb.Append($"{header.Key}: {header.Value}").AppendLine();
            }
            var responseText = result.ResponseStream.ReadAsText();
            var status = result.StatusCode.ToString();
            dispatcher.Invoke(new Action(() =>
            {
                ResponseHeaders = headerSb.ToString();
                ResponseString = responseText;
                Status = status;
            }));
        }

        private List<KeyValuePair<string, string>> GetHeaders()
        {
            var result = new List<KeyValuePair<string, string>>();
            var headersRaw = Headers.SplitToLines(true).ToList();
            foreach (var item in headersRaw)
            {
                var colon = item.IndexOf(':');
                if (colon == -1)
                    continue;
                var key = item.Substring(0, colon);
                var value = item.Substring(colon + 1).TrimStart();
                result.Add(key, value);
            }
            return result;
        }
    }
}