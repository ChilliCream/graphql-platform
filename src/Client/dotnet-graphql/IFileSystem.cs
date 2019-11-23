using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace StrawberryShake.Tools
{
    public interface IFileSystem
    {
        string CurrentDirectory { get; }

        string ResolvePath(string? path);

        string CombinePath(params string[] paths);

        void EnsureDirectoryExists(string path);

        string GetFileNameWithoutExtension(string path);

        string GetFileName(string path);

        bool FileExists(string path);

        IEnumerable<string> GetClientDirectories(string path);

        IEnumerable<string> GetGraphQLFiles(string path);

        Task WriteToAsync(string fileName, Func<Stream, Task> write);

        Task<byte[]> ReadAllBytesAsync(string fileName);
    }
}
