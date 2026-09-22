using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Delegates file operations while controlling sidecar replacement and lock setup.
/// </summary>
internal sealed class ControlledSidecarReplaceFileSystem(
    IFileSystem inner,
    string sidecarPath,
    bool blockFirstReplacement = false,
    bool failReplacement = false,
    bool deleteSidecarDirectoryBeforeLock = false) : IFileSystem
{
    private readonly TaskCompletionSource _replaceStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _releaseReplace = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _hasBlocked;
    private int _hasDeletedSidecarDirectory;

    public bool FileExists(string path) => inner.FileExists(path);

    public Stream OpenReadStream(string path) => inner.OpenReadStream(path);

    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct)
        => inner.ReadAllBytesAsync(path, ct);

    public Task<string> ReadAllTextAsync(string path, CancellationToken ct)
        => inner.ReadAllTextAsync(path, ct);

    public Stream CreateFile(string path) => inner.CreateFile(path);

    public Task WriteAllTextAsync(string path, string content, CancellationToken ct)
        => inner.WriteAllTextAsync(path, content, ct);

    public Task CreateFileAtomicAsync(string path, string content, CancellationToken ct)
        => inner.CreateFileAtomicAsync(path, content, ct);

    public async Task ReplaceFileAtomicAsync(string path, string content, CancellationToken ct)
    {
        if (string.Equals(path, sidecarPath, StringComparison.Ordinal)
            && blockFirstReplacement
            && Interlocked.CompareExchange(ref _hasBlocked, 1, 0) == 0)
        {
            _replaceStarted.TrySetResult();
            await _releaseReplace.Task.WaitAsync(ct);
        }

        if (string.Equals(path, sidecarPath, StringComparison.Ordinal) && failReplacement)
        {
            throw new IOException("The replacement failed.");
        }

        await inner.ReplaceFileAtomicAsync(path, content, ct);
    }

    public void CleanupAbandonedTempFiles(string directory, TimeSpan olderThan)
        => inner.CleanupAbandonedTempFiles(directory, olderThan);

    public void DeleteFile(string path) => inner.DeleteFile(path);

    public bool DirectoryExists(string path)
    {
        var exists = inner.DirectoryExists(path);

        if (exists
            && deleteSidecarDirectoryBeforeLock
            && string.Equals(path, Path.GetDirectoryName(sidecarPath), StringComparison.Ordinal)
            && Interlocked.CompareExchange(ref _hasDeletedSidecarDirectory, 1, 0) == 0)
        {
            inner.DeleteDirectory(path, recursive: true);
        }

        return exists;
    }

    public void CreateDirectory(string path) => inner.CreateDirectory(path);

    public void MoveDirectory(string sourcePath, string targetPath)
        => inner.MoveDirectory(sourcePath, targetPath);

    public void DeleteDirectory(string path, bool recursive)
        => inner.DeleteDirectory(path, recursive);

    public string GetCurrentDirectory() => inner.GetCurrentDirectory();

    public IEnumerable<string> GetFiles(string directory, string pattern, SearchOption searchOption)
        => inner.GetFiles(directory, pattern, searchOption);

    public IEnumerable<string> GlobMatch(
        IEnumerable<string> patterns,
        IEnumerable<string>? excludes = null,
        string? workingDirectory = null)
        => inner.GlobMatch(patterns, excludes, workingDirectory);

    public Task WaitForReplaceAsync(CancellationToken cancellationToken)
        => _replaceStarted.Task.WaitAsync(cancellationToken);

    public void ReleaseReplace() => _releaseReplace.TrySetResult();
}
