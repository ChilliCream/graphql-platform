using System.IO;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace StrawberryShake.Tools
{
    public interface IFileSystem
    {
        string CurrentDirectory { get; }

        string ResolvePath(string? path);

        string CombinePath(params string[] path);

        void EnsureDirectoryExists(string path);

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

        Task WriteToAsync(string fileName, Func<Stream, Task> write);
    }
}
