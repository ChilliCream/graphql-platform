using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace HotChocolate.Fusion.Packaging;

public sealed class FusionArchive : IDisposable
{
    private readonly ZipArchive _archive;
    private readonly Stream _stream;
    private readonly bool _leaveOpen;

    private FusionArchive(Stream stream, FusionArchiveMode mode, bool leaveOpen = false)
    {
        _stream = stream;
        _leaveOpen = leaveOpen;
        _archive = new ZipArchive(stream, (ZipArchiveMode)mode, leaveOpen);
    }

    public static FusionArchive Create(
        Stream stream,
        bool leaveOpen = false)
        => new(stream, FusionArchiveMode.Create, leaveOpen);

    public static FusionArchive Open(
        Stream stream,
        FusionArchiveMode mode = FusionArchiveMode.Read,
        bool leaveOpen = false)
        => new(stream, mode, leaveOpen);

    public static FusionArchive Open(
        string filePath,
        FusionArchiveMode mode = FusionArchiveMode.Read)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        var fileMode = mode == FusionArchiveMode.Read ? FileMode.Open : FileMode.Create;
        var fileStream = new FileStream(filePath, fileMode);
        return new FusionArchive(fileStream, mode);
    }

    public async Task SetArchiveMetadataAsync(ArchiveMetadata metadata)
    {
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        var entry = _archive.CreateEntry("archive-metadata.json");
        using var stream = entry.Open();
        await stream.WriteAsync(Encoding.UTF8.GetBytes(json));
    }

    public async Task<ArchiveMetadata?> GetArchiveMetadataAsync()
    {
        var entry = _archive.GetEntry("archive-metadata.json");
        if (entry == null) return null;

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<ArchiveMetadata>(json);
    }

    public async Task<string> GetLatestSupportedGatewayFormatAsync()
    {
        var metadata = await GetArchiveMetadataAsync();
        if (metadata?.SupportedGatewayFormats == null || !metadata.SupportedGatewayFormats.Any())
            throw new InvalidOperationException("No supported gateway formats found in archive metadata.");

        return metadata.SupportedGatewayFormats
            .Select(v => new Version(v))
            .Max()!
            .ToString();
    }

    // Composition settings operations
    public async Task SetCompositionSettingsAsync(string settingsJson)
    {
        var entry = _archive.CreateEntry("composition-settings.json");
        using var stream = entry.Open();
        await stream.WriteAsync(Encoding.UTF8.GetBytes(settingsJson));
    }

    public async Task SetCompositionSettingsAsync<T>(T settings) where T : class
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await SetCompositionSettingsAsync(json);
    }

    public async Task<string?> GetCompositionSettingsAsync()
    {
        var entry = _archive.GetEntry("composition-settings.json");
        if (entry == null) return null;

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task<T?> GetCompositionSettingsAsync<T>() where T : class
    {
        var json = await GetCompositionSettingsAsync();
        return json != null ? JsonSerializer.Deserialize<T>(json) : null;
    }

    // Gateway schema operations
    public async Task SetGatewaySchemaAsync(string schema, string? version = null)
    {
        version ??= await GetLatestSupportedGatewayFormatAsync();
        var entryPath = $"gateway/{version}/gateway.graphqls";
        var entry = _archive.CreateEntry(entryPath);
        using var stream = entry.Open();
        await stream.WriteAsync(Encoding.UTF8.GetBytes(schema));
    }

    public async Task<string?> GetGatewaySchemaAsync(string? version = null)
    {
        version ??= await GetLatestSupportedGatewayFormatAsync();
        var entryPath = $"gateway/{version}/gateway.graphqls";
        var entry = _archive.GetEntry(entryPath);
        if (entry == null) return null;

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    // Gateway settings operations
    public async Task SetGatewaySettingsAsync(string settingsJson, string? version = null)
    {
        version ??= await GetLatestSupportedGatewayFormatAsync();
        var entryPath = $"gateway/{version}/gateway-settings.json";
        var entry = _archive.CreateEntry(entryPath);
        using var stream = entry.Open();
        await stream.WriteAsync(Encoding.UTF8.GetBytes(settingsJson));
    }

    public async Task SetGatewaySettingsAsync<T>(T settings, string? version = null) where T : class
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await SetGatewaySettingsAsync(json, version);
    }

    public async Task<string?> GetGatewaySettingsAsync(string? version = null)
    {
        version ??= await GetLatestSupportedGatewayFormatAsync();
        var entryPath = $"gateway/{version}/gateway-settings.json";
        var entry = _archive.GetEntry(entryPath);
        if (entry == null) return null;

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task<T?> GetGatewaySettingsAsync<T>(string? version = null) where T : class
    {
        var json = await GetGatewaySettingsAsync(version);
        return json != null ? JsonSerializer.Deserialize<T>(json) : null;
    }

    public async Task<IEnumerable<string>> GetAvailableGatewayFormatsAsync()
    {
        return _archive.Entries
            .Where(e => e.FullName.StartsWith("gateway/") && e.Name == "gateway.graphqls")
            .Select(e => e.FullName.Split('/')[1])
            .Distinct()
            .OrderBy(v => new Version(v));
    }

    // Source schema operations
    public async Task AddSourceSchemaAsync(string schemaName, string schema)
    {
        var entryPath = $"source-schemas/{schemaName}/schema.graphqls";
        var entry = _archive.CreateEntry(entryPath);
        using var stream = entry.Open();
        await stream.WriteAsync(Encoding.UTF8.GetBytes(schema));
    }

    public async Task<string?> GetSourceSchemaAsync(string schemaName)
    {
        var entryPath = $"source-schemas/{schemaName}/schema.graphqls";
        var entry = _archive.GetEntry(entryPath);
        if (entry == null) return null;

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public IEnumerable<string> GetSourceSchemaNames()
    {
        return _archive.Entries
            .Where(e => e.FullName.StartsWith("source-schemas/") && e.Name == "schema.graphqls")
            .Select(e => e.FullName.Split('/')[1])
            .Distinct();
    }

    // Signature operations
    public async Task SignArchiveAsync(X509Certificate2 certificateWithPrivateKey)
    {
        if (!certificateWithPrivateKey.HasPrivateKey)
            throw new ArgumentException("Certificate must contain a private key for signing.", nameof(certificateWithPrivateKey));

        // 1. Generate manifest of all non-signature files
        var manifest = await GenerateManifestAsync();
        var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        var manifestBytes = Encoding.UTF8.GetBytes(manifestJson);

        // 2. Create detached signature
        var contentInfo = new ContentInfo(manifestBytes);
        var signedCms = new SignedCms(contentInfo, true); // true = detached signature
        var signer = new CmsSigner(certificateWithPrivateKey);
        signedCms.ComputeSignature(signer);
        var signatureBytes = signedCms.Encode();

        // 3. Store manifest and signature
        var manifestEntry = _archive.CreateEntry(".signature/manifest.json");
        using (var stream = manifestEntry.Open())
        {
            await stream.WriteAsync(manifestBytes);
        }

        var signatureEntry = _archive.CreateEntry(".signature/signature.p7s");
        using (var stream = signatureEntry.Open())
        {
            await stream.WriteAsync(signatureBytes);
        }
    }

    public async Task<SignatureVerificationResult> VerifySignatureAsync(X509Certificate2? trustedPublicCertificate = null)
    {
        var manifestEntry = _archive.GetEntry(".signature/manifest.json");
        var signatureEntry = _archive.GetEntry(".signature/signature.p7s");

        if (manifestEntry == null || signatureEntry == null)
            return SignatureVerificationResult.NotSigned;

        try
        {
            // 1. Load manifest and signature
            var manifestJson = await ReadEntryAsStringAsync(manifestEntry);
            var manifest = JsonSerializer.Deserialize<SignatureManifest>(manifestJson);
            var signatureBytes = await ReadEntryAsBytesAsync(signatureEntry);

            if (manifest == null)
                return SignatureVerificationResult.ManifestCorrupted;

            // 2. Verify file integrity
            foreach (var file in manifest.Files)
            {
                var entry = _archive.GetEntry(file.Key);
                if (entry == null)
                    return SignatureVerificationResult.FilesMissing;

                var actualHash = await ComputeFileHashAsync(entry);
                if (!actualHash.Equals(file.Value, StringComparison.OrdinalIgnoreCase))
                    return SignatureVerificationResult.FilesModified;
            }

            // 3. Verify manifest hash
            var manifestBytes = Encoding.UTF8.GetBytes(manifestJson);
            var computedManifestHash = ComputeSHA256Hash(manifestBytes);
            if (!computedManifestHash.Equals(manifest.ManifestHash, StringComparison.OrdinalIgnoreCase))
                return SignatureVerificationResult.ManifestCorrupted;

            // 4. Verify cryptographic signature
            var signedCms = new SignedCms();
            signedCms.Decode(signatureBytes);

            signedCms.CheckSignature(trustedPublicCertificate != null
                ? new X509Certificate2Collection(trustedPublicCertificate)
                : null, true);

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
    }

    public async Task<SignatureInfo?> GetSignatureInfoAsync()
    {
        var manifestEntry = _archive.GetEntry(".signature/manifest.json");
        var signatureEntry = _archive.GetEntry(".signature/signature.p7s");

        if (manifestEntry == null || signatureEntry == null)
            return null;

        try
        {
            var manifestJson = await ReadEntryAsStringAsync(manifestEntry);
            var manifest = JsonSerializer.Deserialize<SignatureManifest>(manifestJson);
            var signatureBytes = await ReadEntryAsBytesAsync(signatureEntry);

            var signedCms = new SignedCms();
            signedCms.Decode(signatureBytes);

            var signerInfo = signedCms.SignerInfos[0];
            var certificate = signerInfo.Certificate;

            return new SignatureInfo
            {
                Timestamp = manifest?.Timestamp ?? DateTime.MinValue,
                Algorithm = manifest?.Algorithm ?? "Unknown",
                SignerCertificate = certificate,
                IsValid = await VerifySignatureAsync() == SignatureVerificationResult.Valid
            };
        }
        catch
        {
            return null;
        }
    }

    public bool IsSigned => _archive.GetEntry(".signature/manifest.json") != null;

    // Private helper methods
    private async Task<SignatureManifest> GenerateManifestAsync()
    {
        var files = new Dictionary<string, string>();

        foreach (var entry in _archive.Entries)
        {
            if (entry.FullName.StartsWith(".signature/"))
                continue; // Skip signature files

            var hash = await ComputeFileHashAsync(entry);
            files[entry.FullName] = hash;
        }

        var manifest = new SignatureManifest
        {
            Version = "1.0.0",
            Algorithm = "SHA256",
            Timestamp = DateTime.UtcNow,
            Files = files
        };

        // Compute manifest hash
        var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        var manifestBytes = Encoding.UTF8.GetBytes(manifestJson);
        manifest.ManifestHash = ComputeSHA256Hash(manifestBytes);

        return manifest;
    }

    private async Task<string> ComputeFileHashAsync(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        return "sha256:" + Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string ComputeSHA256Hash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        return "sha256:" + Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private async Task<string> ReadEntryAsStringAsync(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private async Task<byte[]> ReadEntryAsBytesAsync(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    public void Dispose()
    {
        _archive?.Dispose();
        if (!_leaveOpen)
            _stream?.Dispose();
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

public class ArchiveMetadata
{
    public string FormatVersion { get; set; } = "1.0.0";
    public List<string> SupportedGatewayFormats { get; set; } = [];
    public List<string> SourceSchemas { get; set; } = [];
}

public class SignatureManifest
{
    public string Version { get; set; } = "1.0.0";
    public string Algorithm { get; set; } = "SHA256";
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Files { get; set; } = [];
    public string ManifestHash { get; set; } = "";
}

public class SignatureInfo
{
    public DateTime Timestamp { get; set; }
    public string Algorithm { get; set; } = "";
    public X509Certificate2? SignerCertificate { get; set; }
    public bool IsValid { get; set; }
}
