namespace ChilliCream.Nitro.CommandLine.Helpers;

internal interface IFileSystem
{
    // File read
    bool FileExists(string path);
    Stream OpenReadStream(string path);
    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct);
    Task<string> ReadAllTextAsync(string path, CancellationToken ct);

    // File write
    Stream CreateFile(string path);
    Task WriteAllTextAsync(string path, string content, CancellationToken ct);

    // File delete
    void DeleteFile(string path);

    // Directory
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    string GetCurrentDirectory();
    IEnumerable<string> GetFiles(string directory, string pattern, SearchOption searchOption);

    // Glob
    IEnumerable<string> GlobMatch(
        IEnumerable<string> patterns,
        IEnumerable<string>? excludes = null);
}
