using System.Buffers;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Text.Json;
using HotChocolate.Fusion.SourceSchema.Packaging.Serializers;

namespace HotChocolate.Fusion.SourceSchema.Packaging;

/// <summary>
/// Provides functionality for creating, reading, and modifying a Fusion source schema archive.
/// An Fusion source schema archive is a ZIP-based container format
/// that packages a GraphQL schema and source schema settings.
/// </summary>
public sealed class FusionSourceSchemaArchive : IDisposable
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private readonly FusionSourceSchemaArchiveSession _session;
    private ZipArchive _archive;
    private FusionSourceSchemaArchiveMode _mode;
    private ArrayBufferWriter<byte>? _buffer;
    private ArchiveMetadata? _metadata;
    private bool _disposed;

    private FusionSourceSchemaArchive(
        Stream stream,
        FusionSourceSchemaArchiveMode mode,
        bool leaveOpen,
        FusionSourceSchemaArchiveReadOptions options)
    {
        _stream = stream;
        _mode = mode;
        _leaveOpen = leaveOpen;
        _archive = new ZipArchive(stream, (ZipArchiveMode)mode, leaveOpen);
        _session = new FusionSourceSchemaArchiveSession(_archive, mode, options);
    }

    /// <summary>
    /// Creates a new Fusion source schema archive with the specified filename.
    /// </summary>
    /// <param name="filename">The path to the archive file to create.</param>
    /// <returns>A new FusionSourceSchemaArchive instance in Create mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filename is null.</exception>
    public static FusionSourceSchemaArchive Create(string filename)
    {
        ArgumentNullException.ThrowIfNull(filename);
        return Create(File.Create(filename));
    }

    /// <summary>
    /// Creates a new Fusion source schema archive using the provided stream.
    /// </summary>
    /// <param name="stream">The stream to write the archive to.</param>
    /// <param name="leaveOpen">True to leave the stream open after disposal; otherwise, false.</param>
    /// <returns>A new FusionSourceSchemaArchive instance in Create mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public static FusionSourceSchemaArchive Create(Stream stream, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return new FusionSourceSchemaArchive(stream, FusionSourceSchemaArchiveMode.Create, leaveOpen, FusionSourceSchemaArchiveReadOptions.Default);
    }

    /// <summary>
    /// Opens an existing Fusion source schema archive from a file.
    /// </summary>
    /// <param name="filename">The path to the archive file to open.</param>
    /// <param name="mode">The mode to open the archive in.</param>
    /// <returns>A FusionSourceSchemaArchive instance opened in the specified mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filename is null.</exception>
    /// <exception cref="ArgumentException">Thrown when mode is invalid.</exception>
    public static FusionSourceSchemaArchive Open(
        string filename,
        FusionSourceSchemaArchiveMode mode = FusionSourceSchemaArchiveMode.Read)
    {
        ArgumentNullException.ThrowIfNull(filename);

        return mode switch
        {
            FusionSourceSchemaArchiveMode.Read => Open(File.OpenRead(filename), mode),
            FusionSourceSchemaArchiveMode.Create => Create(File.Create(filename)),
            FusionSourceSchemaArchiveMode.Update => Open(File.Open(filename, FileMode.Open, FileAccess.ReadWrite), mode),
            _ => throw new ArgumentException("Invalid mode.", nameof(mode))
        };
    }

    /// <summary>
    /// Opens a Fusion source schema archive from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the archive data.</param>
    /// <param name="mode">The mode to open the archive in.</param>
    /// <param name="leaveOpen">True to leave the stream open after disposal; otherwise, false.</param>
    /// <param name="options">The options to use when reading from the archive.</param>
    /// <returns>A FusionSourceSchemaArchive instance opened in the specified mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public static FusionSourceSchemaArchive Open(
        Stream stream,
        FusionSourceSchemaArchiveMode mode = FusionSourceSchemaArchiveMode.Read,
        bool leaveOpen = false,
        FusionSourceSchemaArchiveOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var readOptions = new FusionSourceSchemaArchiveReadOptions(
            options.MaxAllowedSchemaSize ?? FusionSourceSchemaArchiveReadOptions.Default.MaxAllowedSchemaSize,
            options.MaxAllowedSettingsSize ?? FusionSourceSchemaArchiveReadOptions.Default.MaxAllowedSettingsSize);
        return new FusionSourceSchemaArchive(stream, mode, leaveOpen, readOptions);
    }

    /// <summary>
    /// Sets the archive metadata.
    /// </summary>
    /// <param name="metadata">The metadata to store in the archive.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when metadata is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only.</exception>
    public async Task SetArchiveMetadataAsync(
        ArchiveMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureMutable();

        Exception? exception = null;

        await using var stream = _session.OpenWrite(FileNames.ArchiveMetadata);

        var writer = PipeWriter.Create(stream);

        try
        {
            ArchiveMetadataSerializer.Format(metadata, writer);
            await writer.FlushAsync(cancellationToken);
            _metadata = metadata;
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            await writer.CompleteAsync(exception);
        }
    }

    /// <summary>
    /// Gets the archive metadata.
    /// Returns null if no metadata is present in the archive.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The archive metadata or null if not present.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    public async Task<ArchiveMetadata?> GetArchiveMetadataAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_metadata is not null)
        {
            return _metadata;
        }

        if (!await _session.ExistsAsync(FileNames.ArchiveMetadata, FileKind.Metadata, cancellationToken))
        {
            return null;
        }

        var buffer = TryRentBuffer();

        try
        {
            await using var stream = await _session.OpenReadAsync(
                FileNames.ArchiveMetadata,
                FileKind.Metadata,
                cancellationToken);
            await stream.CopyToAsync(buffer, cancellationToken);
            var metadata = ArchiveMetadataSerializer.Parse(buffer.WrittenMemory);
            _metadata = metadata;
            return metadata;
        }
        finally
        {
            TryReturnBuffer(buffer);
        }
    }

    /// <summary>
    /// Sets the GraphQL schema of the source schema.
    /// </summary>
    /// <param name="schema">The GraphQL schema to store.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the GraphQL schema is empty.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only.</exception>
    public async Task SetSchemaAsync(
        ReadOnlyMemory<byte> schema,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(schema.Length, 0);
        ObjectDisposedException.ThrowIf(_disposed, this);

        EnsureMutable();

        await using (var stream = _session.OpenWrite(FileNames.GraphQLSchema))
        {
            await stream.WriteAsync(schema, cancellationToken);
        }
    }

    /// <summary>
    /// Tries to get the GraphQL schema of the source schema.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The GraphQL schema of the source schema if found, or null if not found.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    public async Task<ReadOnlyMemory<byte>?> TryGetSchemaAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        const string schemaPath = FileNames.GraphQLSchema;

        if (!_session.Exists(schemaPath))
        {
            return null;
        }

        var buffer = TryRentBuffer();

        try
        {
            await using var schemaStream = await _session.OpenReadAsync(
                schemaPath,
                FileKind.GraphQLSchema,
                cancellationToken);
            await schemaStream.CopyToAsync(buffer, cancellationToken);

            return buffer.WrittenMemory.ToArray();
        }
        finally
        {
            TryReturnBuffer(buffer);
        }
    }

    /// <summary>
    /// Sets the settings of the source schema.
    /// </summary>
    /// <param name="settings">The source schema settings to store.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only.</exception>
    public async Task SetSettingsAsync(
        JsonDocument settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ObjectDisposedException.ThrowIf(_disposed, this);

        EnsureMutable();

        await using (var stream = _session.OpenWrite(FileNames.Settings))
        {
            await using var jsonWriter = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            settings.WriteTo(jsonWriter);
            await jsonWriter.FlushAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Tries to get the settings of the source schema.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The settings of the source schema if found, or null if not found.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    public async Task<JsonDocument?> TryGetSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        const string settingsPath = FileNames.Settings;

        if (!_session.Exists(settingsPath))
        {
            return null;
        }

        var buffer = TryRentBuffer();

        try
        {
            await using var settingsStream = await _session.OpenReadAsync(
                settingsPath,
                FileKind.Settings,
                cancellationToken);
            var settings = await JsonDocument.ParseAsync(settingsStream, cancellationToken: cancellationToken);

            return settings;
        }
        finally
        {
            TryReturnBuffer(buffer);
        }
    }

    /// <summary>
    /// Commits any pending changes to the archive and flushes them to the underlying stream.
    /// After committing, the archive may transition to Update mode if the stream supports it.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the archive is read-only, or one of the following has not been set in the archive:
    /// GraphQL schema, settings or archive metadata.
    /// </exception>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_mode is FusionSourceSchemaArchiveMode.Read)
        {
            throw new InvalidOperationException("Cannot commit changes to a read-only archive.");
        }

        if (!_session.Exists(FileNames.GraphQLSchema)
            || !_session.Exists(FileNames.Settings)
            || !_session.Exists(FileNames.ArchiveMetadata))
        {
            throw new InvalidOperationException(
                "Cannot commit changes as long as one of the following has not been set: GraphQL schema, settings or archive metadata.");
        }

        if (_session.HasUncommittedChanges)
        {
            await _session.CommitAsync(cancellationToken);
#if NET10_0_OR_GREATER
            await _archive.DisposeAsync();
#else
            _archive.Dispose();
#endif

            if (_stream is { CanSeek: true, CanRead: true, CanWrite: true })
            {
                _stream.Seek(0, SeekOrigin.Begin);
                _archive = new ZipArchive(_stream, ZipArchiveMode.Update, _leaveOpen);
                _mode = FusionSourceSchemaArchiveMode.Update;
                _session.SetMode(_mode);
            }
            else
            {
                _mode = FusionSourceSchemaArchiveMode.Read;
            }
        }
    }

    /// <summary>
    /// Releases all resources used by the FusionSourceSchemaArchive.
    /// If leaveOpen was false when opening the archive, the underlying stream is also disposed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _session.Dispose();
        _archive.Dispose();

        if (!_leaveOpen)
        {
            _stream.Dispose();
        }
    }

    /// <summary>
    /// We will try to work with a single buffer for all file interactions.
    /// </summary>
    private ArrayBufferWriter<byte> TryRentBuffer()
    {
        return Interlocked.Exchange(ref _buffer, null) ?? new ArrayBufferWriter<byte>(4096);
    }

    /// <summary>
    /// Tries to preserve a used buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer that shall be preserved.
    /// </param>
    private void TryReturnBuffer(ArrayBufferWriter<byte> buffer)
    {
        buffer.Clear();

        var currentBuffer = _buffer;
        var currentCapacity = currentBuffer?.Capacity ?? 0;

        if (currentCapacity < buffer.Capacity)
        {
            Interlocked.CompareExchange(ref _buffer, buffer, currentBuffer);
        }
    }

    private void EnsureMutable()
    {
        if (_mode is FusionSourceSchemaArchiveMode.Read)
        {
            throw new InvalidOperationException("Cannot modify a read-only archive.");
        }
    }
}

file static class Extensions
{
    public static Task CopyToAsync(
        this Stream stream,
        IBufferWriter<byte> buffer,
        CancellationToken cancellationToken)
        => stream.CopyToAsync(buffer, 4096, cancellationToken);

    public static async Task CopyToAsync(
        this Stream stream,
        IBufferWriter<byte> buffer,
        int expectedStreamLength,
        CancellationToken cancellationToken)
    {
        int bytesRead;
        var bufferSize = Math.Min(expectedStreamLength, 4096);

        do
        {
            var memory = buffer.GetMemory(bufferSize);
            bytesRead = await stream.ReadAsync(memory, cancellationToken);
            if (bytesRead > 0)
            {
                buffer.Advance(bytesRead);
            }
        } while (bytesRead > 0);
    }
}
