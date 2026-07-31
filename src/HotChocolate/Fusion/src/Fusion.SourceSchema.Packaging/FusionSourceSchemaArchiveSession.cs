using System.Buffers;
using System.IO.Compression;
using HotChocolate.Fusion.Packaging.Storage;

namespace HotChocolate.Fusion.SourceSchema.Packaging;

internal sealed class FusionSourceSchemaArchiveSession : IDisposable
{
    private readonly Dictionary<string, FileEntry> _files = [];
    private readonly ZipArchive _archive;
    private readonly FusionSourceSchemaArchiveReadOptions _readOptions;
    private readonly ArchiveEntryStorageKind _storageKind;
    private FusionSourceSchemaArchiveMode _mode;
    private long _storedBytes;
    private bool _disposed;

    public FusionSourceSchemaArchiveSession(
        ZipArchive archive,
        FusionSourceSchemaArchiveMode mode,
        FusionSourceSchemaArchiveReadOptions readOptions,
        ArchiveEntryStorageKind storageKind)
    {
        ArgumentNullException.ThrowIfNull(archive);

        _archive = archive;
        _mode = mode;
        _readOptions = readOptions;
        _storageKind = storageKind;
    }

    public bool HasUncommittedChanges
        => _files.Values.Any(file => file.State is not FileState.Read);

    public IEnumerable<string> GetFiles()
    {
        var stagedFiles = _files
            .Where(file => file.Value.State is not FileState.Deleted)
            .Select(file => file.Key);

        if (_mode is FusionSourceSchemaArchiveMode.Create)
        {
            return stagedFiles;
        }

        var files = new HashSet<string>(stagedFiles);

        foreach (var entry in _archive.Entries)
        {
            files.Add(entry.FullName);
        }

        return files;
    }

