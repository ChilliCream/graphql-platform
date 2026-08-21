namespace ChilliCream.Nitro.CommandLine.Services;

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

    // Atomic file write: content is written to a temp file in the same
    // directory as the destination, then moved into place. The move is the
    // only step that touches the destination path, so a crash or
    // cancellation before it leaves the destination untouched and the temp
    // file abandoned.
    //
    // CreateFileAtomicAsync fails (throws IOException) if the destination
    // already exists; it never overwrites. ReplaceFileAtomicAsync always
    // moves into place, overwriting any existing destination; concurrent
    // replacements of the same path are last-writer-wins, the last move to
    // land is what readers see.
    Task CreateFileAtomicAsync(string path, string content, CancellationToken ct);
    Task ReplaceFileAtomicAsync(string path, string content, CancellationToken ct);

    // Removes leftover temp files an atomic write abandoned (process killed
    // between writing the temp file and moving it into place). Only temp
    // files last written before the given age are removed, so a temp file
    // from a write still in flight is left alone. Best-effort: per-file
    // errors are swallowed.
    void CleanupAbandonedTempFiles(string directory, TimeSpan olderThan);

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
        IEnumerable<string>? excludes = null,
        string? workingDirectory = null);
}
