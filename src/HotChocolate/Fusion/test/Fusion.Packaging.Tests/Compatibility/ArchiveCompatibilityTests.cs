using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Proves the archive-format compatibility contract behind the gateway-to-router rename
/// (repo-7i1.2): the renamed FusionArchive reader can read archives produced by the
/// released pre-rename (16.6.4) writer, and the pinned pre-rename reader, run as a
/// separate process against the released package, can read archives produced by the
/// renamed writer. Neither direction relies on a same-process, same-writer/same-reader
/// round trip: reader and writer never come from the same package version in a single
/// assertion.
/// </summary>
public class ArchiveCompatibilityTests : IDisposable
{
    private readonly List<string> _tempFiles = [];
    private readonly List<X509Certificate2> _certificatesToDispose = [];

    [Fact]
    public async Task PreRenameFixture_Should_ReadCompleteContent_When_OpenedWithRenamedApi()
    {
        // arrange
        var archivePath = System.IO.Path.Combine(
            AppContext.BaseDirectory, "__resources__", "pre-rename-archive-16.6.4.far");
        var certPath = System.IO.Path.Combine(
            AppContext.BaseDirectory, "__resources__", "pre-rename-archive-16.6.4.cer");
        Assert.True(File.Exists(archivePath));
        Assert.True(File.Exists(certPath));

        // act
        using var archive = FusionArchive.Open(archivePath, FusionArchiveMode.Read);
        var metadata = await archive.GetArchiveMetadataAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(metadata);

        var latest = await archive.GetLatestSupportedRouterFormatAsync(TestContext.Current.CancellationToken);
        var supported = await archive.GetSupportedRouterFormatsAsync(TestContext.Current.CancellationToken);

        var gatewayProbes = new List<object>();
        foreach (var probe in new[] { new Version("1.0.0"), new Version("2.0.0"), new Version("2.0.5"), new Version("2.1.0") })
        {
            gatewayProbes.Add(await ReadRouterConfigurationAsync(archive, probe));
        }

        var sourceSchemas = new List<object>();
        foreach (var schemaName in metadata.SourceSchemas.OrderBy(s => s, StringComparer.Ordinal))
        {
            sourceSchemas.Add(await ReadSourceSchemaAsync(archive, schemaName));
        }

        var legacyHash = await ReadLegacyArchiveHashAsync(archive);

        var signatureVerificationResult = await VerifySignatureAsync(archive, certPath);
        var signatureInfo = await archive.GetSignatureInfoAsync(TestContext.Current.CancellationToken);

        // assert
        new
        {
            Metadata = new
            {
                metadata.FormatVersion,
                SupportedRouterFormats = metadata.SupportedRouterFormats.OrderBy(v => v).Select(v => v.ToString()),
                SourceSchemas = metadata.SourceSchemas.OrderBy(s => s, StringComparer.Ordinal)
            },
            Latest = latest.ToString(),
            Supported = supported.Select(v => v.ToString()),
            GatewayConfigurations = gatewayProbes,
            SourceSchemaConfigurations = sourceSchemas,
            LegacyArchiveSha256 = legacyHash,
            archive.IsSigned,
            SignatureVerificationResult = signatureVerificationResult.ToString(),
            SignatureInfo = signatureInfo is null
                ? null
                : new { signatureInfo.Algorithm, signatureInfo.IsValid, HasSignerCertificate = signatureInfo.SignerCertificate is not null }
        }.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task RenamedWriter_Should_ProduceArchive_That_PinnedOldReaderReadsCompletely()
    {
        // arrange
        var archivePath = CreateTempFilePath(".far");
        var certPath = CreateTempFilePath(".cer");
        using var cert = CreateSigningCertificate();

        // act: write with the renamed (16.7) in-repo FusionArchive across a Create pass
        // followed by an Update pass, mirroring the pre-rename fixture's shape.
        using (var archive = FusionArchive.Create(archivePath))
        {
            await archive.SetArchiveMetadataAsync(
                new ArchiveMetadata
                {
                    FormatVersion = new Version("1.0.0"),
                    SupportedRouterFormats = [new Version("1.0.0"), new Version("2.0.0")],
                    SourceSchemas = ["inventory", "billing"]
                },
                TestContext.Current.CancellationToken);

            await archive.SetRouterConfigurationAsync(
                RouterFixture.SchemaV1,
                JsonDocument.Parse(RouterFixture.SettingsV1),
                new Version("1.0.0"),
                TestContext.Current.CancellationToken);

            await archive.SetRouterConfigurationAsync(
                RouterFixture.SchemaV2,
                JsonDocument.Parse(RouterFixture.SettingsV2),
                new Version("2.0.0"),
                TestContext.Current.CancellationToken);

            await archive.SetSourceSchemaConfigurationAsync(
                "inventory",
                Encoding.UTF8.GetBytes(RouterFixture.InventorySchema),
                JsonDocument.Parse(RouterFixture.InventorySettings),
                Encoding.UTF8.GetBytes(RouterFixture.InventoryExtensions),
                TestContext.Current.CancellationToken);

            await archive.SetSourceSchemaConfigurationAsync(
                "billing",
                Encoding.UTF8.GetBytes(RouterFixture.BillingSchema),
                JsonDocument.Parse(RouterFixture.BillingSettings),
                cancellationToken: TestContext.Current.CancellationToken);

            await using (var legacy = new MemoryStream(RouterFixture.LegacyArchiveBytes))
            {
                await archive.SetLegacyArchiveFileAsync(legacy, TestContext.Current.CancellationToken);
            }

            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        using (var archive = FusionArchive.Open(archivePath, FusionArchiveMode.Update))
        {
            var metadata = await archive.GetArchiveMetadataAsync(TestContext.Current.CancellationToken)
                ?? throw new InvalidOperationException("Metadata missing after create pass.");

            await archive.SetArchiveMetadataAsync(
                metadata with { SupportedRouterFormats = metadata.SupportedRouterFormats.Add(new Version("2.1.0")) },
                TestContext.Current.CancellationToken);

            await archive.SetRouterConfigurationAsync(
                RouterFixture.SchemaV21,
                JsonDocument.Parse(RouterFixture.SettingsV21),
                new Version("2.1.0"),
                TestContext.Current.CancellationToken);

            await archive.SignArchiveAsync(cert, TestContext.Current.CancellationToken);

            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        await File.WriteAllBytesAsync(
            certPath,
            cert.Export(X509ContentType.Cert),
            TestContext.Current.CancellationToken);

        // act: read the same file with the pinned 16.6.4 reader, out of process.
        var stdOut = await LegacyPackagingHarnessRunner.RunAsync(
            "read",
            archivePath,
            certPath,
            TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(stdOut);
        var root = document.RootElement;

        // assert
        new
        {
            Metadata = new
            {
                FormatVersion = root.GetProperty("metadata").GetProperty("formatVersion").GetString(),
                SupportedGatewayFormats = root.GetProperty("metadata").GetProperty("supportedGatewayFormats")
                    .EnumerateArray().Select(e => e.GetString()),
                SourceSchemas = root.GetProperty("metadata").GetProperty("sourceSchemas")
                    .EnumerateArray().Select(e => e.GetString())
            },
            LatestSupportedGatewayFormat = root.GetProperty("latestSupportedGatewayFormat").GetString(),
            GatewayConfigurations = root.GetProperty("gatewayConfigurations").EnumerateArray()
                .Select(e => new
                {
                    ProbeMaxVersion = e.GetProperty("probeMaxVersion").GetString(),
                    ResolvedVersion = e.GetProperty("resolvedVersion").GetString(),
                    Schema = e.GetProperty("schema").GetString(),
                    Settings = e.GetProperty("settings").GetString()
                }),
            SourceSchemaConfigurations = root.GetProperty("sourceSchemaConfigurations").EnumerateArray()
                .Select(e => new
                {
                    Name = e.GetProperty("name").GetString(),
                    Schema = e.GetProperty("schema").GetString(),
                    Extensions = e.GetProperty("extensions").ValueKind == JsonValueKind.Null
                        ? null
                        : e.GetProperty("extensions").GetString(),
                    Settings = e.GetProperty("settings").GetString()
                }),
            LegacyArchiveSha256 = root.GetProperty("legacyArchiveSha256").GetString(),
            IsSigned = root.GetProperty("isSigned").GetBoolean(),
            SignatureVerificationResult = root.GetProperty("signatureVerificationResult").GetString()
        }.MatchMarkdownSnapshot();
    }

    private static async Task<object> ReadRouterConfigurationAsync(FusionArchive archive, Version probe)
    {
        var configuration = await archive.TryGetRouterConfigurationAsync(probe, TestContext.Current.CancellationToken);
        if (configuration is null)
        {
            return new { ProbeMaxVersion = probe.ToString(), Found = false };
        }

        using (configuration)
        {
            await using var schemaStream = await configuration.OpenReadSchemaAsync(TestContext.Current.CancellationToken);
            using var schemaReader = new StreamReader(schemaStream, Encoding.UTF8);
            var schema = await schemaReader.ReadToEndAsync(TestContext.Current.CancellationToken);

            return new
            {
                ProbeMaxVersion = probe.ToString(),
                Found = true,
                ResolvedVersion = configuration.Version.ToString(),
                Schema = schema,
                Settings = configuration.Settings.RootElement.GetRawText()
            };
        }
    }

    private static async Task<object> ReadSourceSchemaAsync(FusionArchive archive, string schemaName)
    {
        var configuration = await archive.TryGetSourceSchemaConfigurationAsync(
            schemaName, TestContext.Current.CancellationToken);
        Assert.NotNull(configuration);

        using (configuration)
        {
            await using var schemaStream = await configuration.OpenReadSchemaAsync(TestContext.Current.CancellationToken);
            using var schemaReader = new StreamReader(schemaStream, Encoding.UTF8);
            var schema = await schemaReader.ReadToEndAsync(TestContext.Current.CancellationToken);

            await using var extensionsStream = await configuration.TryOpenReadSchemaExtensionsAsync(
                TestContext.Current.CancellationToken);
            string? extensions = null;
            if (extensionsStream is not null)
            {
                using var extensionsReader = new StreamReader(extensionsStream, Encoding.UTF8);
                extensions = await extensionsReader.ReadToEndAsync(TestContext.Current.CancellationToken);
            }

            return new
            {
                Name = schemaName,
                Schema = schema,
                Extensions = extensions,
                Settings = configuration.Settings.RootElement.GetRawText()
            };
        }
    }

    private static async Task<string> ReadLegacyArchiveHashAsync(FusionArchive archive)
    {
        await using var legacy = await archive.TryGetLegacyArchiveFileAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(legacy);

        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(legacy, TestContext.Current.CancellationToken);
        return Convert.ToHexStringLower(hash);
    }

    private static async Task<SignatureVerificationResult> VerifySignatureAsync(FusionArchive archive, string certPath)
    {
#if NET9_0_OR_GREATER
        using var cert = X509CertificateLoader.LoadCertificateFromFile(certPath);
#else
        using var cert = new X509Certificate2(certPath);
#endif
        return await archive.VerifySignatureAsync(cert, TestContext.Current.CancellationToken);
    }

    private X509Certificate2 CreateSigningCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=repo-7i1.2 Renamed Writer Compatibility Test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(5));
        _certificatesToDispose.Add(cert);
        return cert;
    }

    private string CreateTempFilePath(string extension)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"repo-7i1-2-{Guid.NewGuid():N}{extension}");
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var path in _tempFiles)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
                // best-effort cleanup
            }
        }

        foreach (var cert in _certificatesToDispose)
        {
            cert.Dispose();
        }
    }
}

