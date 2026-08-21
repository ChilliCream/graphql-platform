using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Tests.Memory;

/// <summary>
/// Wraps a real <see cref="TestFileSystem"/> (sealed, so composition instead
/// of inheritance) to simulate losing a create race on one specific path:
/// the rival's content is written first, via the same atomic primitive a
/// real promote uses, then an <see cref="IOException"/> is thrown the way
/// the real file system would for a create attempt against a path that
/// already exists.
/// </summary>
internal sealed class RacingFileSystem(TestFileSystem inner, string racedPath, string rivalContent) : IFileSystem
{
    public bool FileExists(string path) => inner.FileExists(path);

    public Stream OpenReadStream(string path) => inner.OpenReadStream(path);

    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct)
        => inner.ReadAllBytesAsync(path, ct);

    public Task<string> ReadAllTextAsync(string path, CancellationToken ct)
        => inner.ReadAllTextAsync(path, ct);

    public Stream CreateFile(string path) => inner.CreateFile(path);

    public Task WriteAllTextAsync(string path, string content, CancellationToken ct)
        => inner.WriteAllTextAsync(path, content, ct);

    public async Task CreateFileAtomicAsync(string path, string content, CancellationToken ct)
    {
        if (path != racedPath)
        {
            await inner.CreateFileAtomicAsync(path, content, ct);
            return;
        }

        // A rival wins the create race for this exact path: its content
        // lands first, via the same atomic primitive a real promote uses.
        await inner.CreateFileAtomicAsync(path, rivalContent, ct);

        throw new IOException("Simulated lost create race: rival already created the file.");
    }

    public Task ReplaceFileAtomicAsync(string path, string content, CancellationToken ct)
        => inner.ReplaceFileAtomicAsync(path, content, ct);

    public void CleanupAbandonedTempFiles(string directory, TimeSpan olderThan)
        => inner.CleanupAbandonedTempFiles(directory, olderThan);

    public void DeleteFile(string path) => inner.DeleteFile(path);

    public bool DirectoryExists(string path) => inner.DirectoryExists(path);

    public void CreateDirectory(string path) => inner.CreateDirectory(path);

    public string GetCurrentDirectory() => inner.GetCurrentDirectory();

    public IEnumerable<string> GetFiles(string directory, string pattern, SearchOption searchOption)
        => inner.GetFiles(directory, pattern, searchOption);

    public IEnumerable<string> GlobMatch(
        IEnumerable<string> patterns,
        IEnumerable<string>? excludes = null,
        string? workingDirectory = null)
        => inner.GlobMatch(patterns, excludes, workingDirectory);
}