    public async Task<bool> ExistsAsync(
        string path,
        FileKind kind,
        CancellationToken cancellationToken)
    {
        if (_files.TryGetValue(path, out var file))
        {
            return file.State is not FileState.Deleted;
        }

        if (_mode is not FusionSourceSchemaArchiveMode.Create && _archive.GetEntry(path) is { } entry)
        {
            file = await ExtractFileAsync(entry, path, GetAllowedSize(kind), cancellationToken);
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

        return _mode is not FusionSourceSchemaArchiveMode.Create && _archive.GetEntry(path) is not null;
    }

    public async Task<Stream> OpenReadAsync(
        string path,
        FileKind kind,
        CancellationToken cancellationToken)
    {
        if (_files.TryGetValue(path, out var file))
        {
            if (file.State is FileState.Deleted)
            {
                throw new FileNotFoundException(path);
            }

            return file.Storage.OpenRead();
        }

        if (_mode is not FusionSourceSchemaArchiveMode.Create && _archive.GetEntry(path) is { } entry)
        {
            file = await ExtractFileAsync(entry, path, GetAllowedSize(kind), cancellationToken);
            _files.Add(path, file);
            return file.Storage.OpenRead();
        }

        throw new FileNotFoundException(path);
    }

    public Stream OpenWrite(string path)
    {
        if (_mode is FusionSourceSchemaArchiveMode.Read)
        {
            throw new InvalidOperationException("Cannot write to a read-only archive.");
        }

        if (!_files.TryGetValue(path, out var file))
        {
            var state = _mode is not FusionSourceSchemaArchiveMode.Create
                && _archive.GetEntry(path) is not null
                    ? FileState.Replaced
                    : FileState.Created;
            file = new FileEntry(path, ArchiveEntryStorage.Create(_storageKind), state);
            _files.Add(path, file);
        }
        else
        {
            file.MarkMutated();
        }

        if (_storageKind is ArchiveEntryStorageKind.TempFile)
        {
            return file.Storage.OpenWrite(int.MaxValue);
        }

        var previousLength = file.Storage.Length;
        var remainingSessionBytes = checked(
            _readOptions.MaxAllowedInMemorySessionSize - _storedBytes + previousLength);
        var maximumLength = (int)Math.Min(
            Math.Max(0, remainingSessionBytes),
            GetAllowedSize(GetFileKind(path)));

        return file.Storage.OpenWrite(
            maximumLength,
            length => _storedBytes = checked(_storedBytes - previousLength + length));
    }

    public void SetMode(FusionSourceSchemaArchiveMode mode)
    {
        _mode = mode;
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        foreach (var file in _files.Values.OrderBy(f => f.Path, StringComparer.Ordinal))
        {
            switch (file.State)
            {
                case FileState.Created:
                    await CreateEntryFromStorageAsync(file, cancellationToken);
                    break;

                case FileState.Replaced:
                    _archive.GetEntry(file.Path)?.Delete();
                    await CreateEntryFromStorageAsync(file, cancellationToken);
                    break;

                case FileState.Deleted:
                    _archive.GetEntry(file.Path)?.Delete();
                    break;
            }

            file.MarkRead();
        }
    }

    private async Task CreateEntryFromStorageAsync(
        FileEntry file,
        CancellationToken cancellationToken)
    {
        var entry = _archive.CreateEntry(file.Path);
        entry.LastWriteTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

#if NET10_0_OR_GREATER
        await using var destination = await entry.OpenAsync(cancellationToken);
#else
        await using var destination = entry.Open();
#endif
        await file.Storage.CopyToAsync(destination, cancellationToken);
    }

    private async Task<FileEntry> ExtractFileAsync(
        ZipArchiveEntry zipEntry,
        string path,
        int maxAllowedSize,
        CancellationToken cancellationToken)
    {
        var file = new FileEntry(
            path,
            ArchiveEntryStorage.Create(_storageKind),
            FileState.Read);
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        var consumed = 0L;

        try
        {
            await using var readStream = zipEntry.Open();
            await using var writeStream = file.Storage.OpenWrite(
                _storageKind is ArchiveEntryStorageKind.Memory
                    ? (int)Math.Min(
                        maxAllowedSize,
                        Math.Max(0L, _readOptions.MaxAllowedInMemorySessionSize - _storedBytes))
                    : int.MaxValue);

            int read;
            while ((read = await readStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                consumed += read;

                if (consumed > maxAllowedSize)
                {
                    throw new InvalidOperationException(
                        $"File is too large and exceeds the allowed size of {maxAllowedSize}.");
                }

                if (_storageKind is ArchiveEntryStorageKind.Memory
                    && _storedBytes + consumed > _readOptions.MaxAllowedInMemorySessionSize)
                {
                    throw new InvalidOperationException(
                        "The archive exceeds the allowed in-memory session size of "
                        + $"{_readOptions.MaxAllowedInMemorySessionSize}.");
                }

                await writeStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            if (_storageKind is ArchiveEntryStorageKind.Memory)
            {
                _storedBytes += file.Storage.Length;
            }

            return file;
        }
        catch
        {
            file.Storage.Dispose();
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    private int GetAllowedSize(FileKind kind)
        => kind switch
        {
            FileKind.GraphQLSchema => _readOptions.MaxAllowedSchemaSize,
            FileKind.Settings or FileKind.Metadata => _readOptions.MaxAllowedSettingsSize,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    private static FileKind GetFileKind(string path)
        => Path.GetFileName(path) switch
        {
            FileNames.GraphQLSchema or FileNames.GraphQLSchemaExtensions => FileKind.GraphQLSchema,
            FileNames.ArchiveMetadata => FileKind.Metadata,
            FileNames.Settings => FileKind.Settings,
            _ => FileKind.Settings
        };

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var file in _files.Values)
        {
            file.Storage.Dispose();
        }

        _storedBytes = 0;
        _disposed = true;
    }

    private sealed class FileEntry(
        string path,
        ArchiveEntryStorage storage,
        FileState state)
    {
        public string Path { get; } = path;

        public ArchiveEntryStorage Storage { get; } = storage;

        public FileState State { get; private set; } = state;

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
    }

    private enum FileState
    {
        Read,
        Created,
        Replaced,
        Deleted
    }
}
