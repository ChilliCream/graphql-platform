namespace StrawberryShake.Tools;

public interface IFileSystem
{
    string CurrentDirectory { get; }

    string ResolvePath(string? path, string? fileName = null);

    string CombinePath(params string[] paths);

    void EnsureDirectoryExists(string path);

    string? GetDirectoryName(string path);

    string GetFileNameWithoutExtension(string path);

    string GetFileName(string path);

    bool FileExists(string path);

    IEnumerable<string> GetClientDirectories(string path);

    IEnumerable<string> GetGraphQLFiles(string path);

    Task WriteToAsync(string fileName, Func<Stream, Task> write);

    Task WriteTextAsync(string fileName, string text);

    Task<byte[]> ReadAllBytesAsync(string fileName);
}
