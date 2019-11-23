using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace StrawberryShake.Tools
{
    public class DefaultFileSystem
        : IFileSystem
    {
        private const string _graphQLFilter = "*.graphql";

        public string CurrentDirectory => Environment.CurrentDirectory;

        public string CombinePath(params string[] paths) => Path.Combine(paths);

        public void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public bool FileExists(string path) => File.Exists(path);

        public IEnumerable<string> GetClientDirectories(string path)
        {
            foreach (string configFile in Directory.GetFiles(
                Environment.CurrentDirectory,
                WellKnownFiles.Config,
                SearchOption.AllDirectories))
            {
                string directory = Path.GetDirectoryName(configFile)!;
                if (Directory.GetFiles(directory, _graphQLFilter).Length > 0)
                {
                    yield return directory;
                }
            }
        }

        public string GetFileName(string path) => Path.GetFileName(path);

        public string GetFileNameWithoutExtension(string path) =>
            Path.GetFileNameWithoutExtension(path);

        public IEnumerable<string> GetGraphQLFiles(string path) =>
            Directory.GetFiles(path, _graphQLFilter);

        public string ResolvePath(string? path) =>
            path is { } ? path : Environment.CurrentDirectory;

        public Task<byte[]> ReadAllBytesAsync(string fileName) =>
            File.ReadAllBytesAsync(fileName);

        public async Task WriteToAsync(string fileName, Func<Stream, Task> write)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            using (FileStream stream = File.Create(fileName))
            {
                await write(stream);
            }
        }
    }
}
