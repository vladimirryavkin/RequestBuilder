using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RequestBuilder.ViewModels
{
    public class RequestedUrlsCache
    {
        private static Lazy<RequestedUrlsCache> instance = new Lazy<RequestedUrlsCache>(() => new RequestedUrlsCache());
        public static RequestedUrlsCache Instance => instance.Value;
        private IDictionary<string, int> _cache = new Dictionary<string, int>();
        private RequestedUrlsCache() { }
        public List<string> GetUrls(string prefix)
        {
            return _cache.OrderBy(x => x.Value).Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).Select(x => x.Key).ToList();
        }

        public void AddUrl(string url)
        {
            if (!_cache.ContainsKey(url))
            {
                _cache.Add(url, 0);
            }
            _cache[url]++;
        }
    }
}
