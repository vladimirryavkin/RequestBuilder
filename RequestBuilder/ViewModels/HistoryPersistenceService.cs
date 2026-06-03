using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace RequestBuilder.ViewModels
{
    public static class HistoryPersistenceService
    {
        private static string GetHistoryFilePath()
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "history.json");
        }

        public static List<RequestHistoryItem> Load()
        {
            try
            {
                var path = GetHistoryFilePath();
                if (!File.Exists(path))
                    return new List<RequestHistoryItem>();
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<List<RequestHistoryItem>>(json)
                       ?? new List<RequestHistoryItem>();
            }
            catch
            {
                return new List<RequestHistoryItem>();
            }
        }

        public static void Save(IEnumerable<RequestHistoryItem> items)
        {
            try
            {
                var json = JsonConvert.SerializeObject(items, Formatting.Indented);
                File.WriteAllText(GetHistoryFilePath(), json);
            }
            catch { }
        }

        public static string FormatRequest(RequestHistoryItem item)
        {
            var sb = new StringBuilder();
            var method = item.HttpVerb.ToString().ToUpper();

            var path = "/";
            var host = item.Url ?? string.Empty;
            try
            {
                var uri = new Uri(item.Url ?? string.Empty);
                path = uri.PathAndQuery;
                host = uri.Host;
                if (!uri.IsDefaultPort)
                    host += $":{uri.Port}";
            }
            catch { }

            sb.AppendLine($"{method} {path} HTTP/1.1");
            sb.AppendLine($"Host: {host}");

            if (!string.IsNullOrWhiteSpace(item.Headers))
            {
                foreach (var line in item.Headers.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!line.StartsWith("Host:", StringComparison.OrdinalIgnoreCase))
                        sb.AppendLine(line.TrimEnd());
                }
            }

            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(item.Body))
                sb.Append(item.Body);

            return sb.ToString();
        }

        public static string FormatResponse(RequestHistoryItem item)
        {
            var sb = new StringBuilder();

            if (item.StatusCode > 0)
                sb.AppendLine($"HTTP/1.1 {item.StatusCode} {item.Status}");
            else
                sb.AppendLine($"ERROR: {item.Status}");

            if (!string.IsNullOrWhiteSpace(item.ResponseHeaders))
            {
                foreach (var line in item.ResponseHeaders.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                    sb.AppendLine(line.TrimEnd());
            }

            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(item.ResponseString))
                sb.Append(item.ResponseString);

            return sb.ToString();
        }
    }
}
