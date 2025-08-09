using System.Buffers;
using System.Collections.Immutable;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
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
    private ZipArchive _archive;
    private FusionArchiveMode _mode;
    private ArrayBufferWriter<byte>? _buffer;
    private ArchiveMetadata? _metadata;
    private bool _disposed;

    private FusionArchive(Stream stream, FusionArchiveMode mode, bool leaveOpen = false)
    {
        _stream = stream;
        _mode = mode;
        _leaveOpen = leaveOpen;
        _archive = new ZipArchive(stream, (ZipArchiveMode)mode, leaveOpen);
        _session = new ArchiveSession(_archive, mode);
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
        return new FusionArchive(stream, FusionArchiveMode.Create, leaveOpen);
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
    /// <returns>A FusionArchive instance opened in the specified mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public static FusionArchive Open(
        Stream stream,
        FusionArchiveMode mode = FusionArchiveMode.Read,
        bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return new FusionArchive(stream, mode, leaveOpen);
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

        if (!await _session.ExistsAsync(FileNames.ArchiveMetadata, cancellationToken))
        {
            return null;
        }

        var buffer = TryRentBuffer();

        try
        {
            await using var stream = await _session.OpenReadAsync(FileNames.ArchiveMetadata, cancellationToken);
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

        if (!await _session.ExistsAsync(FileNames.CompositionSettings, cancellationToken))
        {
            return null;
        }

        await using var stream = await _session.OpenReadAsync(FileNames.CompositionSettings, cancellationToken);
        return await JsonDocument.ParseAsync(stream, default, cancellationToken);
    }

    /// <summary>
    /// Sets the gateway schema for a specific format version.
    /// The version must be declared in the archive metadata before calling this method.
    /// </summary>
    /// <param name="schema">The gateway schema as a GraphQL schema string.</param>
    /// <param name="version">The gateway format version.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when schema is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when version is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only, metadata is missing, or version is not declared.</exception>
    public async Task SetGatewaySchemaAsync(
        string schema,
        Version version,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(schema);
        ArgumentNullException.ThrowIfNull(version);
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureMutable();

        await SetGatewaySchemaAsync(Encoding.UTF8.GetBytes(schema), version, cancellationToken);
    }

    /// <summary>
    /// Sets the gateway schema for a specific format version using raw bytes.
    /// The version must be declared in the archive metadata before calling this method.
    /// </summary>
    /// <param name="schema">The gateway schema as UTF-8 encoded bytes.</param>
    /// <param name="version">The gateway format version.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when version is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only, metadata is missing, or version is not declared.</exception>
    public async Task SetGatewaySchemaAsync(
        ReadOnlyMemory<byte> schema,
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

        await using var stream = _session.OpenWrite(FileNames.GetGatewaySchemaPath(version));
        await stream.WriteAsync(schema, cancellationToken);
    }

    /// <summary>
    /// Attempts to get a gateway schema with the highest version that is less than or equal to the specified maximum version.
    /// The schema data is written to the provided buffer.
    /// </summary>
    /// <param name="maxVersion">The maximum version to consider.</param>
    /// <param name="buffer">The buffer to write the schema data to.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating whether resolution was successful and the actual version used.</returns>
    /// <exception cref="ArgumentNullException">Thrown when maxVersion or buffer is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no supported gateway formats are found.</exception>
    public async Task<ResolvedGatewaySchemaResult> TryGetGatewaySchemaAsync(
        Version maxVersion,
        IBufferWriter<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(maxVersion);
        ArgumentNullException.ThrowIfNull(buffer);
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
            return new ResolvedGatewaySchemaResult { IsResolved = false, ActualVersion = null };
        }

        await using var stream = await _session.OpenReadAsync(
            FileNames.GetGatewaySchemaPath(version),
            cancellationToken);
        await stream.CopyToAsync(buffer, cancellationToken);
        return new ResolvedGatewaySchemaResult { IsResolved = true, ActualVersion = version };
    }

    /// <summary>
    /// Sets the gateway settings for a specific format version.
    /// The version must be declared in the archive metadata before calling this method.
    /// </summary>
    /// <param name="settings">The gateway settings as a JSON document.</param>
    /// <param name="version">The gateway format version.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when settings or version is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only, metadata is missing, or version is not declared.</exception>
    public async Task SetGatewaySettingsAsync(
        JsonDocument settings,
        Version version,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
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

        Exception? exception = null;
        await using var stream = _session.OpenWrite(FileNames.GetGatewaySettingsPath(version));
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
    /// Attempts to get gateway settings with the highest version that is less than or equal to the specified maximum version.
    /// </summary>
    /// <param name="maxVersion">The maximum version to consider.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result indicating whether resolution was successful, the actual version used, and the settings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when maxVersion is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no supported gateway formats are found.</exception>
    public async Task<ResolvedGatewaySettingsResult> TryGetGatewaySettingsAsync(
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
            return new ResolvedGatewaySettingsResult { IsResolved = false, ActualVersion = null, Settings = null };
        }

        if (!await _session.ExistsAsync(FileNames.GetGatewaySettingsPath(version), cancellationToken))
        {
            return new ResolvedGatewaySettingsResult { IsResolved = false, ActualVersion = null, Settings = null };
        }

        await using var stream = await _session.OpenReadAsync(
            FileNames.GetGatewaySettingsPath(version),
            cancellationToken);
        var settings = await JsonDocument.ParseAsync(stream, default, cancellationToken);
        return new ResolvedGatewaySettingsResult { IsResolved = true, ActualVersion = version, Settings = settings };
    }

    /// <summary>
    /// Sets a source schema in the archive.
    /// The schema name must be declared in the archive metadata before calling this method.
    /// </summary>
    /// <param name="schemaName">The name of the source schema.</param>
    /// <param name="schema">The source schema as UTF-8 encoded bytes.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when schemaName is null, empty, or invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when schema is empty.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive is read-only, metadata is missing, or schema name is not declared.</exception>
    public async Task SetSourceSchemaAsync(
        string schemaName,
        ReadOnlyMemory<byte> schema,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(schemaName);
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

        await using var stream = _session.OpenWrite(FileNames.GetSourceSchemaPath(schemaName));
        await stream.WriteAsync(schema, cancellationToken);
    }

    /// <summary>
    /// Attempts to get a source schema from the archive.
    /// The schema data is written to the provided buffer if found.
    /// </summary>
    /// <param name="schemaName">The name of the source schema to retrieve.</param>
    /// <param name="buffer">The buffer to write the schema data to.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the schema was found and retrieved; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when schemaName or buffer is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    public async Task<bool> TryGetSourceSchemaAsync(
        string schemaName,
        IBufferWriter<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schemaName);
        ArgumentNullException.ThrowIfNull(buffer);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!await _session.ExistsAsync(FileNames.GetSourceSchemaPath(schemaName), cancellationToken))
        {
            return false;
        }

        await using var stream = await _session.OpenReadAsync(
            FileNames.GetSourceSchemaPath(schemaName),
            cancellationToken);
        await stream.CopyToAsync(buffer, cancellationToken);
        return true;
    }

    /// <summary>
    /// Digitally signs the archive using the provided certificate with private key.
    /// Creates a manifest of all files and their SHA-256 hashes, then signs the manifest.
    /// </summary>
    /// <param name="privateKey">The certificate containing the private key for signing.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when privateKey is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the archive has been disposed.</exception>
    /// <exception cref="ArgumentException">Thrown when the certificate does not contain a private key.</exception>
    public async Task SignArchiveAsync(
        X509Certificate2 privateKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(privateKey);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!privateKey.HasPrivateKey)
        {
            throw new ArgumentException(
                "Certificate must contain a private key for signing.",
                nameof(privateKey));
        }

        // 1. Generate manifest of all non-signature files
        var manifest = await GenerateManifestAsync(cancellationToken);

        // 2. Create detached signature
        var buffer = TryRentBuffer();

        try
        {
            SignatureManifestSerializer.Format(manifest, buffer, writeManifestHash: true);
            var contentInfo = new ContentInfo(buffer.WrittenSpan.ToArray());
            var signedCms = new SignedCms(contentInfo, detached: true);
            var signer = new CmsSigner(privateKey);
            signedCms.ComputeSignature(signer);
            var signatureBytes = signedCms.Encode();

            await using (var stream = _session.OpenWrite(FileNames.SignatureManifest))
            {
                await stream.WriteAsync(buffer.WrittenMemory, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            await using (var stream = _session.OpenWrite(FileNames.Signature))
            {
                await stream.WriteAsync(signatureBytes, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
        }
        finally
        {
            TryReturnBuffer(buffer);
        }
    }

    /// <summary>
    /// Verifies the digital signature of the archive using the provided public key certificate.
    /// Checks file integrity, manifest hash, and cryptographic signature validity.
    /// </summary>
    /// <param name="publicKey">The certificate containing the public key for verification.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The result of the signature verification process.</returns>
    public async Task<SignatureVerificationResult> VerifySignatureAsync(
        X509Certificate2 publicKey,
        CancellationToken cancellationToken = default)
    {
        var manifestExists = await _session.ExistsAsync(FileNames.SignatureManifest, cancellationToken);
        var signatureExists = await _session.ExistsAsync(FileNames.Signature, cancellationToken);

        if (!manifestExists || !signatureExists)
        {
            return SignatureVerificationResult.NotSigned;
        }

        var buffer = TryRentBuffer();

        try
        {
            // 1. Load manifest and signature
            await using var manifestStream = await _session.OpenReadAsync(
                FileNames.SignatureManifest,
                cancellationToken);
            await using var signatureStream = await _session.OpenReadAsync(
                FileNames.Signature,
                cancellationToken);
            await manifestStream.CopyToAsync(buffer, cancellationToken);
            var manifest = SignatureManifestSerializer.Parse(buffer.WrittenMemory);
            var contentInfo = new ContentInfo(buffer.WrittenSpan.ToArray());

            buffer.Clear();
            await signatureStream.CopyToAsync(buffer, cancellationToken);
            var signatureBytes = buffer.WrittenSpan.ToArray();

            // 2. Verify file integrity
            foreach (var file in manifest.Files.OrderBy(t => t.Key))
            {
                if (!await _session.ExistsAsync(file.Key, cancellationToken))
                {
                    return SignatureVerificationResult.FilesMissing;
                }

                var actualHash = await ComputeFileHashAsync(file.Key, cancellationToken);
                if (!actualHash.Equals(file.Value, StringComparison.OrdinalIgnoreCase))
                {
                    return SignatureVerificationResult.FilesModified;
                }
            }

            // 3. Verify manifest hash
            buffer.Clear();
            SignatureManifestSerializer.Format(manifest, buffer, writeManifestHash: false);
            var manifestHash = ComputeManifestHash(buffer.WrittenSpan);

            if (manifest.ManifestHash?.Equals(manifestHash, StringComparison.OrdinalIgnoreCase) != true)
            {
                return SignatureVerificationResult.ManifestCorrupted;
            }

            // 4. Verify cryptographic signature
            var signedCms  = new SignedCms(contentInfo, detached: true);
            signedCms.Decode(signatureBytes);
            signedCms.CheckSignature(
                new X509Certificate2Collection(publicKey),
                verifySignatureOnly: true);

            return SignatureVerificationResult.Valid;
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
        var manifestExists = await _session.ExistsAsync(FileNames.SignatureManifest, cancellationToken);
        var signatureExists = await _session.ExistsAsync(FileNames.Signature, cancellationToken);

        if (!manifestExists || !signatureExists)
        {
            return null;
        }

        var buffer = TryRentBuffer();

        try
        {
            await using var manifestStream = await _session.OpenReadAsync(
                FileNames.SignatureManifest,
                cancellationToken);
            await using var signatureStream = await _session.OpenReadAsync(
                FileNames.Signature,
                cancellationToken);

            await manifestStream.CopyToAsync(buffer, 1024, cancellationToken);
            var manifest = SignatureManifestSerializer.Parse(buffer.WrittenMemory);
            buffer.Clear();

            await signatureStream.CopyToAsync(buffer, 1024, cancellationToken);

            var signedCms = new SignedCms();
            signedCms.Decode(buffer.WrittenSpan.ToArray());

            var signerInfo = signedCms.SignerInfos[0];
            var certificate = signerInfo.Certificate;

            var verificationResult = certificate is null
                ? SignatureVerificationResult.NotSigned
                : await VerifySignatureAsync(certificate, cancellationToken);

            return new SignatureInfo
            {
                Timestamp = manifest.Timestamp,
                Algorithm = manifest.Algorithm,
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
    public bool IsSigned => _session.Exists(FileNames.SignatureManifest);

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
        var currentCapacity = _buffer?.Capacity ?? 0;

        if (currentCapacity < buffer.Capacity)
        {
            Interlocked.CompareExchange(ref _buffer, buffer, currentBuffer);
        }
    }

    private async Task<SignatureManifest> GenerateManifestAsync(CancellationToken cancellationToken)
    {
        var files = ImmutableDictionary.CreateBuilder<string, string>();

        foreach (var path in _session.GetFiles().Order())
        {
            if (path.StartsWith(".signature/"))
            {
                // Skip signature files
                continue;
            }

            files[path] = await ComputeFileHashAsync(path, cancellationToken);
        }

        var manifest = new SignatureManifest
        {
            Version = "1.0.0",
            Algorithm = "SHA256",
            Timestamp = DateTime.UtcNow,
            Files = files.ToImmutable()
        };

        var buffer = TryRentBuffer();
        try
        {
            SignatureManifestSerializer.Format(manifest, buffer);
            return manifest with { ManifestHash = ComputeManifestHash(buffer.WrittenSpan) };
        }
        finally
        {
            TryReturnBuffer(buffer);
        }
    }

    private async Task<string> ComputeFileHashAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = await _session.OpenReadAsync(path, cancellationToken);
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
#if NET9_0_OR_GREATER
        return "sha256:" + Convert.ToHexStringLower(hashBytes);
#else
        return "sha256:" + Convert.ToHexString(hashBytes).ToLowerInvariant();
#endif
    }

    private static string ComputeManifestHash(ReadOnlySpan<byte> data)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.TryHashData(data, hash, out _);
#if NET9_0_OR_GREATER
        return "sha256:" + Convert.ToHexStringLower(hash);
#else
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
#endif
    }

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
