using System.Buffers;
using System.Collections.Immutable;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Packaging.Serializers;

namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Provides functionality for creating, reading, and modifying Fusion Archive (.far) files.
/// A Fusion Archive is a ZIP-based container format that packages GraphQL Fusion gateway configurations.
/// </summary>
public sealed class FusionArchive : IDisposable
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private readonly ArchiveSession _session;
    private readonly FusionArchiveReadOptions _readOptions;
    private ZipArchive _archive;
    private FusionArchiveMode _mode;
    private ArrayBufferWriter<byte>? _buffer;
    private ArchiveMetadata? _metadata;
    private bool _disposed;

    private FusionArchive(
        Stream stream,
        FusionArchiveMode mode,
        bool leaveOpen,
        FusionArchiveReadOptions options)
    {
        _stream = stream;
        _mode = mode;
        _leaveOpen = leaveOpen;
        _archive = new ZipArchive(stream, (ZipArchiveMode)mode, leaveOpen);
        _session = new ArchiveSession(_archive, mode, options);
        _readOptions = options;
    }

    /// <summary>
    /// Creates a new Fusion Archive with the specified filename.
    /// </summary>
    /// <param name="filename">The path to the archive file to create.</param>
    /// <returns>A new FusionArchive instance in Create mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filename is null.</exception>
    public static FusionArchive Create(string filename)
    {
        ArgumentNullException.ThrowIfNull(filename);
        return Create(File.Create(filename));
    }

    /// <summary>
    /// Creates a new Fusion Archive using the provided stream.
    /// </summary>
    /// <param name="stream">The stream to write the archive to.</param>
    /// <param name="leaveOpen">True to leave the stream open after disposal; otherwise, false.</param>
    /// <returns>A new FusionArchive instance in Create mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public static FusionArchive Create(Stream stream, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return new FusionArchive(stream, FusionArchiveMode.Create, leaveOpen, FusionArchiveReadOptions.Default);
    }

    /// <summary>
    /// Opens an existing Fusion Archive from a file.
    /// </summary>
    /// <param name="filename">The path to the archive file to open.</param>
    /// <param name="mode">The mode to open the archive in.</param>
    /// <returns>A FusionArchive instance opened in the specified mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filename is null.</exception>
    /// <exception cref="ArgumentException">Thrown when mode is invalid.</exception>
    public static FusionArchive Open(
        string filename,
        FusionArchiveMode mode = FusionArchiveMode.Read)
    {
        ArgumentNullException.ThrowIfNull(filename);

        return mode switch
        {
            FusionArchiveMode.Read => Open(File.OpenRead(filename), mode),
            FusionArchiveMode.Create => Create(File.Create(filename)),
            FusionArchiveMode.Update => Open(File.Open(filename, FileMode.Open, FileAccess.ReadWrite), mode),
            _ => throw new ArgumentException("Invalid mode.", nameof(mode))
        };
    }

    /// <summary>
    /// Opens a Fusion Archive from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the archive data.</param>
    /// <param name="mode">The mode to open the archive in.</param>
    /// <param name="leaveOpen">True to leave the stream open after disposal; otherwise, false.</param>
    /// <param name="options">The options to use when reading from the archive.</param>
    /// <returns>A FusionArchive instance opened in the specified mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public static FusionArchive Open(
        Stream stream,
        FusionArchiveMode mode = FusionArchiveMode.Read,
        bool leaveOpen = false,
        FusionArchiveOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var readOptions = new FusionArchiveReadOptions(
            options.MaxAllowedSchemaSize ?? FusionArchiveReadOptions.Default.MaxAllowedSchemaSize,
            options.MaxAllowedSettingsSize ?? FusionArchiveReadOptions.Default.MaxAllowedSettingsSize,
            options.MaxAllowedLegacyArchiveSize ?? FusionArchiveReadOptions.Default.MaxAllowedLegacyArchiveSize,
            options.MaxAllowedPolicySize ?? FusionArchiveReadOptions.Default.MaxAllowedPolicySize,
            options.MaxAllowedPolicyDataSize ?? FusionArchiveReadOptions.Default.MaxAllowedPolicyDataSize);
        return new FusionArchive(stream, mode, leaveOpen, readOptions);
    }

    /// <summary>
    /// Sets the archive metadata containing format version and schema information.
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
    /// Gets the latest (highest version) supported gateway format from the archive metadata.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The latest supported gateway format version.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no supported gateway formats are found.</exception>
    public async Task<Version> GetLatestSupportedGatewayFormatAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var metadata = _metadata ?? await GetArchiveMetadataAsync(cancellationToken);

        if (metadata?.SupportedGatewayFormats == null || !metadata.SupportedGatewayFormats.Any())
        {
            throw new InvalidOperationException("No supported gateway formats found in archive metadata.");
        }

        return metadata.SupportedGatewayFormats.Max() ??
            throw new InvalidOperationException("Invalid metadata format.");
    }

    /// <summary>
    /// Gets all supported gateway format versions from the archive metadata, ordered by version descending.
    /// Returns an empty collection if no formats are supported.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of supported gateway format versions.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    public async Task<IEnumerable<Version>> GetSupportedGatewayFormatsAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var metadata = _metadata ?? await GetArchiveMetadataAsync(cancellationToken);

        if (metadata?.SupportedGatewayFormats == null || !metadata.SupportedGatewayFormats.Any())
        {
            return [];
        }

        return metadata.SupportedGatewayFormats.OrderByDescending(v => v);
    }

    /// <summary>
    /// Gets the names of all source schemas included in the archive metadata, ordered alphabetically.
    /// Returns an empty collection if no source schemas are present.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of source schema names.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    public async Task<IEnumerable<string>> GetSourceSchemaNamesAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var metadata = _metadata ?? await GetArchiveMetadataAsync(cancellationToken);

        if (metadata?.SourceSchemas == null || !metadata.SourceSchemas.Any())
        {
            return [];
        }

        return metadata.SourceSchemas.Order();
    }

    /// <summary>
    /// Sets the composition settings that control how source schemas are composed into the execution schema.
    /// </summary>
    /// <param name="settings">The composition settings as a JSON document.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only.</exception>
    public async Task SetCompositionSettingsAsync(
        JsonDocument settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureMutable();

        Exception? exception = null;
        await using var stream = _session.OpenWrite(FileNames.CompositionSettings);
        var writer = PipeWriter.Create(stream);

        try
        {
            await using var jsonWriter = new Utf8JsonWriter(writer, new JsonWriterOptions { Indented = true });
            settings.WriteTo(jsonWriter);
            await jsonWriter.FlushAsync(cancellationToken);
            await writer.FlushAsync(cancellationToken);
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
    /// Gets the composition settings from the archive.
    /// Returns null if no composition settings are present.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The composition settings as a JSON document or null if not present.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    public async Task<JsonDocument?> GetCompositionSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!await _session.ExistsAsync(FileNames.CompositionSettings, FileKind.Settings, cancellationToken))
        {
            return null;
        }

        await using var stream = await _session.OpenReadAsync(
            FileNames.CompositionSettings,
            FileKind.Settings,
            cancellationToken);
        return await JsonDocument.ParseAsync(stream, default, cancellationToken);
    }

    /// <summary>
    /// Sets the gateway configuration for a specific format version using raw bytes.
    /// The version must be declared in the archive metadata before calling this method.
    /// </summary>
    /// <param name="schema">The gateway schema as a GraphQL schema string.</param>
    /// <param name="settings">The gateway settings as a JSON document.</param>
    /// <param name="version">The gateway format version.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when schema is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when version is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only, metadata is missing, or version is not declared.</exception>
    public async Task SetGatewayConfigurationAsync(
        string schema,
        JsonDocument settings,
        Version version,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(schema);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(version);
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureMutable();

        await SetGatewayConfigurationAsync(Encoding.UTF8.GetBytes(schema), settings, version, cancellationToken);
    }

    /// <summary>
    /// Sets the gateway configuration for a specific format version using raw bytes.
    /// The version must be declared in the archive metadata before calling this method.
    /// </summary>
    /// <param name="schema">The gateway schema as UTF-8 encoded bytes.</param>
    /// <param name="settings">The gateway settings as a JSON document.</param>
    /// <param name="version">The gateway format version.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when version is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the archive is read-only, metadata is missing, or version is not declared.
    /// </exception>
    public async Task SetGatewayConfigurationAsync(
        ReadOnlyMemory<byte> schema,
        JsonDocument settings,
        Version version,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureMutable();

        var metadata = await GetArchiveMetadataAsync(cancellationToken);

        if (metadata is null)
        {
            throw new InvalidOperationException(
                "You need to first define the archive metadata.");
        }

        if (!metadata.SupportedGatewayFormats.Contains(version))
        {
            throw new InvalidOperationException(
                "You need to first declare the gateway schema version in the archive metadata.");
        }

        await using (var stream = _session.OpenWrite(FileNames.GetGatewaySchemaPath(version)))
        {
            await stream.WriteAsync(schema, cancellationToken);
        }

        await using (var stream = _session.OpenWrite(FileNames.GetGatewaySettingsPath(version)))
        {
            await using var jsonWriter = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            settings.WriteTo(jsonWriter);
            await jsonWriter.FlushAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Attempts to get a gateway schema with the highest version that is less
    /// than or equal to the specified maximum version.
    /// </summary>
    /// <param name="maxVersion">The maximum version to consider.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A gateway configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when maxVersion or buffer is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no supported gateway formats are found.</exception>
    public async Task<GatewayConfiguration?> TryGetGatewayConfigurationAsync(
        Version maxVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(maxVersion);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var metadata = await GetArchiveMetadataAsync(cancellationToken);
        if (metadata?.SupportedGatewayFormats == null || !metadata.SupportedGatewayFormats.Any())
        {
            throw new InvalidOperationException("No supported gateway formats found in archive metadata.");
        }

        // we need to find the version that is less than or equal to the maxVersion
        var version = metadata.SupportedGatewayFormats.OrderByDescending(v => v).FirstOrDefault(v => v <= maxVersion);
        if (version == null)
        {
            return null;
        }

        JsonDocument settings;
        await using (var stream = await _session.OpenReadAsync(
            FileNames.GetGatewaySettingsPath(version),
            FileKind.Settings,
            cancellationToken))
        {
            settings = await JsonDocument.ParseAsync(stream, default, cancellationToken);
        }

        return new GatewayConfiguration(OpenReadSchemaAsync, settings, version);

        Task<Stream> OpenReadSchemaAsync(CancellationToken ct)
            => _session.OpenReadAsync(FileNames.GetGatewaySchemaPath(version), FileKind.Schema, ct);
    }

    /// <summary>
    /// Sets a Rego policy and its GraphQL data requirements for a specific policy format version.
    /// </summary>
    /// <param name="policyName">The name of the policy.</param>
    /// <param name="policy">The Rego policy implementation as UTF-8 encoded bytes.</param>
    /// <param name="requirements">The GraphQL data requirements as UTF-8 encoded bytes.</param>
    /// <param name="version">The Rego policy format version.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when the policy name or version is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the policy or requirements are empty.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only.</exception>
    public async Task SetRegoPolicyAsync(
        string policyName,
        ReadOnlyMemory<byte> policy,
        ReadOnlyMemory<byte> requirements,
        Version version,
        CancellationToken cancellationToken = default)
    {
        ValidateRegoPolicyName(policyName);
        ValidateRegoPolicyVersion(version);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(policy.Length, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(requirements.Length, 0);
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureMutable();

        var conflictingPolicy = ReadRegoPolicyFileSets().FirstOrDefault(
            t => t.Version == version
                && t.Name != policyName
                && t.Name.Equals(policyName, StringComparison.OrdinalIgnoreCase));
        if (conflictingPolicy is not null)
        {
            throw new InvalidOperationException(
                $"The Rego policy '{policyName}' conflicts with the existing policy "
                + $"'{conflictingPolicy.Name}' because policy names must be unique ignoring case.");
        }

        // A policy declares a package whose rules form a virtual document rooted at that package path.
        // That virtual document must not overlap a data mount, which is a base document at the same
        // path. Reject the policy when its package collides with an existing data mount.
        if (TryScanRegoPackageSegments(policy.Span, out var packageSegments))
        {
            await EnsureNoRegoBaseVirtualConflictForPolicyAsync(version, packageSegments, cancellationToken);
        }

        await using (var stream = _session.OpenWrite(FileNames.GetRegoPolicyPath(version, policyName)))
        {
            await stream.WriteAsync(policy, cancellationToken);
        }

        await using (var stream = _session.OpenWrite(FileNames.GetRegoPolicyRequirementsPath(version, policyName)))
        {
            await stream.WriteAsync(requirements, cancellationToken);
        }
    }

    /// <summary>
    /// Gets all Rego policy format versions in the archive, ordered by version descending.
    /// </summary>
    /// <returns>The Rego policy format versions.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidDataException">Thrown when the Rego policy layout is invalid.</exception>
    public IEnumerable<Version> GetSupportedRegoPolicyFormats()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return ReadRegoPolicyFileSets()
            .Select(t => t.Version)
            .Distinct()
            .OrderDescending()
            .ToArray();
    }

    /// <summary>
    /// Gets all Rego policy names for a policy format version, ordered alphabetically.
    /// </summary>
    /// <param name="version">The Rego policy format version.</param>
    /// <returns>The Rego policy names.</returns>
    /// <exception cref="ArgumentException">Thrown when the version is invalid.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidDataException">Thrown when the Rego policy layout is invalid.</exception>
    public IEnumerable<string> GetRegoPolicyNames(Version version)
    {
        ValidateRegoPolicyVersion(version);
        ObjectDisposedException.ThrowIf(_disposed, this);

        return ReadRegoPolicyFileSets()
            .Where(t => t.Version == version)
            .Select(t => t.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Gets the complete Rego policy catalog for one exact policy format version.
    /// </summary>
    /// <param name="version">The Rego policy format version.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The Rego policies ordered by name.</returns>
    /// <exception cref="ArgumentException">Thrown when the version is invalid.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidDataException">Thrown when the Rego policy layout is invalid.</exception>
    public async Task<IReadOnlyList<RegoPolicyConfiguration>> GetRegoPoliciesAsync(
        Version version,
        CancellationToken cancellationToken = default)
    {
        ValidateRegoPolicyVersion(version);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var policies = ReadRegoPolicyFileSets()
            .Where(t => t.Version == version)
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToArray();
        var configurations = new RegoPolicyConfiguration[policies.Length];

        for (var i = 0; i < policies.Length; i++)
        {
            configurations[i] = await CreateRegoPolicyConfigurationAsync(
                policies[i],
                cancellationToken);
        }

        return configurations;
    }

    /// <summary>
    /// Attempts to get a Rego policy and its GraphQL data requirements from the archive.
    /// </summary>
    /// <param name="policyName">The name of the policy.</param>
    /// <param name="version">The Rego policy format version.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The Rego policy configuration, or null when the policy is not present.</returns>
    /// <exception cref="ArgumentException">Thrown when the policy name or version is invalid.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidDataException">Thrown when the Rego policy layout is invalid.</exception>
    public async Task<RegoPolicyConfiguration?> TryGetRegoPolicyAsync(
        string policyName,
        Version version,
        CancellationToken cancellationToken = default)
    {
        ValidateRegoPolicyName(policyName);
        ValidateRegoPolicyVersion(version);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var fileSet = ReadRegoPolicyFileSets().FirstOrDefault(
            t => t.Version == version && t.Name == policyName);
        if (fileSet is null)
        {
            return null;
        }

        return await CreateRegoPolicyConfigurationAsync(fileSet, cancellationToken);
    }

    /// <summary>
    /// Sets a Rego data document mounted at the specified path within the data tree of a policy format version.
    /// </summary>
    /// <param name="mountPath">
    /// The slash-separated directory path relative to the data root at which the document is mounted.
    /// An empty string mounts the document at the data root.
    /// </param>
    /// <param name="data">
    /// The data document as UTF-8 encoded JSON bytes. The root of the document must be a JSON object.
    /// </param>
    /// <param name="formatVersion">The Rego policy format version.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when the mount path is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the mount path, version, or data is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the data is empty or exceeds the maximum allowed size for a Rego data mount.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the archive is read-only or the mount conflicts with an existing mount.
    /// </exception>
    public async Task SetRegoDataAsync(
        string mountPath,
        ReadOnlyMemory<byte> data,
        Version formatVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mountPath);
        ValidateRegoPolicyVersion(formatVersion);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(data.Length, 0);

        if (data.Length > _readOptions.MaxAllowedPolicyDataSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(data),
                data.Length,
                "The Rego data document exceeds the maximum allowed size of "
                + $"{_readOptions.MaxAllowedPolicyDataSize} bytes for a data mount.");
        }

        ObjectDisposedException.ThrowIf(_disposed, this);

        var segments = ValidateRegoDataMountPath(mountPath);
        EnsureMutable();

        using var document = ParseRegoDataObject(data);
        await EnsureNoRegoDataConflictAsync(
            formatVersion,
            mountPath,
            segments,
            document.RootElement,
            cancellationToken);

        // A data mount is a base document; it must not overlap the virtual document rooted at a policy
        // package path. Reject the mount when it collides with an existing policy's package.
        await EnsureNoRegoBaseVirtualConflictForDataAsync(
            formatVersion,
            mountPath,
            segments,
            document.RootElement,
            cancellationToken);

        await using var stream = _session.OpenWrite(FileNames.GetRegoDataPath(formatVersion, mountPath));
        await stream.WriteAsync(data, cancellationToken);
    }

    /// <summary>
    /// Attempts to get the Rego data document mounted at the specified path for a policy format version.
    /// </summary>
    /// <param name="mountPath">
    /// The slash-separated directory path relative to the data root. An empty string is the data root.
    /// </param>
    /// <param name="formatVersion">The Rego policy format version.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The data document as UTF-8 encoded JSON bytes, or <c>null</c> when it is not present.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the mount path is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the mount path or version is invalid.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    public async Task<ReadOnlyMemory<byte>?> TryGetRegoDataAsync(
        string mountPath,
        Version formatVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mountPath);
        ValidateRegoPolicyVersion(formatVersion);
        ValidateRegoDataMountPath(mountPath);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var path = FileNames.GetRegoDataPath(formatVersion, mountPath);

        if (!await _session.ExistsAsync(path, FileKind.PolicyData, cancellationToken))
        {
            return null;
        }

        await using var stream = await _session.OpenReadAsync(path, FileKind.PolicyData, cancellationToken);
        await using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        return memory.ToArray();
    }

    /// <summary>
    /// Gets the mount paths of all Rego data documents for a policy format version, ordered by path.
    /// The data root is represented by an empty string.
    /// </summary>
    /// <param name="formatVersion">The Rego policy format version.</param>
    /// <returns>The Rego data mount paths.</returns>
    /// <exception cref="ArgumentException">Thrown when the version is invalid.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidDataException">Thrown when the Rego data layout is invalid.</exception>
    public IEnumerable<string> GetRegoDataMountPaths(Version formatVersion)
    {
        ValidateRegoPolicyVersion(formatVersion);
        ObjectDisposedException.ThrowIf(_disposed, this);

        return ReadRegoDataMounts(formatVersion)
            .Select(m => m.MountPath)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Removes the Rego data document mounted at the specified path for a policy format version.
    /// </summary>
    /// <param name="mountPath">
    /// The slash-separated directory path relative to the data root. An empty string is the data root.
    /// </param>
    /// <param name="formatVersion">The Rego policy format version.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if the data document was present and removed; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the mount path is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the mount path or version is invalid.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only.</exception>
    public async Task<bool> RemoveRegoDataAsync(
        string mountPath,
        Version formatVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mountPath);
        ValidateRegoPolicyVersion(formatVersion);
        ValidateRegoDataMountPath(mountPath);
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureMutable();

        var path = FileNames.GetRegoDataPath(formatVersion, mountPath);

        if (!await _session.ExistsAsync(path, FileKind.PolicyData, cancellationToken))
        {
            return false;
        }

        _session.Delete(path);
        return true;
    }

    /// <summary>
    /// Attempts to assemble the merged Rego data document for a policy format version by combining every
    /// mounted data document into a single hierarchical JSON document. Returns <c>null</c> when the version
    /// has no data subtree. The merge produces the tree by mount path; the order of keys in the result is
    /// not specified.
    /// </summary>
    /// <param name="formatVersion">The Rego policy format version.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// An owner of the merged data document that the caller disposes, or <c>null</c> when no data subtree exists.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when the version is invalid.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidDataException">Thrown when the Rego data layout defines conflicting mounts.</exception>
    public async Task<JsonDocumentOwner?> TryGetRegoDataDocumentAsync(
        Version formatVersion,
        CancellationToken cancellationToken = default)
    {
        ValidateRegoPolicyVersion(formatVersion);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var mounts = ReadRegoDataMounts(formatVersion);

        if (mounts.Count == 0)
        {
            return null;
        }

        // Parse every mount document up front, ordered so that ancestor mounts precede their descendants.
        var ordered = mounts.OrderBy(m => m.MountPath, StringComparer.Ordinal).ToArray();
        var documents = new (string MountPath, string[] Segments, JsonObject Document)[ordered.Length];

        for (var i = 0; i < ordered.Length; i++)
        {
            JsonNode? node;
            await using (var stream = await _session.OpenReadAsync(
                ordered[i].Path,
                FileKind.PolicyData,
                cancellationToken))
            {
                node = JsonNode.Parse(stream);
            }

            if (node is not JsonObject document)
            {
                throw new InvalidDataException(
                    $"The Rego data document '{ordered[i].Path}' must be a JSON object.");
            }

            var segments = ordered[i].MountPath.Length == 0 ? [] : ordered[i].MountPath.Split('/');
            documents[i] = (ordered[i].MountPath, segments, document);
        }

        // Reject conflicting mounts on read using the same rule the write path enforces, so an archive
        // produced elsewhere is not silently deep-merged where the write API would reject it.
        for (var i = 0; i < documents.Length; i++)
        {
            for (var j = 0; j < documents.Length; j++)
            {
                if (i == j || !IsRegoDataMountPrefix(documents[i].Segments, documents[j].Segments))
                {
                    continue;
                }

                if (RegoDataDocumentDefinesMountPath(
                    documents[i].Document,
                    documents[j].Segments.AsSpan(documents[i].Segments.Length)))
                {
                    throw CreateRegoDataConflictDataException(documents[i].MountPath, documents[j].MountPath);
                }
            }
        }

        // Reject data mounts that overlap a policy package's virtual document, the base versus virtual
        // document conflict, using the same rule the write paths enforce so an archive produced
        // elsewhere is not silently merged where the write API would reject it.
        var virtualRoots = await ReadRegoPolicyPackageRootsAsync(formatVersion, cancellationToken);

        foreach (var (mountPath, segments, document) in documents)
        {
            foreach (var policySegments in virtualRoots)
            {
                if (RegoSegmentsEqual(segments, policySegments)
                    || IsRegoDataMountPrefix(policySegments, segments))
                {
                    throw CreateRegoBaseVirtualConflictDataException(mountPath, policySegments);
                }

                if (IsRegoDataMountPrefix(segments, policySegments)
                    && RegoDataDocumentDefinesMountPath(document, policySegments.AsSpan(segments.Length)))
                {
                    throw CreateRegoBaseVirtualConflictDataException(mountPath, policySegments);
                }
            }
        }

        var root = new JsonObject();

        foreach (var (mountPath, segments, document) in documents)
        {
            var target = segments.Length == 0
                ? root
                : NavigateOrCreateRegoDataObject(root, segments, mountPath);
            MergeRegoDataObject(target, document, mountPath);
        }

        var buffer = new PooledArrayWriter();

        try
        {
            await using (var writer = new Utf8JsonWriter(buffer))
            {
                root.WriteTo(writer);
            }

            return new JsonDocumentOwner(JsonDocument.Parse(buffer.WrittenMemory), buffer);
        }
        catch
        {
            buffer.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Sets a source schema in the archive.
    /// The schema name must be declared in the archive metadata before calling this method.
    /// </summary>
    /// <param name="schemaName">The name of the source schema.</param>
    /// <param name="schema">The source schema as UTF-8 encoded bytes.</param>
    /// <param name="settings">The source schema configuration.</param>
    /// <param name="schemaExtensions">
    /// The source schema extensions as UTF-8 encoded bytes. If empty, no extensions file is written.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when schemaName is null, empty, or invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when schema is empty.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only, metadata is missing, or schema name is not declared.</exception>
    public async Task SetSourceSchemaConfigurationAsync(
        string schemaName,
        ReadOnlyMemory<byte> schema,
        JsonDocument settings,
        ReadOnlyMemory<byte> schemaExtensions = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(schemaName);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(schema.Length, 0);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!SchemaNameValidator.IsValidSchemaName(schemaName))
        {
            throw new ArgumentException("Invalid schema name.", nameof(schemaName));
        }

        EnsureMutable();

        var metadata = await GetArchiveMetadataAsync(cancellationToken);

        if (metadata is null)
        {
            throw new InvalidOperationException(
                "You need to first define the archive metadata.");
        }

        if (!metadata.SourceSchemas.Contains(schemaName))
        {
            throw new InvalidOperationException(
                "You need to first declare the source schema in the archive metadata.");
        }

        await using (var stream = _session.OpenWrite(FileNames.GetSourceSchemaPath(schemaName)))
        {
            await stream.WriteAsync(schema, cancellationToken);
        }

        if (schemaExtensions.Length > 0)
        {
            await using var stream = _session.OpenWrite(FileNames.GetSourceSchemaExtensionsPath(schemaName));
            await stream.WriteAsync(schemaExtensions, cancellationToken);
        }
        else
        {
            // Ensure no stale extensions linger from a previous composition.
            _session.Delete(FileNames.GetSourceSchemaExtensionsPath(schemaName));
        }

        await using (var stream = _session.OpenWrite(FileNames.GetSourceSchemaSettingsPath(schemaName)))
        {
            await using var jsonWriter = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            settings.WriteTo(jsonWriter);
            await jsonWriter.FlushAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Attempts to get a source schema configuration from the archive.
    /// </summary>
    /// <param name="schemaName">The name of the source schema to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A source schema configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when schemaName or buffer is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    public async Task<SourceSchemaConfiguration?> TryGetSourceSchemaConfigurationAsync(
        string schemaName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schemaName);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!await _session.ExistsAsync(
            FileNames.GetSourceSchemaPath(schemaName),
            FileKind.Schema,
            cancellationToken))
        {
            return null;
        }

        JsonDocument settings;
        await using (var stream = await _session.OpenReadAsync(
            FileNames.GetSourceSchemaSettingsPath(schemaName),
            FileKind.Settings,
            cancellationToken))
        {
            settings = await JsonDocument.ParseAsync(stream, default, cancellationToken);
        }

        return new SourceSchemaConfiguration(OpenReadSchemaAsync, TryOpenReadSchemaExtensionsAsync, settings);

        Task<Stream> OpenReadSchemaAsync(CancellationToken ct)
            => _session.OpenReadAsync(FileNames.GetSourceSchemaPath(schemaName), FileKind.Schema, ct);

        async Task<Stream?> TryOpenReadSchemaExtensionsAsync(CancellationToken ct)
        {
            var extensionsPath = FileNames.GetSourceSchemaExtensionsPath(schemaName);

            if (!await _session.ExistsAsync(extensionsPath, FileKind.Schema, ct))
            {
                return null;
            }

            return await _session.OpenReadAsync(extensionsPath, FileKind.Schema, ct);
        }
    }

    /// <summary>
    /// Removes a source schema configuration from the archive, deleting its files and
    /// removing it from the archive metadata.
    /// </summary>
    /// <param name="schemaName">The name of the source schema to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the source schema was present and removed; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when schemaName is null or empty.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only.</exception>
    public async Task<bool> RemoveSourceSchemaConfigurationAsync(
        string schemaName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(schemaName);
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureMutable();

        var metadata = await GetArchiveMetadataAsync(cancellationToken);

        if (metadata is null || !metadata.SourceSchemas.Contains(schemaName))
        {
            return false;
        }

        _session.Delete(FileNames.GetSourceSchemaPath(schemaName));
        _session.Delete(FileNames.GetSourceSchemaSettingsPath(schemaName));
        _session.Delete(FileNames.GetSourceSchemaExtensionsPath(schemaName));

        var updatedMetadata = metadata with
        {
            SourceSchemas = metadata.SourceSchemas.Remove(schemaName)
        };

        await SetArchiveMetadataAsync(updatedMetadata, cancellationToken);

        return true;
    }

    /// <summary>
    /// Sets the legacy archive file in the archive by copying the content from the provided stream.
    /// </summary>
    /// <param name="content">The stream containing the legacy archive content.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when content is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only.</exception>
    public async Task SetLegacyArchiveFileAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureMutable();

        await using var stream = _session.OpenWrite(FileNames.LegacyArchive);
        await content.CopyToAsync(stream, cancellationToken);
    }

    /// <summary>
    /// Attempts to get the legacy archive file from the archive as a stream.
    /// Returns null if no legacy archive file is present.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A stream to read the legacy archive content, or null if not present.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    public async Task<Stream?> TryGetLegacyArchiveFileAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!await _session.ExistsAsync(FileNames.LegacyArchive, FileKind.LegacyArchive, cancellationToken))
        {
            return null;
        }

        return await _session.OpenReadAsync(FileNames.LegacyArchive, FileKind.LegacyArchive, cancellationToken);
    }

    /// <summary>
    /// Digitally signs the archive using the provided certificate with private key.
    /// Brings the root content manifest up to date, then creates a PKCS#7/CMS detached signature over
    /// the raw bytes of that manifest.
    /// </summary>
    /// <param name="privateKey">The certificate containing the private key for signing.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when privateKey is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="ArgumentException">Thrown when the certificate does not contain a private key.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only.</exception>
    public async Task SignArchiveAsync(
        X509Certificate2 privateKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(privateKey);
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureMutable();

        if (!privateKey.HasPrivateKey)
        {
            throw new ArgumentException(
                "Certificate must contain a private key for signing.",
                nameof(privateKey));
        }

        // Bring the root manifest up to date so the signature covers the current archive contents.
        var manifestBytes = await WriteManifestAsync(cancellationToken);

        // Create the detached CMS signature over the raw manifest bytes with a signing-time attribute.
        var contentInfo = new ContentInfo(manifestBytes);
        var signedCms = new SignedCms(contentInfo, detached: true);
        var signer = new CmsSigner(privateKey);
        signer.SignedAttributes.Add(new Pkcs9SigningTime());
        signedCms.ComputeSignature(signer);
        var signatureBytes = signedCms.Encode();

        await using var stream = _session.OpenWrite(FileNames.Signature);
        await stream.WriteAsync(signatureBytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Verifies the archive against its content manifest, and against the detached signature when the
    /// archive is signed, using the provided public key certificate. The manifest integrity checks apply
    /// to every archive: the root manifest must be present; every file present in the archive, other than
    /// the manifest and the contents of the signature directory, must be listed in the manifest; listed
    /// files that are absent are permitted (a stripped archive); and every listed file that is present
    /// must match its recorded digest. When the archive carries a signature, the detached signature must
    /// additionally verify over the manifest bytes. An archive whose manifest integrity holds but that
    /// carries no signature reports <see cref="SignatureVerificationResult.NotSigned"/>.
    /// </summary>
    /// <param name="publicKey">The certificate containing the public key for verification.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The result of the verification process.</returns>
    public async Task<SignatureVerificationResult> VerifySignatureAsync(
        X509Certificate2 publicKey,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var manifestPresent = await _session.ExistsAsync(
            FileNames.Manifest,
            FileKind.Manifest,
            cancellationToken);
        var signaturePresent = await _session.ExistsAsync(
            FileNames.Signature,
            FileKind.Signature,
            cancellationToken);

        // The content manifest establishes integrity and is required for every archive. Without it
        // neither the file digests nor a signature covering the manifest can be verified.
        if (!manifestPresent)
        {
            return signaturePresent
                ? SignatureVerificationResult.ManifestMissing
                : SignatureVerificationResult.NotSigned;
        }

        var buffer = TryRentBuffer();

        try
        {
            // 1. Load the manifest bytes and parse the manifest.
            await using (var manifestStream = await _session.OpenReadAsync(
                FileNames.Manifest,
                FileKind.Manifest,
                cancellationToken))
            {
                await manifestStream.CopyToAsync(buffer, cancellationToken);
            }

            var manifestBytes = buffer.WrittenSpan.ToArray();
            var manifest = ArchiveManifestSerializer.Parse(buffer.WrittenMemory);

            // sha256 is the only supported digest algorithm.
            if (!manifest.Algorithm.Equals("sha256", StringComparison.Ordinal))
            {
                return SignatureVerificationResult.UnsupportedAlgorithm;
            }

            // 2. Every present file, other than the manifest and the signature directory, must be listed.
            foreach (var path in _session.GetFiles())
            {
                if (path.Equals(FileNames.Manifest, StringComparison.Ordinal)
                    || path.StartsWith(FileNames.SignatureDirectory, StringComparison.Ordinal))
                {
                    continue;
                }

                if (path.EndsWith("/", StringComparison.Ordinal) && _session.GetContentLength(path) == 0)
                {
                    // An empty directory placeholder carries no content and is not a listed file.
                    continue;
                }

                if (!manifest.Files.ContainsKey(path))
                {
                    return SignatureVerificationResult.UnlistedFile;
                }
            }

            // 3. Re-hash every listed file that is present; listed files that are absent are permitted.
            foreach (var file in manifest.Files)
            {
                var kind = FileNames.GetFileKind(file.Key);

                if (!await _session.ExistsAsync(file.Key, kind, cancellationToken))
                {
                    continue;
                }

                var actualHash = await ComputeFileHashAsync(file.Key, kind, cancellationToken);
                if (!actualHash.Equals(file.Value, StringComparison.Ordinal))
                {
                    return SignatureVerificationResult.FilesModified;
                }
            }

            // 4. The detached signature is verified only when the archive is signed.
            if (!signaturePresent)
            {
                return SignatureVerificationResult.NotSigned;
            }

            // 5. Verify the detached signature over the raw manifest bytes.
            buffer.Clear();
            await using (var signatureStream = await _session.OpenReadAsync(
                FileNames.Signature,
                FileKind.Signature,
                cancellationToken))
            {
                await signatureStream.CopyToAsync(buffer, cancellationToken);
            }

            var contentInfo = new ContentInfo(manifestBytes);
            var signedCms = new SignedCms(contentInfo, detached: true);
            signedCms.Decode(buffer.WrittenSpan.ToArray());
            signedCms.CheckSignature(
                new X509Certificate2Collection(publicKey),
                verifySignatureOnly: true);

            return SignatureVerificationResult.Valid;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (CryptographicException)
        {
            return SignatureVerificationResult.InvalidSignature;
        }
        catch (Exception)
        {
            return SignatureVerificationResult.VerificationFailed;
        }
        finally
        {
            TryReturnBuffer(buffer);
        }
    }

    /// <summary>
    /// Gets information about the digital signature if the archive is signed.
    /// Returns null if the archive is not signed or signature information cannot be read.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Signature information or null if not available.</returns>
    public async Task<SignatureInfo?> GetSignatureInfoAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!await _session.ExistsAsync(FileNames.Signature, FileKind.Signature, cancellationToken))
        {
            return null;
        }

        var buffer = TryRentBuffer();

        try
        {
            await using (var signatureStream = await _session.OpenReadAsync(
                FileNames.Signature,
                FileKind.Signature,
                cancellationToken))
            {
                await signatureStream.CopyToAsync(buffer, 1024, cancellationToken);
            }

            var signedCms = new SignedCms();
            signedCms.Decode(buffer.WrittenSpan.ToArray());

            var signerInfo = signedCms.SignerInfos[0];
            var certificate = signerInfo.Certificate;

            var verificationResult = certificate is null
                ? SignatureVerificationResult.NotSigned
                : await VerifySignatureAsync(certificate, cancellationToken);

            return new SignatureInfo
            {
                Timestamp = TryGetSigningTime(signerInfo),
                Algorithm = (signerInfo.DigestAlgorithm.FriendlyName
                    ?? signerInfo.DigestAlgorithm.Value
                    ?? "sha256").ToLowerInvariant(),
                SignerCertificate = certificate,
                IsValid = verificationResult is SignatureVerificationResult.Valid
            };
        }
        catch
        {
            return null;
        }
        finally
        {
            TryReturnBuffer(buffer);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the archive contains a digital signature.
    /// </summary>
    public bool IsSigned => _session.Exists(FileNames.Signature);

    private static DateTimeOffset? TryGetSigningTime(SignerInfo signerInfo)
    {
        foreach (var attribute in signerInfo.SignedAttributes)
        {
            if (attribute.Oid?.Value != "1.2.840.113549.1.9.5")
            {
                continue;
            }

            foreach (var value in attribute.Values)
            {
                if (value is Pkcs9SigningTime signingTime)
                {
                    return new DateTimeOffset(signingTime.SigningTime.ToUniversalTime());
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the root content manifest that records a digest for every file and artifact in the archive.
    /// Returns <c>null</c> when the archive contains no content manifest, which indicates a legacy or
    /// otherwise invalid archive.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The content manifest or <c>null</c> if not present.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    public async Task<ArchiveManifest?> GetManifestAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!await _session.ExistsAsync(FileNames.Manifest, FileKind.Manifest, cancellationToken))
        {
            return null;
        }

        var buffer = TryRentBuffer();

        try
        {
            await using var stream = await _session.OpenReadAsync(
                FileNames.Manifest,
                FileKind.Manifest,
                cancellationToken);
            await stream.CopyToAsync(buffer, cancellationToken);
            return ArchiveManifestSerializer.Parse(buffer.WrittenMemory);
        }
        finally
        {
            TryReturnBuffer(buffer);
        }
    }

    /// <summary>
    /// Removes the specified components from the archive without regenerating the content manifest.
    /// The root <c>manifest.json</c> and the signature directory are preserved byte-identically, so the
    /// manifest intentionally continues to list the now-absent files and any existing signature remains valid.
    /// </summary>
    /// <param name="components">The components to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the archive is read-only or has pending uncommitted changes.
    /// </exception>
    public async Task StripAsync(
        FusionArchiveComponents components,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureMutable();

        if (_session.HasUncommittedChanges)
        {
            throw new InvalidOperationException(
                "Cannot strip an archive that has pending uncommitted changes. Commit the changes first.");
        }

        if ((components & FusionArchiveComponents.SourceSchemas) == FusionArchiveComponents.SourceSchemas)
        {
            foreach (var path in _session.GetFiles().ToArray())
            {
                if (path.StartsWith(FileNames.SourceSchemas, StringComparison.Ordinal))
                {
                    _session.Delete(path);
                }
            }
        }

        if ((components & FusionArchiveComponents.CompositionSettings) == FusionArchiveComponents.CompositionSettings)
        {
            _session.Delete(FileNames.CompositionSettings);
        }

        if (_session.HasUncommittedChanges)
        {
            // Commit the deletions directly. The content manifest is deliberately not regenerated so that
            // it keeps listing the stripped files and any existing signature stays valid.
            await CommitCoreAsync(cancellationToken);
        }
    }

    private RegoPolicyFileSet[] ReadRegoPolicyFileSets()
    {
        var fileSets = new Dictionary<(Version Version, string Name), RegoPolicyFileSet>();

        foreach (var path in _session.GetFiles())
        {
            if (!path.StartsWith(FileNames.RegoPolicies, StringComparison.Ordinal))
            {
                continue;
            }

            if (path.EndsWith("/", StringComparison.Ordinal))
            {
                continue;
            }

            var relativePath = path.AsSpan(FileNames.RegoPolicies.Length);
            var separator = relativePath.IndexOf('/');

            if (separator <= 0 || separator == relativePath.Length - 1)
            {
                throw new InvalidDataException($"The Rego policy path '{path}' is invalid.");
            }

            var versionText = relativePath[..separator].ToString();
            if (!TryParseRegoPolicyVersion(versionText, out var version))
            {
                throw new InvalidDataException(
                    $"The Rego policy path '{path}' does not contain a canonical three-part version.");
            }

            var remainder = relativePath[(separator + 1)..];
            var remainderSeparator = remainder.IndexOf('/');

            if (remainderSeparator >= 0)
            {
                // Nested paths are only valid inside the 'data/' subtree, where every file must be data.json.
                if (!remainder[..remainderSeparator].SequenceEqual("data"))
                {
                    throw new InvalidDataException($"The Rego policy path '{path}' is invalid.");
                }

                if (!remainder[(remainder.LastIndexOf('/') + 1)..].SequenceEqual(FileNames.DataFile))
                {
                    throw new InvalidDataException(
                        $"The Rego policy path '{path}' is invalid because only '{FileNames.DataFile}' "
                        + "files are permitted within the 'data/' subtree.");
                }

                // Data files are validated and read through the data tree APIs, not as policy pairs.
                continue;
            }

            var fileName = remainder.ToString();
            if (fileName.Contains('\\'))
            {
                throw new InvalidDataException($"The Rego policy path '{path}' is invalid.");
            }

            if (fileName.Equals(FileNames.DataFile, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"The Rego policy path '{path}' is invalid because '{FileNames.DataFile}' files must be "
                    + "located within the 'data/' subtree.");
            }

            string policyName;
            bool isPolicy;

            if (fileName.EndsWith(".rego", StringComparison.Ordinal))
            {
                policyName = fileName[..^5];
                isPolicy = true;
            }
            else if (fileName.EndsWith(".graphql", StringComparison.Ordinal))
            {
                policyName = fileName[..^8];
                isPolicy = false;
            }
            else
            {
                if (fileName.EndsWith(".rego", StringComparison.OrdinalIgnoreCase)
                    || fileName.EndsWith(".graphql", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        $"The Rego policy path '{path}' uses an invalid file extension casing.");
                }

                continue;
            }

            if (IsReservedRegoPolicyName(policyName))
            {
                throw new InvalidDataException(
                    $"The Rego policy path '{path}' uses the reserved policy name 'data', which "
                    + "identifies the policy data subtree.");
            }

            if (!IsValidRegoPolicyName(policyName))
            {
                throw new InvalidDataException($"The Rego policy path '{path}' contains an invalid policy name.");
            }

            var key = (version, policyName);
            if (!fileSets.TryGetValue(key, out var fileSet))
            {
                fileSet = new RegoPolicyFileSet(version, policyName);
                fileSets.Add(key, fileSet);
            }

            if (isPolicy)
            {
                fileSet.HasPolicy = true;
            }
            else
            {
                fileSet.HasRequirements = true;
            }
        }

        foreach (var versionGroup in fileSets.Values.GroupBy(t => t.Version))
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var fileSet in versionGroup)
            {
                if (!names.Add(fileSet.Name))
                {
                    throw new InvalidDataException(
                        $"The Rego policy format '{fileSet.Version}' contains policy names that differ only by case.");
                }

                if (!fileSet.HasPolicy || !fileSet.HasRequirements)
                {
                    throw CreateIncompleteRegoPolicyException(fileSet.Name, fileSet.Version);
                }
            }
        }

        return fileSets.Values.ToArray();
    }

    private async Task<RegoPolicyConfiguration> CreateRegoPolicyConfigurationAsync(
        RegoPolicyFileSet fileSet,
        CancellationToken cancellationToken)
    {
        var policyPath = FileNames.GetRegoPolicyPath(fileSet.Version, fileSet.Name);
        var requirementsPath = FileNames.GetRegoPolicyRequirementsPath(fileSet.Version, fileSet.Name);

        // Extract both files before returning the lazy readers. This applies the configured
        // archive size limits to the complete policy pair during catalog loading.
        await _session.ExistsAsync(policyPath, FileKind.Policy, cancellationToken);
        await _session.ExistsAsync(requirementsPath, FileKind.Schema, cancellationToken);

        return new RegoPolicyConfiguration(
            fileSet.Name,
            fileSet.Version,
            OpenReadPolicyAsync,
            OpenReadRequirementsAsync);

        Task<Stream> OpenReadPolicyAsync(CancellationToken ct)
            => _session.OpenReadAsync(policyPath, FileKind.Policy, ct);

        Task<Stream> OpenReadRequirementsAsync(CancellationToken ct)
            => _session.OpenReadAsync(requirementsPath, FileKind.Schema, ct);
    }

    private static InvalidDataException CreateIncompleteRegoPolicyException(
        string policyName,
        Version version)
        => new(
            $"The Rego policy '{policyName}' in format '{version}' must contain both "
            + $"'{policyName}.rego' and '{policyName}.graphql'.");

    private static void ValidateRegoPolicyName(string policyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(policyName);

        if (IsReservedRegoPolicyName(policyName))
        {
            throw new ArgumentException(
                "The Rego policy name 'data' is reserved for the policy data subtree.",
                nameof(policyName));
        }

        if (!IsValidRegoPolicyName(policyName))
        {
            throw new ArgumentException("Invalid Rego policy name.", nameof(policyName));
        }
    }

    // 'data' names the shared policy data subtree, so it cannot also identify a policy pair without
    // colliding with the 'policies/<language>/<version>/data' artifact key.
    private static bool IsReservedRegoPolicyName(string policyName)
        => policyName.Equals("data", StringComparison.Ordinal);

    private static bool IsValidRegoPolicyName(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName) || policyName is "." or "..")
        {
            return false;
        }

        foreach (var character in policyName)
        {
            if (character is '/' or '\\' || char.IsControl(character))
            {
                return false;
            }
        }

        return true;
    }

    private static void ValidateRegoPolicyVersion(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);

        if (version.Build < 0 || version.Revision >= 0)
        {
            throw new ArgumentException(
                "The Rego policy format version must contain exactly three components.",
                nameof(version));
        }
    }

    private static bool TryParseRegoPolicyVersion(string value, out Version version)
    {
        if (Version.TryParse(value, out var parsed)
            && parsed.Build >= 0
            && parsed.Revision < 0
            && parsed.ToString(3).Equals(value, StringComparison.Ordinal))
        {
            version = parsed;
            return true;
        }

        version = null!;
        return false;
    }

    private List<RegoDataMount> ReadRegoDataMounts(Version version)
    {
        var directory = FileNames.GetRegoDataDirectory(version);
        var mounts = new List<RegoDataMount>();

        foreach (var path in _session.GetFiles())
        {
            if (path.EndsWith("/", StringComparison.Ordinal)
                || !path.StartsWith(directory, StringComparison.Ordinal))
            {
                continue;
            }

            var relative = path[directory.Length..];
            var lastSeparator = relative.LastIndexOf('/');
            var fileName = lastSeparator < 0 ? relative : relative[(lastSeparator + 1)..];

            if (!fileName.Equals(FileNames.DataFile, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"The Rego data path '{path}' is invalid because only '{FileNames.DataFile}' "
                    + "files are permitted within the 'data/' subtree.");
            }

            var mountPath = lastSeparator < 0 ? string.Empty : relative[..lastSeparator];
            mounts.Add(new RegoDataMount(mountPath, path));
        }

        return mounts;
    }

    private static string[] ValidateRegoDataMountPath(string mountPath)
    {
        if (mountPath.Length == 0)
        {
            return [];
        }

        var segments = mountPath.Split('/');

        foreach (var segment in segments)
        {
            if (!IsValidRegoPolicyName(segment))
            {
                throw new ArgumentException(
                    $"The data mount path '{mountPath}' contains an invalid path segment.",
                    nameof(mountPath));
            }
        }

        return segments;
    }

    private static JsonDocument ParseRegoDataObject(ReadOnlyMemory<byte> data)
    {
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(data);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("The Rego data document must be valid JSON.", nameof(data), exception);
        }

        if (document.RootElement.ValueKind is not JsonValueKind.Object)
        {
            document.Dispose();
            throw new ArgumentException("The Rego data document must be a JSON object.", nameof(data));
        }

        return document;
    }

    private async Task EnsureNoRegoDataConflictAsync(
        Version version,
        string mountPath,
        string[] segments,
        JsonElement document,
        CancellationToken cancellationToken)
    {
        foreach (var existing in ReadRegoDataMounts(version))
        {
            if (existing.MountPath.Equals(mountPath, StringComparison.Ordinal))
            {
                // Writing the same mount replaces its previous document.
                continue;
            }

            var existingSegments = existing.MountPath.Length == 0
                ? []
                : existing.MountPath.Split('/');

            if (IsRegoDataMountPrefix(existingSegments, segments))
            {
                // The existing mount is an ancestor; its document must not already define the path the
                // new mount occupies.
                await using var stream = await _session.OpenReadAsync(
                    existing.Path,
                    FileKind.PolicyData,
                    cancellationToken);
                using var existingDocument = await JsonDocument.ParseAsync(stream, default, cancellationToken);

                if (RegoDataDocumentDefinesMountPath(
                    existingDocument.RootElement,
                    segments.AsSpan(existingSegments.Length)))
                {
                    throw CreateRegoDataConflictException(mountPath, existing.MountPath);
                }
            }
            else if (IsRegoDataMountPrefix(segments, existingSegments))
            {
                // The new mount is an ancestor; its document must not define the path the existing mount occupies.
                if (RegoDataDocumentDefinesMountPath(document, existingSegments.AsSpan(segments.Length)))
                {
                    throw CreateRegoDataConflictException(mountPath, existing.MountPath);
                }
            }
        }
    }

    private static bool IsRegoDataMountPrefix(string[] prefix, string[] path)
    {
        if (prefix.Length >= path.Length)
        {
            return false;
        }

        for (var i = 0; i < prefix.Length; i++)
        {
            if (!prefix[i].Equals(path[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    // Determines whether the document at an ancestor mount already defines the mount point that a
    // descendant mount occupies. The descendant conflicts when the ancestor defines a value exactly at
    // the mount point, or a non-object value at any segment along the way (which would block descending
    // into a container). Disjoint sibling keys along the path are not a conflict.
    private static bool RegoDataDocumentDefinesMountPath(JsonElement document, ReadOnlySpan<string> relativeSegments)
    {
        var current = document;

        foreach (var segment in relativeSegments)
        {
            if (current.ValueKind is not JsonValueKind.Object)
            {
                return true;
            }

            if (!current.TryGetProperty(segment, out var next))
            {
                return false;
            }

            current = next;
        }

        return true;
    }

    private static bool RegoDataDocumentDefinesMountPath(JsonObject document, ReadOnlySpan<string> relativeSegments)
    {
        JsonNode? current = document;

        foreach (var segment in relativeSegments)
        {
            if (current is not JsonObject next)
            {
                return true;
            }

            if (!next.TryGetPropertyValue(segment, out current))
            {
                return false;
            }
        }

        return true;
    }

    private static JsonObject NavigateOrCreateRegoDataObject(JsonObject root, string[] segments, string mountPath)
    {
        var current = root;

        foreach (var segment in segments)
        {
            if (current[segment] is JsonObject child)
            {
                current = child;
            }
            else if (current.ContainsKey(segment))
            {
                throw new InvalidDataException(
                    $"The Rego data mount '{mountPath}' conflicts with another mount on the key '{segment}'.");
            }
            else
            {
                child = new JsonObject();
                current[segment] = child;
                current = child;
            }
        }

        return current;
    }

    private static void MergeRegoDataObject(JsonObject destination, JsonObject source, string mountPath)
    {
        foreach (var key in source.Select(property => property.Key).ToArray())
        {
            var value = source[key];
            source.Remove(key);

            if (!destination.TryAdd(key, value))
            {
                throw new InvalidDataException(
                    $"The Rego data mount '{mountPath}' conflicts with another mount on the key '{key}'.");
            }
        }
    }

    private static InvalidOperationException CreateRegoDataConflictException(
        string mountPath,
        string existingMountPath)
    {
        var newLabel = mountPath.Length == 0 ? "the data root" : $"'{mountPath}'";
        var existingLabel = existingMountPath.Length == 0 ? "the data root" : $"'{existingMountPath}'";

        return new InvalidOperationException(
            $"The Rego data mount at {newLabel} conflicts with the existing mount at {existingLabel} "
            + "because their documents define overlapping paths.");
    }

    private static InvalidDataException CreateRegoDataConflictDataException(
        string ancestorMountPath,
        string descendantMountPath)
    {
        var ancestorLabel = ancestorMountPath.Length == 0 ? "the data root" : $"'{ancestorMountPath}'";
        var descendantLabel = descendantMountPath.Length == 0 ? "the data root" : $"'{descendantMountPath}'";

        return new InvalidDataException(
            $"The Rego data mount at {descendantLabel} conflicts with the mount at {ancestorLabel} "
            + "because their documents define overlapping paths.");
    }

    private async Task EnsureNoRegoBaseVirtualConflictForDataAsync(
        Version version,
        string mountPath,
        string[] dataSegments,
        JsonElement dataDocument,
        CancellationToken cancellationToken)
    {
        foreach (var policySegments in await ReadRegoPolicyPackageRootsAsync(version, cancellationToken))
        {
            if (RegoSegmentsEqual(dataSegments, policySegments)
                || IsRegoDataMountPrefix(policySegments, dataSegments))
            {
                // The data mount sits at or inside the policy's virtual document subtree.
                throw CreateRegoBaseVirtualConflictException(mountPath, policySegments);
            }

            if (IsRegoDataMountPrefix(dataSegments, policySegments)
                && RegoDataDocumentDefinesMountPath(dataDocument, policySegments.AsSpan(dataSegments.Length)))
            {
                // The data mount is an ancestor whose document defines the policy's virtual root.
                throw CreateRegoBaseVirtualConflictException(mountPath, policySegments);
            }
        }
    }

    private async Task EnsureNoRegoBaseVirtualConflictForPolicyAsync(
        Version version,
        string[] policySegments,
        CancellationToken cancellationToken)
    {
        foreach (var mount in ReadRegoDataMounts(version))
        {
            var dataSegments = mount.MountPath.Length == 0 ? [] : mount.MountPath.Split('/');

            if (RegoSegmentsEqual(dataSegments, policySegments)
                || IsRegoDataMountPrefix(policySegments, dataSegments))
            {
                throw CreateRegoBaseVirtualConflictException(mount.MountPath, policySegments);
            }

            if (!IsRegoDataMountPrefix(dataSegments, policySegments))
            {
                continue;
            }

            bool defines;
            await using (var stream = await _session.OpenReadAsync(
                mount.Path,
                FileKind.PolicyData,
                cancellationToken))
            {
                using var document = await JsonDocument.ParseAsync(stream, default, cancellationToken);
                defines = RegoDataDocumentDefinesMountPath(
                    document.RootElement,
                    policySegments.AsSpan(dataSegments.Length));
            }

            if (defines)
            {
                throw CreateRegoBaseVirtualConflictException(mount.MountPath, policySegments);
            }
        }
    }

    private async Task<List<string[]>> ReadRegoPolicyPackageRootsAsync(
        Version version,
        CancellationToken cancellationToken)
    {
        List<string[]>? roots = null;

        foreach (var fileSet in ReadRegoPolicyFileSets())
        {
            if (fileSet.Version != version)
            {
                continue;
            }

            var buffer = new ArrayBufferWriter<byte>();
            await using (var stream = await _session.OpenReadAsync(
                FileNames.GetRegoPolicyPath(version, fileSet.Name),
                FileKind.Policy,
                cancellationToken))
            {
                await stream.CopyToAsync(buffer, cancellationToken);
            }

            if (TryScanRegoPackageSegments(buffer.WrittenSpan, out var segments))
            {
                (roots ??= []).Add(segments);
            }
        }

        return roots ?? [];
    }

    private static bool RegoSegmentsEqual(string[] left, string[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if (!left[i].Equals(right[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    // Reads the package path a Rego policy declares (for example 'package a.b.c' yields ["a", "b", "c"]).
    // Package references that use bracket or string notation cannot be mapped to a simple path and are
    // reported as not scanned, so the conflict check is skipped rather than risk a false rejection.
    private static bool TryScanRegoPackageSegments(ReadOnlySpan<byte> source, out string[] segments)
    {
        // Skip a leading UTF-8 byte order mark so a policy authored with one is scanned correctly.
        if (source.Length >= 3 && source[0] == 0xEF && source[1] == 0xBB && source[2] == 0xBF)
        {
            source = source[3..];
        }

        var token = ScanRegoPackageToken(Encoding.UTF8.GetString(source));

        if (string.IsNullOrEmpty(token))
        {
            segments = [];
            return false;
        }

        var parts = token.Split('.');

        foreach (var part in parts)
        {
            if (!IsRegoPackageSegment(part))
            {
                segments = [];
                return false;
            }
        }

        segments = parts;
        return true;
    }

    private static string? ScanRegoPackageToken(string source)
    {
        var span = source.AsSpan();
        var i = 0;

        while (i < span.Length)
        {
            while (i < span.Length && char.IsWhiteSpace(span[i]))
            {
                i++;
            }

            if (i >= span.Length)
            {
                break;
            }

            if (span[i] == '#')
            {
                while (i < span.Length && span[i] != '\n')
                {
                    i++;
                }

                continue;
            }

            const string keyword = "package";

            if (span[i..].StartsWith(keyword)
                && i + keyword.Length < span.Length
                && char.IsWhiteSpace(span[i + keyword.Length]))
            {
                i += keyword.Length;

                while (i < span.Length && char.IsWhiteSpace(span[i]))
                {
                    i++;
                }

                var start = i;

                while (i < span.Length && !char.IsWhiteSpace(span[i]) && span[i] != '#')
                {
                    i++;
                }

                return span[start..i].ToString();
            }

            while (i < span.Length && span[i] != '\n')
            {
                i++;
            }
        }

        return null;
    }

    private static bool IsRegoPackageSegment(string segment)
    {
        if (segment.Length == 0)
        {
            return false;
        }

        foreach (var character in segment)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character != '_')
            {
                return false;
            }
        }

        return true;
    }

    private static InvalidOperationException CreateRegoBaseVirtualConflictException(
        string mountPath,
        string[] policySegments)
    {
        var mountLabel = mountPath.Length == 0 ? "the data root" : $"'{mountPath}'";
        var virtualPath = "data." + string.Join('.', policySegments);

        return new InvalidOperationException(
            $"The Rego data mount at {mountLabel} conflicts with the virtual document '{virtualPath}' "
            + "defined by a policy package because a data document and a policy package must not define "
            + "overlapping paths.");
    }

    private static InvalidDataException CreateRegoBaseVirtualConflictDataException(
        string mountPath,
        string[] policySegments)
    {
        var mountLabel = mountPath.Length == 0 ? "the data root" : $"'{mountPath}'";
        var virtualPath = "data." + string.Join('.', policySegments);

        return new InvalidDataException(
            $"The Rego data mount at {mountLabel} conflicts with the virtual document '{virtualPath}' "
            + "defined by a policy package because a data document and a policy package must not define "
            + "overlapping paths.");
    }

    /// <summary>
    /// We will try to work with a single buffer for all file interactions.
    /// </summary>
    private ArrayBufferWriter<byte> TryRentBuffer()
    {
        return Interlocked.Exchange(ref _buffer, null) ?? new ArrayBufferWriter<byte>(4096);
    }

    private sealed class RegoPolicyFileSet(Version version, string name)
    {
        public Version Version { get; } = version;

        public string Name { get; } = name;

        public bool HasPolicy { get; set; }

        public bool HasRequirements { get; set; }
    }

    private readonly record struct RegoDataMount(string MountPath, string Path);

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

    private async Task<byte[]> WriteManifestAsync(CancellationToken cancellationToken)
    {
        var manifest = await GenerateManifestAsync(cancellationToken);
        var buffer = TryRentBuffer();

        try
        {
            ArchiveManifestSerializer.Format(manifest, buffer);
            var bytes = buffer.WrittenSpan.ToArray();

            await using var stream = _session.OpenWrite(FileNames.Manifest);
            await stream.WriteAsync(bytes, cancellationToken);
            await stream.FlushAsync(cancellationToken);

            return bytes;
        }
        finally
        {
            TryReturnBuffer(buffer);
        }
    }

    private async Task<ArchiveManifest> GenerateManifestAsync(CancellationToken cancellationToken)
    {
        var files = ImmutableSortedDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);

        foreach (var path in _session.GetFiles())
        {
            if (!IsManifestListedFile(path))
            {
                continue;
            }

            var kind = FileNames.GetFileKind(path);
            files[path] = await ComputeFileHashAsync(path, kind, cancellationToken);
        }

        var fileDigests = files.ToImmutable();

        return new ArchiveManifest
        {
            Version = "1.0.0",
            Algorithm = "sha256",
            Files = fileDigests,
            Artifacts = ComputeArtifactDigests(fileDigests)
        };
    }

    private static bool IsManifestListedFile(string path)
        => !path.EndsWith("/", StringComparison.Ordinal)
            && !path.Equals(FileNames.Manifest, StringComparison.Ordinal)
            && !path.StartsWith(FileNames.SignatureDirectory, StringComparison.Ordinal);

    private static ImmutableSortedDictionary<string, string> ComputeArtifactDigests(
        ImmutableSortedDictionary<string, string> files)
    {
        var members = new Dictionary<string, List<KeyValuePair<string, string>>>(StringComparer.Ordinal);

        foreach (var file in files)
        {
            var key = TryGetArtifactKey(file.Key);

            if (key is null)
            {
                continue;
            }

            if (!members.TryGetValue(key, out var list))
            {
                list = [];
                members.Add(key, list);
            }

            list.Add(file);
        }

        var artifacts = ImmutableSortedDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);

        foreach (var (key, list) in members)
        {
            artifacts.Add(key, ComputeArtifactDigest(list));
        }

        return artifacts.ToImmutable();
    }

    private static string? TryGetArtifactKey(string path)
    {
        var segments = path.Split('/');

        switch (segments[0])
        {
            case "gateway" when segments.Length >= 3:
                return $"gateway/{segments[1]}";

            case "source-schemas" when segments.Length >= 3:
                return $"source-schemas/{segments[1]}";

            case "policies" when segments.Length >= 5 && segments[3] == "data":
                return $"policies/{segments[1]}/{segments[2]}/data";

            case "policies" when segments.Length == 4 && segments[3].EndsWith(".rego", StringComparison.Ordinal):
                return $"policies/{segments[1]}/{segments[2]}/{segments[3][..^5]}";

            case "policies" when segments.Length == 4 && segments[3].EndsWith(".graphql", StringComparison.Ordinal):
                return $"policies/{segments[1]}/{segments[2]}/{segments[3][..^8]}";

            default:
                return null;
        }
    }

    private static string ComputeArtifactDigest(List<KeyValuePair<string, string>> members)
    {
        // Build the "<path>:<digest>" line for each member and sort the lines by ordinal (byte) order
        // over their UTF-8 encoding, as the specification requires. Sorting the composed lines rather
        // than the paths keeps the order well defined for supplementary-plane characters, whose UTF-8
        // byte order differs from UTF-16 code-unit order, and for degenerate prefix and colon cases.
        var lines = new byte[members.Count][];

        for (var i = 0; i < members.Count; i++)
        {
            lines[i] = Encoding.UTF8.GetBytes(members[i].Key + ":" + members[i].Value);
        }

        Array.Sort(lines, static (left, right) => left.AsSpan().SequenceCompareTo(right.AsSpan()));

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        foreach (var line in lines)
        {
            hash.AppendData(line);
            hash.AppendData("\n"u8);
        }

        return "sha256:" + ToHexLower(hash.GetHashAndReset());
    }

    private async Task<string> ComputeFileHashAsync(string path, FileKind kind, CancellationToken cancellationToken)
    {
        await using var stream = await _session.OpenReadAsync(path, kind, cancellationToken);
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        return "sha256:" + ToHexLower(hashBytes);
    }

    private static string ToHexLower(ReadOnlySpan<byte> bytes)
#if NET9_0_OR_GREATER
        => Convert.ToHexStringLower(bytes);
#else
        => Convert.ToHexString(bytes).ToLowerInvariant();
#endif

    private void EnsureMutable()
    {
        if (_mode is FusionArchiveMode.Read)
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

        if (_mode is FusionArchiveMode.Read)
        {
            throw new InvalidOperationException("Cannot commit changes to a read-only archive.");
        }

        if (_session.HasUncommittedChanges)
        {
            // Regenerate the content manifest so it always reflects the committed archive contents.
            // Determinism guarantees that unchanged content yields a byte-identical manifest.
            await WriteManifestAsync(cancellationToken);
            await CommitCoreAsync(cancellationToken);
        }
    }

    private async Task CommitCoreAsync(CancellationToken cancellationToken)
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
            _mode = FusionArchiveMode.Update;
            _session.SetMode(_mode);
        }
        else
        {
            _mode = FusionArchiveMode.Read;
        }
    }

    /// <summary>
    /// Releases all resources used by the FusionArchive.
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
