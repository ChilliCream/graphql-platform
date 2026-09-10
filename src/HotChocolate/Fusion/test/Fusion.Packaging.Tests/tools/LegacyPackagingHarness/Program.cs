using System.Buffers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Packaging;

namespace LegacyPackagingHarness;

// This process is built against the released 16.6.4 HotChocolate.Fusion.Packaging
// package (the last pre-rename version, whose public C# surface still uses the
// Gateway* names: GatewayConfiguration, SetGatewayConfigurationAsync,
// GetSupportedGatewayFormatsAsync, ArchiveMetadata.SupportedGatewayFormats, and so
// on). ArchiveCompatibilityTests in the renamed 16.7 Fusion.Packaging.Tests project
// invokes the compiled output of this project as a separate OS process, so the
// pinned old reader/writer runs in true isolation from the renamed
// HotChocolate.Fusion.Packaging assembly under test: it is never recompiled or
// linked against the new API, only exercised through the released package.
//
// Usage:
//   LegacyPackagingHarness write <archivePath> <certPath>
//     Writes the canonical repo-7i1.2 fixture archive (see BuildFixture below)
//     using the pinned old writer, across a Create pass followed by an Update
//     pass, then signs it and exports the public signing certificate to
//     <certPath>. Used once to produce the committed pre-rename fixture; also
//     runnable directly to reproduce it.
//   LegacyPackagingHarness read <archivePath> <certPath>
//     Reads <archivePath> with the pinned old reader and prints a single-line
//     JSON summary of its complete contents (metadata, every declared gateway
//     format, every source schema, the legacy archive payload, and the
//     signature verification result against the certificate at <certPath>) to
//     stdout. Diagnostic output goes to stderr only, so stdout is safe to
//     parse as JSON.
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length < 3)
            {
                await Console.Error.WriteLineAsync(
                    "Usage: LegacyPackagingHarness <write|read> <archivePath> <certPath>");
                return 2;
            }

            var command = args[0];
            var archivePath = args[1];
            var certPath = args[2];

            switch (command)
            {
                case "write":
                    await WriteFixtureAsync(archivePath, certPath);
                    return 0;

                case "read":
                    await ReadArchiveAsync(archivePath, certPath);
                    return 0;

                default:
                    await Console.Error.WriteLineAsync($"Unknown command '{command}'.");
                    return 2;
            }
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.ToString());
            return 1;
        }
    }

    // Canonical repo-7i1.2 fixture scenario. Kept deliberately explicit (rather than
    // parameterized) because the committed pre-rename fixture archive under
    // __resources__ must stay byte-stable: ArchiveCompatibilityTests asserts against
    // these exact values, mirrored on the C# test side.
    private static async Task WriteFixtureAsync(string archivePath, string certPath)
    {
        using var cert = CreateSigningCertificate();

        using (var archive = FusionArchive.Create(archivePath))
        {
            await archive.SetArchiveMetadataAsync(
                new ArchiveMetadata
                {
                    FormatVersion = new Version("1.0.0"),
                    SupportedGatewayFormats = [new Version("1.0.0"), new Version("2.0.0")],
                    SourceSchemas = ["accounts", "catalog"]
                },
                CancellationToken.None);

            await archive.SetGatewayConfigurationAsync(
                FixtureData.SchemaV1,
                ParseJson(FixtureData.SettingsV1),
                new Version("1.0.0"),
                CancellationToken.None);

            await archive.SetGatewayConfigurationAsync(
                FixtureData.SchemaV2,
                ParseJson(FixtureData.SettingsV2),
                new Version("2.0.0"),
                CancellationToken.None);

            await archive.SetSourceSchemaConfigurationAsync(
                "accounts",
                Encoding.UTF8.GetBytes(FixtureData.AccountsSchema),
                ParseJson(FixtureData.AccountsSettings),
                Encoding.UTF8.GetBytes(FixtureData.AccountsExtensions),
                CancellationToken.None);

            await archive.SetSourceSchemaConfigurationAsync(
                "catalog",
                Encoding.UTF8.GetBytes(FixtureData.CatalogSchema),
                ParseJson(FixtureData.CatalogSettings),
                cancellationToken: CancellationToken.None);

            await using (var legacy = new MemoryStream(FixtureData.LegacyArchiveBytes))
            {
                await archive.SetLegacyArchiveFileAsync(legacy, CancellationToken.None);
            }

            await archive.CommitAsync(CancellationToken.None);
        }

        // A second, Update-mode pass: declares and writes a third gateway format
        // version against the already-committed archive, proving the pinned old
        // writer's Update mode round-trips through the same file the renamed
        // reader will later open.
        using (var archive = FusionArchive.Open(archivePath, FusionArchiveMode.Update))
        {
            var metadata = await archive.GetArchiveMetadataAsync(CancellationToken.None)
                ?? throw new InvalidOperationException("Fixture metadata missing after create pass.");

            await archive.SetArchiveMetadataAsync(
                metadata with
                {
                    SupportedGatewayFormats = metadata.SupportedGatewayFormats.Add(new Version("2.1.0"))
                },
                CancellationToken.None);

            await archive.SetGatewayConfigurationAsync(
                FixtureData.SchemaV21,
                ParseJson(FixtureData.SettingsV21),
                new Version("2.1.0"),
                CancellationToken.None);

            await archive.SignArchiveAsync(cert, CancellationToken.None);

            await archive.CommitAsync(CancellationToken.None);
        }

        await File.WriteAllBytesAsync(certPath, cert.Export(X509ContentType.Cert));
    }

    private static async Task ReadArchiveAsync(string archivePath, string certPath)
    {
        using var archive = FusionArchive.Open(archivePath, FusionArchiveMode.Read);

        var metadata = await archive.GetArchiveMetadataAsync(CancellationToken.None);

        var buffer = new ArrayBufferWriter<byte>();
        await using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();

        writer.WriteStartObject("metadata");
        if (metadata is null)
        {
            writer.WriteBoolean("present", false);
        }
        else
        {
            writer.WriteBoolean("present", true);
            writer.WriteString("formatVersion", metadata.FormatVersion.ToString());
            writer.WriteStartArray("supportedGatewayFormats");
            foreach (var version in metadata.SupportedGatewayFormats.OrderBy(v => v))
            {
                writer.WriteStringValue(version.ToString());
            }
            writer.WriteEndArray();
            writer.WriteStartArray("sourceSchemas");
            foreach (var schema in metadata.SourceSchemas.OrderBy(s => s, StringComparer.Ordinal))
            {
                writer.WriteStringValue(schema);
            }
            writer.WriteEndArray();
        }
        writer.WriteEndObject();

        if (metadata is not null)
        {
            writer.WriteString(
                "latestSupportedGatewayFormat",
                (await archive.GetLatestSupportedGatewayFormatAsync(CancellationToken.None)).ToString());

            writer.WriteStartArray("gatewayConfigurations");
            foreach (var probe in metadata.SupportedGatewayFormats.OrderBy(v => v))
            {
                await WriteGatewayConfigurationAsync(writer, archive, probe);
            }
            writer.WriteEndArray();

            writer.WriteStartArray("sourceSchemaConfigurations");
            foreach (var schemaName in metadata.SourceSchemas.OrderBy(s => s, StringComparer.Ordinal))
            {
                await WriteSourceSchemaConfigurationAsync(writer, archive, schemaName);
            }
            writer.WriteEndArray();
        }

        var legacy = await archive.TryGetLegacyArchiveFileAsync(CancellationToken.None);
        if (legacy is null)
        {
            writer.WriteNull("legacyArchiveSha256");
        }
        else
        {
            await using (legacy)
            {
                using var sha256 = SHA256.Create();
                var hash = await sha256.ComputeHashAsync(legacy, CancellationToken.None);
                writer.WriteString("legacyArchiveSha256", Convert.ToHexStringLower(hash));
            }
        }

        writer.WriteBoolean("isSigned", archive.IsSigned);

        using var cert = X509CertificateLoader.LoadCertificateFromFile(certPath);
        var verification = await archive.VerifySignatureAsync(cert, CancellationToken.None);
        writer.WriteString("signatureVerificationResult", verification.ToString());

        var signatureInfo = await archive.GetSignatureInfoAsync(CancellationToken.None);
        if (signatureInfo is null)
        {
            writer.WriteNull("signatureInfo");
        }
        else
        {
            writer.WriteStartObject("signatureInfo");
            writer.WriteString("algorithm", signatureInfo.Algorithm);
            writer.WriteBoolean("isValid", signatureInfo.IsValid);
            writer.WriteBoolean("hasSignerCertificate", signatureInfo.SignerCertificate is not null);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.Flush();

        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    private static async Task WriteGatewayConfigurationAsync(
        Utf8JsonWriter writer,
        FusionArchive archive,
        Version maxVersion)
    {
        writer.WriteStartObject();
        writer.WriteString("probeMaxVersion", maxVersion.ToString());

        var configuration = await archive.TryGetGatewayConfigurationAsync(maxVersion, CancellationToken.None);
        if (configuration is null)
        {
            writer.WriteBoolean("found", false);
        }
        else
        {
            using (configuration)
            {
                writer.WriteBoolean("found", true);
                writer.WriteString("resolvedVersion", configuration.Version.ToString());

                await using var schemaStream = await configuration.OpenReadSchemaAsync(CancellationToken.None);
                using var schemaReader = new StreamReader(schemaStream, Encoding.UTF8);
                writer.WriteString("schema", await schemaReader.ReadToEndAsync());

                var settingsBuffer = new ArrayBufferWriter<byte>();
                await using (var settingsWriter = new Utf8JsonWriter(settingsBuffer))
                {
                    configuration.Settings.WriteTo(settingsWriter);
                }
                writer.WriteString("settings", Encoding.UTF8.GetString(settingsBuffer.WrittenSpan));
            }
        }

        writer.WriteEndObject();
    }

    private static async Task WriteSourceSchemaConfigurationAsync(
        Utf8JsonWriter writer,
        FusionArchive archive,
        string schemaName)
    {
        writer.WriteStartObject();
        writer.WriteString("name", schemaName);

        var configuration = await archive.TryGetSourceSchemaConfigurationAsync(schemaName, CancellationToken.None);
        if (configuration is null)
        {
            writer.WriteBoolean("found", false);
        }
        else
        {
            using (configuration)
            {
                writer.WriteBoolean("found", true);

                await using var schemaStream = await configuration.OpenReadSchemaAsync(CancellationToken.None);
                using var schemaReader = new StreamReader(schemaStream, Encoding.UTF8);
                writer.WriteString("schema", await schemaReader.ReadToEndAsync());

                await using var extensionsStream =
                    await configuration.TryOpenReadSchemaExtensionsAsync(CancellationToken.None);
                if (extensionsStream is null)
                {
                    writer.WriteNull("extensions");
                }
                else
                {
                    using var extensionsReader = new StreamReader(extensionsStream, Encoding.UTF8);
                    writer.WriteString("extensions", await extensionsReader.ReadToEndAsync());
                }

                var settingsBuffer = new ArrayBufferWriter<byte>();
                await using (var settingsWriter = new Utf8JsonWriter(settingsBuffer))
                {
                    configuration.Settings.WriteTo(settingsWriter);
                }
                writer.WriteString("settings", Encoding.UTF8.GetString(settingsBuffer.WrittenSpan));
            }
        }

        writer.WriteEndObject();
    }

    private static JsonDocument ParseJson(string json) => JsonDocument.Parse(json);

    private static X509Certificate2 CreateSigningCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=repo-7i1.2 Legacy Packaging Harness",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(5));
    }
}

