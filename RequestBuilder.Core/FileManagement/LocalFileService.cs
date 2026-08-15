using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RequestBuilder.FileManagement
{
    public class LocalFileService : IFileService
    {
        private readonly string RootFolder;

        public LocalFileService(string rootFolder)
        {
            RootFolder = rootFolder;
        }

        public Task<bool> DeleteFileAsync(string key)
        {
            var fullPath = GetFullPath(key);

            var helper = new FileSystemHelper();
            var result = helper.DeleteFileSafe(fullPath);
            return Task.FromResult(result);
        }

        public async Task<Stream> DownloadFileAsync(string key)
        {
            var fullPath = GetFullPath(key);
            if (File.Exists(fullPath))
            {
                var bytes = await File.ReadAllBytesAsync(fullPath);
                var ms = new MemoryStream(File.ReadAllBytes(fullPath));
                ms.Position = 0;
                return ms;
            }
            throw new Errors.NotFoundException("File not found");
        }

        public Task<List<EntryInfo>> ListFiles(string path)
        {
            var fullPath = GetFullPath(path);
            var dir = new DirectoryInfo(fullPath);
            var result = dir.EnumerateFiles(path).Select(x => (EntryInfo)new FileInfo
            {
                FullPath = x.FullName,
                Name = x.Name
            }).ToList();
            return Task.FromResult(result);
        }

        public Task<List<EntryInfo>> ListFoldersAndFilesAsync(string path)
        {
            var fullPath = GetFullPath(path);
            var dir = new DirectoryInfo(fullPath);
            var folders = dir.EnumerateDirectories(path).Select(x => (EntryInfo)new FolderInfo
            {
                FullPath = x.FullName,
                Name = x.Name
            }).ToList();
            var files = dir.EnumerateFiles(path).Select(x => (EntryInfo)new FileInfo
            {
                FullPath = x.FullName,
                Name = x.Name
            }).ToList();
            var result = folders.Union(files).ToList();
            return Task.FromResult(result);
        }

        public Task<List<EntryInfo>> ListFoldersAsync(string path)
        {
            var fullPath = GetFullPath(path);
            var dir = new DirectoryInfo(fullPath);
            var folders = dir.EnumerateDirectories(path).Select(x => (EntryInfo)new FolderInfo
            {
                FullPath = x.FullName,
                Name = x.Name
            }).ToList();
            return Task.FromResult(folders);
        }

        public Task<bool> MoveFileAsync(string fromKey, string toKey)
        {
            var from = GetFullPath(fromKey);
            var to = GetFullPath(toKey);
            try
            {
                File.Move(from, to);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                throw new Errors.PersistenceException("Error moving file", ex);
            }
        }

        public async Task<bool> SaveFileAsync(Stream stream, string key)
        {
            var fullPath = GetFullPath(key);
            var helper = new FileSystemHelper();
            var folder = helper.GetContainingFolder(fullPath);
            if (!Directory.Exists(folder))
            {
                new DirectoryInfo(folder).Create();
            }
            
            var file = (FileStream)null;
            try
            {
                file = File.Create(fullPath);
                await stream.CopyToAsync(file);
            }
            catch (Exception ex)
            {
                throw new Errors.PersistenceException("Error during saving", ex);
            }
            finally
            {
                file.SafeDispose();
            }
            return true;
        }

        public async Task<bool> SaveFileAsync(byte[] data, string key)
        {
            var fullPath = GetFullPath(key);
            var helper = new FileSystemHelper();
            var folder = helper.GetContainingFolder(fullPath);
            if (!Directory.Exists(folder))
            {
                new DirectoryInfo(folder).Create();
            }

            try
            {
                await File.WriteAllBytesAsync(fullPath, data);
            }
            catch (Exception ex)
            {
                throw new Errors.PersistenceException("Error during saving", ex);
            }
            return true;
        }

        private string GetFullPath(string key)
        {
            var helper = new FileSystemHelper();
            string[] parts = [RootFolder, .. helper.ExtractPartsFromWeb(key)];
            var fullPath = Path.Combine(parts);
            return fullPath;
        }
    }
}
