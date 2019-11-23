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

        string CombinePath(params string[] path);

        void EnsureDirectoryExists(string path);

        string GetFileNameWithoutExtension(string path);

        string GetFileName(string path);

        /*
        foreach (string configFile in Directory.GetFiles(
                Environment.CurrentDirectory,
                WellKnownFiles.Config,
                SearchOption.AllDirectories))
            {
                string directory = IOPath.GetDirectoryName(configFile)!;
                if (Directory.GetFiles(directory, "*.graphql").Length > 0)
                {
        */
        IEnumerable<string> GetClientDirectories(string path);

        IEnumerable<string> GetGraphQLFiles(string path);

        Task WriteToAsync(string fileName, Func<Stream, Task> write);

        Task<byte[]> ReadAllBytesAsync(string fileName);
    }
}
