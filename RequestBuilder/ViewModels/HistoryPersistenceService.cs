using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace RequestBuilder.ViewModels
{
    public static class HistoryPersistenceService
    {
        private class MetaData
        {
            public string Url { get; set; }
            public string HttpVerb { get; set; }
            public string Status { get; set; }
            public int StatusCode { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private static string GetHistoryDir()
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history");
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static void SaveItem(RequestHistoryItem item)
        {
            try
            {
                var folderName = item.Timestamp.ToString("yyyyMMdd_HHmmss_fff");
                var folder = Path.Combine(GetHistoryDir(), folderName);
                Directory.CreateDirectory(folder);

                var meta = new MetaData
                {
                    Url = item.Url,
                    HttpVerb = item.HttpVerb.ToString(),
                    Status = item.Status,
                    StatusCode = item.StatusCode,
                    Timestamp = item.Timestamp
                };
                File.WriteAllText(Path.Combine(folder, "meta.json"),
                    JsonConvert.SerializeObject(meta, Formatting.Indented));

                File.WriteAllText(Path.Combine(folder, "request_headers.txt"), item.Headers ?? string.Empty);
                File.WriteAllText(Path.Combine(folder, "request_body.txt"), item.Body ?? string.Empty);
                File.WriteAllText(Path.Combine(folder, "response_headers.txt"), item.ResponseHeaders ?? string.Empty);
                File.WriteAllText(Path.Combine(folder, "response_body.txt"), item.ResponseString ?? string.Empty);

                item.FolderPath = folder;
            }
            catch { }
        }

        public static void DeleteItem(RequestHistoryItem item)
        {
            try
            {
                if (!string.IsNullOrEmpty(item.FolderPath) && Directory.Exists(item.FolderPath))
                    Directory.Delete(item.FolderPath, recursive: true);
            }
            catch { }
        }

        public static List<RequestHistoryItem> Load()
        {
            var result = new List<RequestHistoryItem>();
            try
            {
                foreach (var folder in Directory.GetDirectories(GetHistoryDir()))
                {
                    try
                    {
                        var metaPath = Path.Combine(folder, "meta.json");
                        if (!File.Exists(metaPath))
                            continue;

                        var meta = JsonConvert.DeserializeObject<MetaData>(File.ReadAllText(metaPath));
                        if (meta == null)
                            continue;

                        result.Add(new RequestHistoryItem
                        {
                            Url = meta.Url,
                            HttpVerb = Enum.Parse<HttpVerb>(meta.HttpVerb),
                            Status = meta.Status,
                            StatusCode = meta.StatusCode,
                            Timestamp = meta.Timestamp,
                            Headers = ReadFile(folder, "request_headers.txt"),
                            Body = ReadFile(folder, "request_body.txt"),
                            ResponseHeaders = ReadFile(folder, "response_headers.txt"),
                            ResponseString = ReadFile(folder, "response_body.txt"),
                            FolderPath = folder
                        });
                    }
                    catch { }
                }
            }
            catch { }

            result.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
            return result;
        }

        private static string ReadFile(string folder, string filename)
        {
            var path = Path.Combine(folder, filename);
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
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
