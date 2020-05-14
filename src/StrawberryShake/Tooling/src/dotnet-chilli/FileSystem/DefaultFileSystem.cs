using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using StrawberryShake.Tools.Abstractions;
using StrawberryShake.Tools.Config;

namespace StrawberryShake.Tools.FileSystem
{
    public class DefaultFileSystem
        : IFileSystem
    {
        private const string GraphQLFilter = "*.graphql";

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
                if (Directory.GetFiles(directory, GraphQLFilter).Length > 0)
                {
                    yield return directory;
                }
            }
        }

        public string GetFileName(string path) => Path.GetFileName(path);

        public string GetFileNameWithoutExtension(string path) =>
            Path.GetFileNameWithoutExtension(path);

        public string GetDirectoryName(string path) => Path.GetDirectoryName(path);

        public IEnumerable<string> GetGraphQLFiles(string path) =>
            Directory.GetFiles(path, GraphQLFilter);

        public string ResolvePath(string? path, string? fileName)
        {
            if (path is { })
            {
                return path;
            }

            if (fileName is { })
            {
                return Path.Combine(Environment.CurrentDirectory, fileName);
            }

            return Environment.CurrentDirectory;
        }

        public Task<byte[]> ReadAllBytesAsync(string fileName) =>
            Task.Run(() => File.ReadAllBytes(fileName));

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
