using System.Buffers;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Text.Json;
using HotChocolate.Adapters.OpenApi.Packaging.Serializers;

namespace HotChocolate.Adapters.OpenApi.Packaging;

/// <summary>
/// Provides functionality for creating, reading, and modifying OpenAPI collection files.
/// An OpenAPI collection is a ZIP-based container format that packages OpenAPI endpoint and model definitions.
/// </summary>
public sealed class OpenApiCollectionArchive : IDisposable
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private readonly OpenApiCollectionArchiveSession _session;
    private ZipArchive _archive;
    private OpenApiCollectionArchiveMode _mode;
    private ArrayBufferWriter<byte>? _buffer;
    private ArchiveMetadata? _metadata;
    private bool _disposed;

    private OpenApiCollectionArchive(
        Stream stream,
        OpenApiCollectionArchiveMode mode,
        bool leaveOpen,
        OpenApiCollectionArchiveReadOptions options)
    {
        _stream = stream;
        _mode = mode;
        _leaveOpen = leaveOpen;
        _archive = new ZipArchive(stream, (ZipArchiveMode)mode, leaveOpen);
        _session = new OpenApiCollectionArchiveSession(_archive, mode, options);
    }

    /// <summary>
    /// Creates a new OpenAPI collection archive with the specified filename.
    /// </summary>
    /// <param name="filename">The path to the archive file to create.</param>
    /// <returns>A new OpenApiCollectionArchive instance in Create mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filename is null.</exception>
    public static OpenApiCollectionArchive Create(string filename)
    {
        ArgumentNullException.ThrowIfNull(filename);
        return Create(File.Create(filename));
    }

    /// <summary>
    /// Creates a new OpenAPI collection archive using the provided stream.
    /// </summary>
    /// <param name="stream">The stream to write the archive to.</param>
    /// <param name="leaveOpen">True to leave the stream open after disposal; otherwise, false.</param>
    /// <returns>A new OpenApiCollectionArchive instance in Create mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public static OpenApiCollectionArchive Create(Stream stream, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return new OpenApiCollectionArchive(stream, OpenApiCollectionArchiveMode.Create, leaveOpen, OpenApiCollectionArchiveReadOptions.Default);
    }

    /// <summary>
    /// Opens an existing OpenAPI collection archive from a file.
    /// </summary>
    /// <param name="filename">The path to the archive file to open.</param>
    /// <param name="mode">The mode to open the archive in.</param>
    /// <returns>A OpenApiCollectionArchive instance opened in the specified mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filename is null.</exception>
    /// <exception cref="ArgumentException">Thrown when mode is invalid.</exception>
    public static OpenApiCollectionArchive Open(
        string filename,
        OpenApiCollectionArchiveMode mode = OpenApiCollectionArchiveMode.Read)
    {
        ArgumentNullException.ThrowIfNull(filename);

        return mode switch
        {
            OpenApiCollectionArchiveMode.Read => Open(File.OpenRead(filename), mode),
            OpenApiCollectionArchiveMode.Create => Create(File.Create(filename)),
            OpenApiCollectionArchiveMode.Update => Open(File.Open(filename, FileMode.Open, FileAccess.ReadWrite), mode),
            _ => throw new ArgumentException("Invalid mode.", nameof(mode))
        };
    }

    /// <summary>
    /// Opens an OpenAPI collection archive from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the archive data.</param>
    /// <param name="mode">The mode to open the archive in.</param>
    /// <param name="leaveOpen">True to leave the stream open after disposal; otherwise, false.</param>
    /// <param name="options">The options to use when reading from the archive.</param>
    /// <returns>An OpenAPI collection archive instance opened in the specified mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public static OpenApiCollectionArchive Open(
        Stream stream,
        OpenApiCollectionArchiveMode mode = OpenApiCollectionArchiveMode.Read,
        bool leaveOpen = false,
        OpenApiCollectionArchiveOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var readOptions = new OpenApiCollectionArchiveReadOptions(
            options.MaxAllowedOperationSize ?? OpenApiCollectionArchiveReadOptions.Default.MaxAllowedOperationSize,
            options.MaxAllowedSettingsSize ?? OpenApiCollectionArchiveReadOptions.Default.MaxAllowedSettingsSize);
        return new OpenApiCollectionArchive(stream, mode, leaveOpen, readOptions);
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
    /// Gets the archive metadata containing format version and schema information.
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
    /// Adds an OpenAPI endpoint to the archive.
    /// </summary>
    /// <param name="key">The unique key of this endpoint.</param>
    /// <param name="document">The GraphQL document to store.</param>
    /// <param name="settings">The settings document for this endpoint.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the GraphQL document is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only, metadata is not set, or endpoint already exists.</exception>
    public async Task AddOpenApiEndpointAsync(
        OpenApiEndpointKey key,
        ReadOnlyMemory<byte> document,
        JsonDocument settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(document.Length, 0);
        ArgumentNullException.ThrowIfNull(settings);
        ObjectDisposedException.ThrowIf(_disposed, this);

        EnsureMutable();

        var metadata = await GetArchiveMetadataAsync(cancellationToken);

        if (metadata is null)
        {
            throw new InvalidOperationException(
                "You need to first define the archive metadata.");
        }

        if (metadata.Endpoints.Contains(key))
        {
            throw new InvalidOperationException(
                $"An endpoint with HTTP method '{key.HttpMethod}' and route '{key.Route}' already exists in the archive.");
        }

        await using (var stream = _session.OpenWrite(FileNames.GetEndpointOperationPath(key)))
        {
            await stream.WriteAsync(document, cancellationToken);
        }

        await using (var stream = _session.OpenWrite(FileNames.GetEndpointSettingsPath(key)))
        {
            await using var jsonWriter = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            settings.WriteTo(jsonWriter);
            await jsonWriter.FlushAsync(cancellationToken);
        }

        _metadata = metadata with { Endpoints = metadata.Endpoints.Add(key) };
        await SetArchiveMetadataAsync(_metadata, cancellationToken);
    }

    /// <summary>
    /// Tries to get an OpenAPI endpoint by name.
    /// </summary>
    /// <param name="key">The key of the endpoint to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The OpenAPI endpoint if found, or null if not found.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    public async Task<OpenApiEndpoint?> TryGetOpenApiEndpointAsync(
        OpenApiEndpointKey key,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var metadata = await GetArchiveMetadataAsync(cancellationToken);

        if (metadata?.Endpoints.Contains(key) != true)
        {
            return null;
        }

        var operationPath = FileNames.GetEndpointOperationPath(key);
        var settingsPath = FileNames.GetEndpointSettingsPath(key);

        if (!_session.Exists(operationPath) || !_session.Exists(settingsPath))
        {
            return null;
        }

        var buffer = TryRentBuffer();

        try
        {
            await using var operationStream = await _session.OpenReadAsync(
                operationPath,
                FileKind.Operation,
                cancellationToken);
            await operationStream.CopyToAsync(buffer, cancellationToken);
            var operation = buffer.WrittenMemory.ToArray();
            buffer.Clear();

            await using var settingsStream = await _session.OpenReadAsync(
                settingsPath,
                FileKind.Settings,
                cancellationToken);
            var settings = await JsonDocument.ParseAsync(settingsStream, cancellationToken: cancellationToken);

            return new OpenApiEndpoint(operation, settings);
        }
        finally
        {
            TryReturnBuffer(buffer);
        }
    }

    /// <summary>
    /// Adds an OpenAPI model to the archive.
    /// </summary>
    /// <param name="name">The unique name for this model.</param>
    /// <param name="document">The GraphQL document to store.</param>
    /// <param name="settings">The settings document for this model.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when name is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the GraphQL document is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only, metadata is not set, or model already exists.</exception>
    public async Task AddOpenApiModelAsync(
        string name,
        ReadOnlyMemory<byte> document,
        JsonDocument settings,
        CancellationToken cancellationToken = default)
    {
        if (!NameValidator.IsValidName(name))
        {
            throw new ArgumentException($"The model name '{name}' is invalid.", nameof(name));
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(document.Length, 0);
        ArgumentNullException.ThrowIfNull(settings);
        ObjectDisposedException.ThrowIf(_disposed, this);

        EnsureMutable();

        var metadata = await GetArchiveMetadataAsync(cancellationToken);

        if (metadata is null)
        {
            throw new InvalidOperationException(
                "You need to first define the archive metadata.");
        }

        if (metadata.Models.Contains(name))
        {
            throw new InvalidOperationException(
                $"A model with the name '{name}' already exists in the archive.");
        }

        await using (var stream = _session.OpenWrite(FileNames.GetModelFragmentPath(name)))
        {
            await stream.WriteAsync(document, cancellationToken);
        }

        await using (var stream = _session.OpenWrite(FileNames.GetModelSettingsPath(name)))
        {
            await using var jsonWriter = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            settings.WriteTo(jsonWriter);
            await jsonWriter.FlushAsync(cancellationToken);
        }

        _metadata = metadata with { Models = metadata.Models.Add(name) };
        await SetArchiveMetadataAsync(_metadata, cancellationToken);
    }

    /// <summary>
    /// Tries to get an OpenAPI model by name.
    /// </summary>
    /// <param name="name">The name of the model to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The OpenAPI model if found, or null if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when name is invalid.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    public async Task<OpenApiModel?> TryGetOpenApiModelAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        if (!NameValidator.IsValidName(name))
        {
            throw new ArgumentException($"The model name '{name}' is invalid.", nameof(name));
        }

        ObjectDisposedException.ThrowIf(_disposed, this);

        var metadata = await GetArchiveMetadataAsync(cancellationToken);

        if (metadata?.Models.Contains(name) != true)
        {
            return null;
        }

        var fragmentPath = FileNames.GetModelFragmentPath(name);
        var settingsPath = FileNames.GetModelSettingsPath(name);

        if (!_session.Exists(fragmentPath) || !_session.Exists(settingsPath))
        {
            return null;
        }

        var buffer = TryRentBuffer();

        try
        {
            await using var fragmentStream = await _session.OpenReadAsync(
                fragmentPath,
                FileKind.Fragment,
                cancellationToken);
            await fragmentStream.CopyToAsync(buffer, cancellationToken);
            var fragment = buffer.WrittenMemory.ToArray();
            buffer.Clear();

            await using var settingsStream = await _session.OpenReadAsync(
                settingsPath,
                FileKind.Settings,
                cancellationToken);
            var settings = await JsonDocument.ParseAsync(settingsStream, cancellationToken: cancellationToken);

            return new OpenApiModel(fragment, settings);
        }
        finally
        {
            TryReturnBuffer(buffer);
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
        if (_mode is OpenApiCollectionArchiveMode.Read)
        {
            throw new InvalidOperationException("Cannot modify a read-only archive.");
        }
    }

    /// <summary>
    /// Commits any pending changes to the archive and flushes them to the underlying stream.
    /// After committing, the archive may transition to Update mode if the stream supports it.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only.</exception>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_mode is OpenApiCollectionArchiveMode.Read)
        {
            throw new InvalidOperationException("Cannot commit changes to a read-only archive.");
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
                _mode = OpenApiCollectionArchiveMode.Update;
                _session.SetMode(_mode);
            }
            else
            {
                _mode = OpenApiCollectionArchiveMode.Read;
            }
        }
    }

    /// <summary>
    /// Releases all resources used by the OpenApiCollectionArchive.
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
