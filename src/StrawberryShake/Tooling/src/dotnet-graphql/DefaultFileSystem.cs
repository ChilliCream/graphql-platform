using System.Text;

namespace StrawberryShake.Tools;

public class DefaultFileSystem : IFileSystem
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
        foreach (var configFile in Directory.GetFiles(
            Environment.CurrentDirectory,
            WellKnownFiles.Config,
            SearchOption.AllDirectories))
        {
            var directory = Path.GetDirectoryName(configFile)!;
            if (Directory.GetFiles(directory, _graphQLFilter).Length > 0)
            {
                yield return directory;
            }
        }
    }

    public string GetFileName(string path) => Path.GetFileName(path);

    public string GetFileNameWithoutExtension(string path) =>
        Path.GetFileNameWithoutExtension(path);

    public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);

    public IEnumerable<string> GetGraphQLFiles(string path) =>
        Directory.GetFiles(path, _graphQLFilter);

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

    public Task WriteTextAsync(string fileName, string text) =>
        Task.Run(() => File.WriteAllText(
            fileName,
            text,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)));

    public Task<byte[]> ReadAllBytesAsync(string fileName) =>
        Task.Run(() => File.ReadAllBytes(fileName));

    public async Task WriteToAsync(string fileName, Func<Stream, Task> write)
    {
        if (File.Exists(fileName))
        {
            File.Delete(fileName);
        }

        using (var stream = File.Create(fileName))
        {
            await write(stream).ConfigureAwait(false);
        }
    }
}