// The exact strings written into the fixture archive. Both the harness (writer)
// and ArchiveCompatibilityTests (reader assertions) reference these values so the
// committed __resources__ fixture and its expected content stay in one place.
internal static class FixtureData
{
    public const string SchemaV1 =
        "type Query {\n  hello: String\n}\n";

    public const string SettingsV1 =
        """{ "maxOperationComplexity": 100, "nodeResolution": "GATEWAY" }""";

    public const string SchemaV2 =
        "type Query {\n  hello: String @fusion__gateway_field(schema: SOURCE_SCHEMA)\n}\n";

    public const string SettingsV2 =
        """{ "maxOperationComplexity": 250, "nodeResolution": "GATEWAY" }""";

    public const string SchemaV21 =
        "type Query {\n"
        + "  hello: String @fusion__gateway_field(schema: SOURCE_SCHEMA)\n"
        + "  world: String @fusion__gateway_field(schema: SOURCE_SCHEMA)\n"
        + "}\n";

    public const string SettingsV21 =
        """{ "maxOperationComplexity": 500, "nodeResolution": "GATEWAY" }""";

    public const string AccountsSchema =
        "type Query {\n  account: String\n}\n";

    public const string AccountsExtensions =
        "extend type Query {\n  accountExtra: String\n}\n";

    public const string AccountsSettings =
        """{ "schemaName": "accounts" }""";

    public const string CatalogSchema =
        "type Query {\n  catalog: String\n}\n";

    public const string CatalogSettings =
        """{ "schemaName": "catalog" }""";

    public static readonly byte[] LegacyArchiveBytes =
        Encoding.UTF8.GetBytes("repo-7i1.2-legacy-v1-archive-fixture-payload");
}