/// <summary>
/// Content used by RenamedWriter_Should_ProduceArchive_That_PinnedOldReaderReadsCompletely.
/// Deliberately distinct from tools/LegacyPackagingHarness's FixtureData so the two
/// compatibility directions (old writer read by the new reader, new writer read by the
/// old reader) are never exercising the exact same bytes.
/// </summary>
internal static class RouterFixture
{
    public const string SchemaV1 = "type Query {\n  ping: String\n}\n";
    public const string SettingsV1 = """{ "maxOperationComplexity": 120, "nodeResolution": "GATEWAY" }""";

    public const string SchemaV2 =
        "type Query {\n  ping: String @fusion__gateway_field(schema: SOURCE_SCHEMA)\n}\n";
    public const string SettingsV2 = """{ "maxOperationComplexity": 260, "nodeResolution": "GATEWAY" }""";

    public const string SchemaV21 =
        "type Query {\n"
        + "  ping: String @fusion__gateway_field(schema: SOURCE_SCHEMA)\n"
        + "  pong: String @fusion__gateway_field(schema: SOURCE_SCHEMA)\n"
        + "}\n";
    public const string SettingsV21 = """{ "maxOperationComplexity": 520, "nodeResolution": "GATEWAY" }""";

    public const string InventorySchema = "type Query {\n  inventory: String\n}\n";
    public const string InventoryExtensions = "extend type Query {\n  inventoryExtra: String\n}\n";
    public const string InventorySettings = """{ "schemaName": "inventory" }""";

    public const string BillingSchema = "type Query {\n  billing: String\n}\n";
    public const string BillingSettings = """{ "schemaName": "billing" }""";

    public static readonly byte[] LegacyArchiveBytes =
        Encoding.UTF8.GetBytes("repo-7i1.2-router-writer-legacy-fixture-payload");
}
