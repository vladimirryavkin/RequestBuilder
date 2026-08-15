using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RequestBuilder.FileManagement
{
    public interface IFileService
    {
        Task<bool> SaveFileAsync(Stream stream, string key);
        Task<bool> SaveFileAsync(byte[] data, string key);
        Task<bool> DeleteFileAsync(string key);
        Task<Stream> DownloadFileAsync(string key);
        Task<bool> MoveFileAsync(string fromKey, string toKey);
        Task<List<EntryInfo>> ListFoldersAsync(string path);
        Task<List<EntryInfo>> ListFoldersAndFilesAsync(string path);
        Task<List<EntryInfo>> ListFiles(string path);
    }
}
