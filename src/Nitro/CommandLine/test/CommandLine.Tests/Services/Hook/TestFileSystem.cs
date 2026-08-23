using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// A real file system rooted at a fixed current directory, for tests that run
/// against actual files in a temp directory.
/// </summary>
internal sealed class TestFileSystem(string currentDirectory) : IFileSystem
{
    private readonly FileSystem _inner = new();

    public string GetCurrentDirectory() => currentDirectory;

    public bool FileExists(string path) => _inner.FileExists(path);

    public Stream OpenReadStream(string path) => _inner.OpenReadStream(path);

    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct)
        => _inner.ReadAllBytesAsync(path, ct);

    public Task<string> ReadAllTextAsync(string path, CancellationToken ct)
        => _inner.ReadAllTextAsync(path, ct);

    public Stream CreateFile(string path) => _inner.CreateFile(path);

    public Task WriteAllTextAsync(string path, string content, CancellationToken ct)
        => _inner.WriteAllTextAsync(path, content, ct);

    public Task CreateFileAtomicAsync(string path, string content, CancellationToken ct)
        => _inner.CreateFileAtomicAsync(path, content, ct);

    public Task ReplaceFileAtomicAsync(string path, string content, CancellationToken ct)
        => _inner.ReplaceFileAtomicAsync(path, content, ct);

    public void CleanupAbandonedTempFiles(string directory, TimeSpan olderThan)
        => _inner.CleanupAbandonedTempFiles(directory, olderThan);

    public void DeleteFile(string path) => _inner.DeleteFile(path);

    public bool DirectoryExists(string path) => _inner.DirectoryExists(path);

    public void CreateDirectory(string path) => _inner.CreateDirectory(path);

    public IEnumerable<string> GetFiles(
        string directory,
        string pattern,
        SearchOption searchOption)
        => _inner.GetFiles(directory, pattern, searchOption);

    public IEnumerable<string> GlobMatch(
        IEnumerable<string> patterns,
        IEnumerable<string>? excludes = null,
        string? workingDirectory = null)
        => _inner.GlobMatch(patterns, excludes, workingDirectory ?? currentDirectory);
}
