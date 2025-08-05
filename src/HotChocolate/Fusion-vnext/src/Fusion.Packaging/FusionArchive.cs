using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HotChocolate.Fusion.Packaging.Serializers;

namespace HotChocolate.Fusion.Packaging;

public sealed class FusionArchive : IDisposable
{
    private readonly ZipArchive _archive;
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private ArrayBufferWriter<byte>? _buffer;

    private FusionArchive(Stream stream, FusionArchiveMode mode, bool leaveOpen = false)
    {
        _stream = stream;
        _leaveOpen = leaveOpen;
        _archive = new ZipArchive(stream, (ZipArchiveMode)mode, leaveOpen);
    }

    public static FusionArchive Create(Stream stream, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return new FusionArchive(stream, FusionArchiveMode.Create, leaveOpen);
    }

    public static FusionArchive Open(
        Stream stream,
        FusionArchiveMode mode = FusionArchiveMode.Read,
        bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return new FusionArchive(stream, mode, leaveOpen);
    }

    public async Task SetArchiveMetadataAsync(
        ArchiveMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        Exception? exception = null;
        var entry = CreateEntry(FileNames.ArchiveMetadata);

#if NET10_0_OR_GREATER
        await using var stream = await entry.OpenAsync(cancellationToken);
#else
        using var stream = entry.Open();
#endif

        var writer = PipeWriter.Create(stream);

        try
        {
            ArchiveMetadataSerializer.Format(metadata, writer);
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

    public async Task<ArchiveMetadata?> GetArchiveMetadataAsync(
        CancellationToken cancellationToken = default)
    {
        var entry = _archive.GetEntry(FileNames.ArchiveMetadata);

        if (entry == null)
        {
            return null;
        }

        // Use the uncompressed size to initialize the buffer efficiently
        var expectedStreamLength = (int)entry.Length;
        var buffer = TryRentBuffer(expectedStreamLength);

        try
        {
#if NET10_0_OR_GREATER
            await using var stream = await entry.OpenAsync(cancellationToken);
#else
            using var stream = entry.Open();
#endif
            await stream.CopyToAsync(buffer, expectedStreamLength, cancellationToken);
            return ArchiveMetadataSerializer.Parse(buffer.WrittenMemory);
        }
        finally
        {
            TryReturnBuffer(buffer);
        }
    }

    public async Task<Version> GetLatestSupportedGatewayFormatAsync(CancellationToken cancellationToken = default)
    {
        var metadata = await GetArchiveMetadataAsync(cancellationToken);

        if (metadata?.SupportedGatewayFormats == null || !metadata.SupportedGatewayFormats.Any())
        {
            throw new InvalidOperationException("No supported gateway formats found in archive metadata.");
        }

        return metadata.SupportedGatewayFormats.Max() ??
            throw new InvalidOperationException("Invalid metadata format.");
    }

    public async Task<IEnumerable<Version>> GetSupportedGatewayFormatsAsync(
        CancellationToken cancellationToken = default)
    {
        var metadata = await GetArchiveMetadataAsync(cancellationToken);

        if (metadata?.SupportedGatewayFormats == null || !metadata.SupportedGatewayFormats.Any())
        {
            throw new InvalidOperationException("No supported gateway formats found in archive metadata.");
        }

        return metadata.SupportedGatewayFormats.OrderByDescending(v => v);
    }

    public async Task<IEnumerable<string>> GetSourceSchemaNamesAsync(
        CancellationToken cancellationToken = default)
    {
        var metadata = await GetArchiveMetadataAsync(cancellationToken);

        if (metadata?.SourceSchemas == null || !metadata.SourceSchemas.Any())
        {
            throw new InvalidOperationException("No source schemas found in archive metadata.");
        }

        return metadata.SourceSchemas.Order();
    }

    public async Task SetCompositionSettingsAsync(
        JsonDocument settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Exception? exception = null;
        var entry = CreateEntry(FileNames.CompositionSettings);

#if NET10_0_OR_GREATER
        await using var stream = await entry.OpenAsync(cancellationToken);
#else
        using var stream = entry.Open();
#endif
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

    public async Task<JsonDocument?> GetCompositionSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        var entry = _archive.GetEntry(FileNames.CompositionSettings);

        if (entry == null)
        {
            return null;
        }

#if NET10_0_OR_GREATER
        await using var stream = await entry.OpenAsync(cancellationToken);
#else
        using var stream = entry.Open();
#endif

        return await JsonDocument.ParseAsync(stream, default, cancellationToken);
    }

    public async Task SetGatewaySchemaAsync(
        string schema,
        Version version,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(schema);
        ArgumentNullException.ThrowIfNull(version);

        await SetGatewaySchemaAsync(Encoding.UTF8.GetBytes(schema), version, cancellationToken);
    }

    public async Task SetGatewaySchemaAsync(
        ReadOnlyMemory<byte> schema,
        Version version,
        CancellationToken cancellationToken = default)
    {
        var metaData = await GetArchiveMetadataAsync(cancellationToken);

        if (metaData is null)
        {
            throw new InvalidOperationException(
                "You need to first define the archive metadata.");
        }

        if (!metaData.SupportedGatewayFormats.Contains(version))
        {
            throw new InvalidOperationException(
                "You need to first declare the gateway schema version in the archive metadata.");
        }

        var entryPath = FileNames.GetGatewaySchemaPath(version);
        var entry = CreateEntry(entryPath);

#if NET10_0_OR_GREATER
        await using var stream = await entry.OpenAsync(cancellationToken);
#else
        using var stream = entry.Open();
#endif

        await stream.WriteAsync(schema, cancellationToken);
    }

    public async Task<ResolvedGatewaySchemaResult> TryGetGatewaySchemaAsync(
        Version maxVersion,
        IBufferWriter<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(maxVersion);
        ArgumentNullException.ThrowIfNull(buffer);

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

        var entryPath = FileNames.GetGatewaySchemaPath(version);
        var entry = _archive.GetEntry(entryPath);
        if (entry == null)
        {
            return new ResolvedGatewaySchemaResult { IsResolved = false, ActualVersion = null };
        }

#if NET10_0_OR_GREATER
        await using var stream = await entry.OpenAsync(cancellationToken);
#else
        using var stream = entry.Open();
#endif

        await stream.CopyToAsync(buffer, (int)entry.Length, cancellationToken);
        return new ResolvedGatewaySchemaResult { IsResolved = true, ActualVersion = version };
    }

    public async Task SetGatewaySettingsAsync(
        JsonDocument settings,
        Version version,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(version);

        var metaData = await GetArchiveMetadataAsync(cancellationToken);

        if (metaData is null)
        {
            throw new InvalidOperationException(
                "You need to first define the archive metadata.");
        }

        if (!metaData.SupportedGatewayFormats.Contains(version))
        {
            throw new InvalidOperationException(
                "You need to first declare the gateway schema version in the archive metadata.");
        }

        Exception? exception = null;
        var entryPath = FileNames.GetGatewaySettingsPath(version);
        var entry = CreateEntry(entryPath);

#if NET10_0_OR_GREATER
        await using var stream = await entry.OpenAsync(cancellationToken);
#else
        using var stream = entry.Open();
#endif

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

    public async Task<ResolvedGatewaySettingsResult> TryGetGatewaySettingsAsync(
        Version maxVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(maxVersion);

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

        var entryPath = FileNames.GetGatewaySettingsPath(version);
        var entry = _archive.GetEntry(entryPath);
        if (entry == null)
        {
            return new ResolvedGatewaySettingsResult { IsResolved = false, ActualVersion = null, Settings = null };
        }

#if NET10_0_OR_GREATER
        await using var stream = await entry.OpenAsync(cancellationToken);
#else
        using var stream = entry.Open();
#endif

        var settings = await JsonDocument.ParseAsync(stream, default, cancellationToken);
        return new ResolvedGatewaySettingsResult { IsResolved = true, ActualVersion = version, Settings = settings };
    }

    // Source schema operations
    public async Task AddSourceSchemaAsync(
        string schemaName,
        ReadOnlyMemory<byte> schema,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(schemaName);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(schema.Length, 0);

        if (!SchemaNameValidator.IsValidSchemaName(schemaName))
        {
            throw new ArgumentException("Invalid schema name.", nameof(schemaName));
        }

        var metaData = await GetArchiveMetadataAsync(cancellationToken);

        if (metaData is null)
        {
            throw new InvalidOperationException(
                "You need to first define the archive metadata.");
        }

        if (!metaData.SourceSchemas.Contains(schemaName))
        {
            throw new InvalidOperationException(
                "You need to first declare the source schema in the archive metadata.");
        }

        var entry = CreateEntry(FileNames.GetSourceSchemaPath(schemaName));

#if NET10_0_OR_GREATER
        await using var stream = await entry.OpenAsync(cancellationToken);
#else
        using var stream = entry.Open();
#endif

        await stream.WriteAsync(schema, cancellationToken);
    }

    public async Task<bool> TryGetSourceSchemaAsync(
        string schemaName,
        IBufferWriter<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schemaName);
        ArgumentNullException.ThrowIfNull(buffer);

        var entryPath = FileNames.GetSourceSchemaPath(schemaName);
        var entry = _archive.GetEntry(entryPath);
        if (entry == null)
        {
            return false;
        }

#if NET10_0_OR_GREATER
        await using var stream = await entry.OpenAsync(cancellationToken);
#else
        using var stream = entry.Open();
#endif
        await stream.CopyToAsync(buffer, (int)entry.Length, cancellationToken);
        return true;
    }

    // Signature operations
    public async Task SignArchiveAsync(
        X509Certificate2 privateKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(privateKey);

        if (!privateKey.HasPrivateKey)
        {
            throw new ArgumentException(
                "Certificate must contain a private key for signing.",
                nameof(privateKey));
        }

        // 1. Generate manifest of all non-signature files
        var manifest = await GenerateManifestAsync(cancellationToken);

        var buffer = TryRentBuffer(1024);

        try
        {
            // 2. Create detached signature
            SignatureManifestSerializer.Format(manifest, buffer, writeManifestHash: true);
            var contentInfo = new ContentInfo(buffer.WrittenSpan.ToArray());
            var signedCms = new SignedCms(contentInfo, detached: true);
            var signer = new CmsSigner(privateKey);
            signedCms.ComputeSignature(signer);
            var signatureBytes = signedCms.Encode();

            // 3. Store manifest and signature
            var manifestEntry = CreateEntry(FileNames.SignatureManifest);

#if NET10_0_OR_GREATER
            await using (var stream = await manifestEntry.OpenAsync(cancellationToken))
#else
            using (var stream = manifestEntry.Open())
#endif
            {
                await stream.WriteAsync(buffer.WrittenMemory, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            var signatureEntry = CreateEntry(FileNames.Signature);

#if NET10_0_OR_GREATER
            await using (var stream = await signatureEntry.OpenAsync(cancellationToken))
#else
            using (var stream = signatureEntry.Open())
#endif
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

    public async Task<SignatureVerificationResult> VerifySignatureAsync(
        X509Certificate2 publicKey,
        CancellationToken cancellationToken = default)
    {
        var manifestEntry = _archive.GetEntry(FileNames.SignatureManifest);
        var signatureEntry = _archive.GetEntry(FileNames.Signature);

        if (manifestEntry == null || signatureEntry == null)
        {
            return SignatureVerificationResult.NotSigned;
        }

        var buffer = TryRentBuffer(4096);

        try
        {
            // 1. Load manifest and signature
#if NET10_0_OR_GREATER
            await using var manifestStream = await manifestEntry.OpenAsync(cancellationToken);
            await using var signatureStream = await signatureEntry.OpenAsync(cancellationToken);
#else
            using var manifestStream = manifestEntry.Open();
            using var signatureStream = signatureEntry.Open();
#endif
            await manifestStream.CopyToAsync(buffer, 4096, cancellationToken);
            var manifest = SignatureManifestSerializer.Parse(buffer.WrittenMemory);

            buffer.Clear();

            await signatureStream.CopyToAsync(buffer, 4096, cancellationToken);
            var signatureBytes = buffer.WrittenSpan.ToArray();

            // 2. Verify file integrity
            foreach (var file in manifest.Files)
            {
                var entry = _archive.GetEntry(file.Key);
                if (entry == null)
                {
                    return SignatureVerificationResult.FilesMissing;
                }

                var actualHash = await ComputeFileHashAsync(entry, cancellationToken);
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
            var signedCms = new SignedCms();
            signedCms.Decode(signatureBytes);
            signedCms.CheckSignature(new X509Certificate2Collection(publicKey), true);

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

    public async Task<SignatureInfo?> GetSignatureInfoAsync(
        CancellationToken cancellationToken = default)
    {
        var manifestEntry = _archive.GetEntry(FileNames.SignatureManifest);
        var signatureEntry = _archive.GetEntry(FileNames.Signature);

        if (manifestEntry == null || signatureEntry == null)
        {
            return null;
        }

        var buffer = TryRentBuffer(1024);

        try
        {
#if NET10_0_OR_GREATER
            await using var manifestStream = await manifestEntry.OpenAsync(cancellationToken);
            await using var signatureStream = await signatureEntry.OpenAsync(cancellationToken);
#else
            using var manifestStream = manifestEntry.Open();
            using var signatureStream = signatureEntry.Open();
#endif

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
    }

    public bool IsSigned => _archive.GetEntry(FileNames.SignatureManifest) != null;

    // Private helper methods
    private ArrayBufferWriter<byte> TryRentBuffer(int expectedSize)
    {
        return Interlocked.Exchange(ref _buffer, null) ?? new ArrayBufferWriter<byte>(expectedSize);
    }

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

    private ZipArchiveEntry CreateEntry(string path)
    {
        // if there is already an entry with the same path we delete
        var existingEntry = _archive.GetEntry(path);
        existingEntry?.Delete();

        // then we create a new entry
        return _archive.CreateEntry(path);
    }

    private async Task<SignatureManifest> GenerateManifestAsync(CancellationToken cancellationToken)
    {
        var files = ImmutableDictionary.CreateBuilder<string, string>();

        foreach (var entry in _archive.Entries)
        {
            if (entry.FullName.StartsWith(".signature/"))
            {
                // Skip signature files
                continue;
            }

            files[entry.FullName] = await ComputeFileHashAsync(entry, cancellationToken);
        }

        var manifest = new SignatureManifest
        {
            Version = "1.0.0", Algorithm = "SHA256", Timestamp = DateTime.UtcNow, Files = files.ToImmutable()
        };

        var buffer = TryRentBuffer(1024);
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

    private static async Task<string> ComputeFileHashAsync(ZipArchiveEntry entry, CancellationToken cancellationToken)
    {
#if NET10_0_OR_GREATER
        await using var stream = await entry.OpenAsync(cancellationToken);
#else
        using var stream = entry.Open();
#endif
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

    public void Dispose()
    {
        _archive.Dispose();

        if (!_leaveOpen)
        {
            _stream.Dispose();
        }
    }
}

public enum FusionArchiveMode
{
    Read = ZipArchiveMode.Read,
    Create = ZipArchiveMode.Create,
    Update = ZipArchiveMode.Update
}

public enum SignatureVerificationResult
{
    Valid,
    NotSigned,
    FilesMissing,
    FilesModified,
    ManifestCorrupted,
    InvalidSignature,
    VerificationFailed
}

public record ArchiveMetadata
{
    public Version FormatVersion { get; init; } = new("1.0.0");
    public required ImmutableArray<Version> SupportedGatewayFormats { get; init; }
    public required ImmutableArray<string> SourceSchemas { get; init; }
}

public record SignatureManifest
{
    public string Version { get; init; } = "1.0.0";
    public string Algorithm { get; init; } = "SHA256";
    public required DateTime Timestamp { get; init; }
    public required ImmutableDictionary<string, string> Files { get; init; }
    public string? ManifestHash { get; init; }
}

public record SignatureInfo
{
    public required DateTime Timestamp { get; init; }
    public required string Algorithm { get; init; }
    public X509Certificate2? SignerCertificate { get; init; }
    public bool IsValid { get; init; }
}

internal static class FileNames
{
    private const string GatewaySchemaFormat = "gateway/{0}/gateway.graphqls";
    private const string GatewaySettingsFormat = "gateway/{0}/gateway-settings.json";
    private const string SourceSchemaFormat = "source-schemas/{0}/schema.graphqls";

    public const string ArchiveMetadata = "archive-metadata.json";
    public const string CompositionSettings = "composition-settings.json";
    public const string SignatureManifest = ".signature/manifest.json";
    public const string Signature = ".signature/signature.p7s";

    public static string GetGatewaySchemaPath(Version version)
        => string.Format(GatewaySchemaFormat, version);

    public static string GetGatewaySettingsPath(Version version)
        => string.Format(GatewaySettingsFormat, version);

    public static string GetSourceSchemaPath(string schemaName)
        => string.Format(SourceSchemaFormat, schemaName);
}

file static class Extensions
{
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

/// <summary>
/// Validates schema names according to the Fusion Execution Schema Format specification.
/// </summary>
internal static partial class SchemaNameValidator
{
    // Schema Name Grammar:
    // Name ::= NameStart NameContinue* [lookahead != NameContinue]
    // NameStart ::= Letter | `_`
    // NameContinue ::= Letter | Digit | `_` | `-`

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_-]*$")]
    private static partial Regex SchemaNameRegex();

    /// <summary>
    /// Validates whether the given string is a valid schema name.
    /// </summary>
    /// <param name="name">The schema name to validate.</param>
    /// <returns>True if the name is valid, false otherwise.</returns>
    public static bool IsValidSchemaName(string? name)
    {
        return !string.IsNullOrEmpty(name) && SchemaNameRegex().IsMatch(name) && name.Any(char.IsLetterOrDigit);
    }
}

public readonly struct ResolvedGatewaySchemaResult
{
    public required Version? ActualVersion { get; init; }

    [MemberNotNullWhen(true, nameof(ActualVersion))]
    public required bool IsResolved { get; init; }

    public static implicit operator Version?(ResolvedGatewaySchemaResult result)
        => result.ActualVersion;

    public static implicit operator bool(ResolvedGatewaySchemaResult result)
        => result.IsResolved;
}

public readonly struct ResolvedGatewaySettingsResult
{
    public required Version? ActualVersion { get; init; }

    [MemberNotNullWhen(true, nameof(Settings), nameof(ActualVersion))]
    public required bool IsResolved { get; init; }

    public required JsonDocument? Settings { get; init; }

    public static implicit operator Version?(ResolvedGatewaySettingsResult result)
        => result.ActualVersion;

    public static implicit operator bool(ResolvedGatewaySettingsResult result)
        => result.IsResolved;

    public static implicit operator JsonDocument?(ResolvedGatewaySettingsResult result)
        => result.Settings;
}
