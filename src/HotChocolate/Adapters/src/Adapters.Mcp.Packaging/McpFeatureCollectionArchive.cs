using System.Buffers;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Text.Json;
using HotChocolate.Adapters.Mcp.Packaging.Serializers;

namespace HotChocolate.Adapters.Mcp.Packaging;

/// <summary>
/// Provides functionality for creating, reading, and modifying MCP Feature Collection files.
/// An MCP Feature Collection is a ZIP-based container format that packages MCP tool and prompt definitions.
/// </summary>
public sealed class McpFeatureCollectionArchive : IDisposable
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private readonly McpFeatureCollectionArchiveSession _session;
    private ZipArchive _archive;
    private McpFeatureCollectionArchiveMode _mode;
    private ArrayBufferWriter<byte>? _buffer;
    private ArchiveMetadata? _metadata;
    private bool _disposed;

    private McpFeatureCollectionArchive(
        Stream stream,
        McpFeatureCollectionArchiveMode mode,
        bool leaveOpen,
        McpFeatureCollectionArchiveReadOptions options)
    {
        _stream = stream;
        _mode = mode;
        _leaveOpen = leaveOpen;
        _archive = new ZipArchive(stream, (ZipArchiveMode)mode, leaveOpen);
        _session = new McpFeatureCollectionArchiveSession(_archive, mode, options);
    }

    /// <summary>
    /// Creates a new MCP Feature Collection archive with the specified filename.
    /// </summary>
    /// <param name="filename">The path to the archive file to create.</param>
    /// <returns>A new McpFeatureCollectionArchive instance in Create mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filename is null.</exception>
    public static McpFeatureCollectionArchive Create(string filename)
    {
        ArgumentNullException.ThrowIfNull(filename);
        return Create(File.Create(filename));
    }

    /// <summary>
    /// Creates a new MCP Feature Collection archive using the provided stream.
    /// </summary>
    /// <param name="stream">The stream to write the archive to.</param>
    /// <param name="leaveOpen">True to leave the stream open after disposal; otherwise, false.</param>
    /// <returns>A new McpFeatureCollectionArchive instance in Create mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public static McpFeatureCollectionArchive Create(Stream stream, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return new McpFeatureCollectionArchive(stream, McpFeatureCollectionArchiveMode.Create, leaveOpen, McpFeatureCollectionArchiveReadOptions.Default);
    }

    /// <summary>
    /// Opens an existing MCP Feature Collection archive from a file.
    /// </summary>
    /// <param name="filename">The path to the archive file to open.</param>
    /// <param name="mode">The mode to open the archive in.</param>
    /// <returns>A McpFeatureCollectionArchive instance opened in the specified mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filename is null.</exception>
    /// <exception cref="ArgumentException">Thrown when mode is invalid.</exception>
    public static McpFeatureCollectionArchive Open(
        string filename,
        McpFeatureCollectionArchiveMode mode = McpFeatureCollectionArchiveMode.Read)
    {
        ArgumentNullException.ThrowIfNull(filename);

        return mode switch
        {
            McpFeatureCollectionArchiveMode.Read => Open(File.OpenRead(filename), mode),
            McpFeatureCollectionArchiveMode.Create => Create(File.Create(filename)),
            McpFeatureCollectionArchiveMode.Update => Open(File.Open(filename, FileMode.Open, FileAccess.ReadWrite), mode),
            _ => throw new ArgumentException("Invalid mode.", nameof(mode))
        };
    }

    /// <summary>
    /// Opens an MCP Feature Collection archive from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the archive data.</param>
    /// <param name="mode">The mode to open the archive in.</param>
    /// <param name="leaveOpen">True to leave the stream open after disposal; otherwise, false.</param>
    /// <param name="options">The options to use when reading from the archive.</param>
    /// <returns>An MCP Feature Collection archive instance opened in the specified mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public static McpFeatureCollectionArchive Open(
        Stream stream,
        McpFeatureCollectionArchiveMode mode = McpFeatureCollectionArchiveMode.Read,
        bool leaveOpen = false,
        McpFeatureCollectionArchiveOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var readOptions = new McpFeatureCollectionArchiveReadOptions(
            options.MaxAllowedDocumentSize
                ?? McpFeatureCollectionArchiveReadOptions.Default.MaxAllowedDocumentSize,
            options.MaxAllowedSettingsSize
                ?? McpFeatureCollectionArchiveReadOptions.Default.MaxAllowedSettingsSize,
            options.MaxAllowedOpenAiComponentSize
                ?? McpFeatureCollectionArchiveReadOptions.Default.MaxAllowedOpenAiComponentSize);
        return new McpFeatureCollectionArchive(stream, mode, leaveOpen, readOptions);
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
    /// Adds an MCP prompt to the archive.
    /// </summary>
    /// <param name="name">The unique name of this prompt.</param>
    /// <param name="settings">The settings document for this prompt.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only, metadata is not set, or prompt already exists.</exception>
    public async Task AddPromptAsync(
        string name,
        JsonDocument settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ObjectDisposedException.ThrowIf(_disposed, this);

        EnsureMutable();

        var metadata = await GetArchiveMetadataAsync(cancellationToken);

        if (metadata is null)
        {
            throw new InvalidOperationException(
                "You need to first define the archive metadata.");
        }

        if (metadata.Prompts.Contains(name))
        {
            throw new InvalidOperationException(
                $"A prompt with the name '{name}' already exists in the archive.");
        }

        await using (var stream = _session.OpenWrite(FileNames.GetPromptSettingsPath(name)))
        {
            await using var jsonWriter = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            settings.WriteTo(jsonWriter);
            await jsonWriter.FlushAsync(cancellationToken);
        }

        _metadata = metadata with { Prompts = metadata.Prompts.Add(name) };
        await SetArchiveMetadataAsync(_metadata, cancellationToken);
    }

    /// <summary>
    /// Tries to get an MCP prompt by name.
    /// </summary>
    /// <param name="name">The name of the prompt to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The MCP prompt if found, or null if not found.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    public async Task<McpPrompt?> TryGetPromptAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var metadata = await GetArchiveMetadataAsync(cancellationToken);

        if (metadata?.Prompts.Contains(name) != true)
        {
            return null;
        }

        var settingsPath = FileNames.GetPromptSettingsPath(name);

        if (!_session.Exists(settingsPath))
        {
            return null;
        }

        await using var settingsStream = await _session.OpenReadAsync(
            settingsPath,
            FileKind.Settings,
            cancellationToken);
        var settings = await JsonDocument.ParseAsync(settingsStream, cancellationToken: cancellationToken);

        return new McpPrompt(settings);
    }

    /// <summary>
    /// Adds an MCP tool to the archive.
    /// </summary>
    /// <param name="name">The unique name for this tool.</param>
    /// <param name="document">The GraphQL document to store.</param>
    /// <param name="settings">The optional settings document for this tool.</param>
    /// <param name="openAiComponent">The optional OpenAI component for this tool.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when name is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the GraphQL document is empty.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only, metadata is not set, or tool already exists.</exception>
    public async Task AddToolAsync(
        string name,
        ReadOnlyMemory<byte> document,
        JsonDocument? settings,
        ReadOnlyMemory<byte>? openAiComponent,
        CancellationToken cancellationToken = default)
    {
        if (!NameValidator.IsValidName(name))
        {
            throw new ArgumentException($"The tool name '{name}' is invalid.", nameof(name));
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(document.Length, 0);
        ObjectDisposedException.ThrowIf(_disposed, this);

        EnsureMutable();

        var metadata = await GetArchiveMetadataAsync(cancellationToken);

        if (metadata is null)
        {
            throw new InvalidOperationException(
                "You need to first define the archive metadata.");
        }

        if (metadata.Tools.Contains(name))
        {
            throw new InvalidOperationException(
                $"A tool with the name '{name}' already exists in the archive.");
        }

        await using (var stream = _session.OpenWrite(FileNames.GetToolDocumentPath(name)))
        {
            await stream.WriteAsync(document, cancellationToken);
        }

        if (settings is not null)
        {
            await using var stream = _session.OpenWrite(FileNames.GetToolSettingsPath(name));
            await using var jsonWriter = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            settings.WriteTo(jsonWriter);
            await jsonWriter.FlushAsync(cancellationToken);
        }

        if (openAiComponent is not null)
        {
            await using var stream = _session.OpenWrite(FileNames.GetToolOpenAiComponentPath(name));
            await stream.WriteAsync(openAiComponent.Value, cancellationToken);
        }

        _metadata = metadata with { Tools = metadata.Tools.Add(name) };
        await SetArchiveMetadataAsync(_metadata, cancellationToken);
    }

    /// <summary>
    /// Tries to get an MCP tool by name.
    /// </summary>
    /// <param name="name">The name of the tool to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The MCP tool if found, or null if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when name is invalid.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    public async Task<McpTool?> TryGetToolAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        if (!NameValidator.IsValidName(name))
        {
            throw new ArgumentException($"The tool name '{name}' is invalid.", nameof(name));
        }

        ObjectDisposedException.ThrowIf(_disposed, this);

        var metadata = await GetArchiveMetadataAsync(cancellationToken);

        if (metadata?.Tools.Contains(name) != true)
        {
            return null;
        }

        var documentPath = FileNames.GetToolDocumentPath(name);

        if (!_session.Exists(documentPath))
        {
            return null;
        }

        var buffer = TryRentBuffer();

        try
        {
            // Document.
            await using var documentStream = await _session.OpenReadAsync(
                documentPath,
                FileKind.Document,
                cancellationToken);
            await documentStream.CopyToAsync(buffer, cancellationToken);
            var document = buffer.WrittenMemory.ToArray();
            buffer.Clear();

            // Settings.
            var settingsPath = FileNames.GetToolSettingsPath(name);
            JsonDocument? settings = null;

            if (_session.Exists(settingsPath))
            {
                await using var settingsStream = await _session.OpenReadAsync(
                    settingsPath,
                    FileKind.Settings,
                    cancellationToken);
                settings = await JsonDocument.ParseAsync(settingsStream, cancellationToken: cancellationToken);
            }

            // OpenAI component.
            var openAiComponentPath = FileNames.GetToolOpenAiComponentPath(name);
            ReadOnlyMemory<byte>? openAiComponent = null;

            if (_session.Exists(openAiComponentPath))
            {
                await using var componentStream = await _session.OpenReadAsync(
                    openAiComponentPath,
                    FileKind.OpenAiComponent,
                    cancellationToken);
                await componentStream.CopyToAsync(buffer, cancellationToken);
                openAiComponent = buffer.WrittenMemory.ToArray();
                buffer.Clear();
            }

            return new McpTool(document, settings, openAiComponent);
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
        if (_mode is McpFeatureCollectionArchiveMode.Read)
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

        if (_mode is McpFeatureCollectionArchiveMode.Read)
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
                _mode = McpFeatureCollectionArchiveMode.Update;
                _session.SetMode(_mode);
            }
            else
            {
                _mode = McpFeatureCollectionArchiveMode.Read;
            }
        }
    }

    /// <summary>
    /// Releases all resources used by the McpFeatureCollectionArchive.
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
