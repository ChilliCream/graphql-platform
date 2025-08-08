using System.IO.Compression;

namespace HotChocolate.Fusion.Packaging;

internal sealed class ArchiveSession : IDisposable
{
    private readonly Dictionary<string, FileEntry> _files = [];
    private readonly ZipArchive _archive;
    private FusionArchiveMode _mode;
    private bool _disposed;

    public ArchiveSession(ZipArchive archive, FusionArchiveMode mode)
    {
        ArgumentNullException.ThrowIfNull(archive);

        _archive = archive;
        _mode = mode;
    }

    public bool HasUncommittedChanges
        => _files.Values.Any(file => file.State is not FileState.Read);

    public IEnumerable<string> GetFiles()
    {
        var tempFiles = _files.Where(file => file.Value.State is not FileState.Deleted).Select(file => file.Key);

        if (_mode is FusionArchiveMode.Create)
        {
            return tempFiles;
        }

        var files = new HashSet<string>(tempFiles);

        foreach (var entry in _archive.Entries)
        {
            files.Add(entry.FullName);
        }

        return files;
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken)
    {
        if (_files.TryGetValue(path, out var file))
        {
            return file.State is not FileState.Deleted;
        }

        if (_mode is not FusionArchiveMode.Create && _archive.GetEntry(path) is { } entry)
        {
            file = FileEntry.Read(path);
#if NET10_0_OR_GREATER
            await entry.ExtractToFileAsync(file.TempPath, cancellationToken);
#else
            entry.ExtractToFile(file.TempPath);
            await Task.CompletedTask;
#endif
            _files.Add(path, file);
            return true;
        }

        return false;
    }

    public bool Exists(string path)
    {
        if (_files.TryGetValue(path, out var file))
        {
            return file.State is not FileState.Deleted;
        }

        return _mode is not FusionArchiveMode.Create && _archive.GetEntry(path) is not null;
    }

    public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken)
    {
        if (_files.TryGetValue(path, out var file))
        {
            if (file.State is FileState.Deleted)
            {
                throw new FileNotFoundException(path);
            }

            return File.OpenRead(file.TempPath);
        }

        if (_mode is not FusionArchiveMode.Create && _archive.GetEntry(path) is { } entry)
        {
            file = FileEntry.Read(path);
#if NET10_0_OR_GREATER
            await entry.ExtractToFileAsync(file.TempPath, cancellationToken);
#else
            entry.ExtractToFile(file.TempPath);
            await Task.CompletedTask;
#endif
            var stream = File.OpenRead(file.TempPath);
            _files.Add(path, file);
            return stream;
        }

        throw new FileNotFoundException(path);
    }

    public Stream OpenWrite(string path)
    {
        if (_mode is FusionArchiveMode.Read)
        {
            throw new InvalidOperationException("Cannot write to a read-only archive.");
        }

        if (_files.TryGetValue(path, out var file))
        {
            file.MarkMutated();
            return File.Open(file.TempPath, FileMode.Create, FileAccess.Write);
        }

        if (_mode is not FusionArchiveMode.Create && _archive.GetEntry(path) is not null)
        {
            file = FileEntry.Read(path);
            file.MarkMutated();
        }

        file ??= FileEntry.Created(path);
        var stream = File.Open(file.TempPath, FileMode.Create, FileAccess.Write);
        _files.Add(path, file);
        return stream;
    }

    public void SetMode(FusionArchiveMode mode)
    {
        _mode = mode;
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        foreach (var file in _files.Values)
        {
#if NET10_0_OR_GREATER
            switch (file.State)
            {
                case FileState.Created:
                    await _archive.CreateEntryFromFileAsync(
                        file.TempPath,
                        file.Path,
                        cancellationToken: cancellationToken);
                    break;

                case FileState.Replaced:
                    _archive.GetEntry(file.Path)?.Delete();
                    await _archive.CreateEntryFromFileAsync(
                        file.TempPath,
                        file.Path,
                        cancellationToken);
                    break;

                case FileState.Deleted:
                    _archive.GetEntry(file.Path)?.Delete();
                    break;
            }
#else
            switch (file.State)
            {
                case FileState.Created:
                    _archive.CreateEntryFromFile(file.TempPath, file.Path);
                    break;

                case FileState.Replaced:
                    _archive.GetEntry(file.Path)?.Delete();
                    _archive.CreateEntryFromFile(file.TempPath, file.Path);
                    break;

                case FileState.Deleted:
                    _archive.GetEntry(file.Path)?.Delete();
                    break;
            }

            await Task.CompletedTask;
#endif

            file.MarkRead();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var file in _files.Values)
        {
            if (file.State is not FileState.Deleted)
            {
                File.Delete(file.TempPath);
            }
        }
    }

    private class FileEntry
    {
        private FileEntry(string path, string tempPath, FileState state)
        {
            Path = path;
            TempPath = tempPath;
            State = state;
        }

        public string Path { get; }

        public string TempPath { get; }

        public FileState State { get; private set; }

        public void MarkMutated()
        {
            if (State is FileState.Read or FileState.Deleted)
            {
                State = FileState.Replaced;
            }
        }

        public void MarkRead()
        {
            State = FileState.Read;
        }

        public static FileEntry Created(string path)
            => new(path, GetRandomTempFileName(), FileState.Created);

        public static FileEntry Read(string path)
            => new(path, GetRandomTempFileName(), FileState.Read);

        private static string GetRandomTempFileName()
        {
            var tempDir = System.IO.Path.GetTempPath();
            var fileName = System.IO.Path.GetRandomFileName();
            return System.IO.Path.Combine(tempDir, fileName);
        }
    }

    private enum FileState
    {
        Read,
        Created,
        Replaced,
        Deleted
    }
}
