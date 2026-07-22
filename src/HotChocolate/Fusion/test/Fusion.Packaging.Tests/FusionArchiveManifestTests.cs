using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace HotChocolate.Fusion.Packaging;

public class FusionArchiveManifestTests : IDisposable
{
    private static readonly Version s_gatewayVersion = new("2.0.0");
    private static readonly Version s_policyVersion = new("1.0.0");
    private readonly List<Stream> _streamsToDispose = [];

    [Fact]
    public async Task GetManifest_Should_ReturnManifest_When_ChangesAreCommitted()
    {
        // arrange
        var stream = CreateStream();

        // act
        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(CreateMetadata(), TestContext.Current.CancellationToken);
            await archive.SetGatewayConfigurationAsync(
                "type Query { hello: String }",
                CreateSettings(),
                s_gatewayVersion,
                TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        // assert
        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        var manifest = await readArchive.GetManifestAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(manifest);
        Assert.Equal("1.0.0", manifest.Version);
        Assert.Equal("sha256", manifest.Algorithm);
        Assert.Contains("gateway/2.0.0/gateway.graphqls", manifest.Files.Keys);
    }

    [Fact]
    public async Task GetManifest_Should_ReturnNull_When_ArchiveHasNoManifest()
    {
        // arrange
        var stream = CreateStream();
        using var archive = FusionArchive.Create(stream);

        // act
        var manifest = await archive.GetManifestAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Null(manifest);
    }

    [Fact]
    public async Task GenerateManifest_Should_BeByteIdentical_When_TwoArchivesHaveIdenticalContent()
    {
        // arrange
        var first = CreateStream();
        var second = CreateStream();

        // act
        await BuildArchiveAsync(first);
        await BuildArchiveAsync(second);

        // assert
        var firstManifest = await ReadEntryAsync(first, "manifest.json");
        var secondManifest = await ReadEntryAsync(second, "manifest.json");
        Assert.Equal(firstManifest, secondManifest);
    }

    [Fact]
    public async Task GenerateManifest_Should_ExcludeManifestAndSignature_When_ArchiveIsSigned()
    {
        // arrange
        var stream = CreateStream();
        using var certificate = CreateTestCertificate();

        // act
        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(CreateMetadata(), TestContext.Current.CancellationToken);
            await archive.SetGatewayConfigurationAsync(
                "type Query { hello: String }",
                CreateSettings(),
                s_gatewayVersion,
                TestContext.Current.CancellationToken);
            await archive.SignArchiveAsync(certificate, TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        // assert
        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        var manifest = await readArchive.GetManifestAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(manifest);
        Assert.Equal(
            [
                "archive-metadata.json",
                "gateway/2.0.0/gateway-settings.json",
                "gateway/2.0.0/gateway.graphqls"
            ],
            manifest.Files.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task GenerateManifest_Should_ContainArtifactDigests_When_AllUnitTypesPresent()
    {
        // arrange
        var stream = CreateStream();

        // act
        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(CreateMetadata(), TestContext.Current.CancellationToken);
            await archive.SetGatewayConfigurationAsync(
                "type Query { hello: String }",
                CreateSettings(),
                s_gatewayVersion,
                TestContext.Current.CancellationToken);
            await archive.SetSourceSchemaConfigurationAsync(
                "user-service",
                "type User { id: ID! }"u8.ToArray(),
                CreateSettings(),
                cancellationToken: TestContext.Current.CancellationToken);
            await archive.SetRegoPolicyAsync(
                "CanReadProduct",
                "package authz"u8.ToArray(),
                "fragment Requirements on Product { id }"u8.ToArray(),
                s_policyVersion,
                TestContext.Current.CancellationToken);
            await archive.SetRegoDataAsync(
                "roles",
                """{ "admin": ["product:read"] }"""u8.ToArray(),
                s_policyVersion,
                TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        // assert
        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        var manifest = await readArchive.GetManifestAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(manifest);
        Assert.Equal(
            [
                "gateway/2.0.0",
                "policies/rego/1.0.0/CanReadProduct",
                "policies/rego/1.0.0/data",
                "source-schemas/user-service"
            ],
            manifest.Artifacts.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task GenerateManifest_Should_ChangeOnlyAffectedDigests_When_OneSourceFileChanges()
    {
        // arrange
        var stream = CreateStream();
        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = [s_gatewayVersion],
            SourceSchemas = ["schema-a", "schema-b"]
        };

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata, TestContext.Current.CancellationToken);
            await archive.SetGatewayConfigurationAsync(
                "type Query { hello: String }",
                CreateSettings(),
                s_gatewayVersion,
                TestContext.Current.CancellationToken);
            await archive.SetSourceSchemaConfigurationAsync(
                "schema-a",
                "type A { id: ID! }"u8.ToArray(),
                CreateSettings(),
                cancellationToken: TestContext.Current.CancellationToken);
            await archive.SetSourceSchemaConfigurationAsync(
                "schema-b",
                "type B { id: ID! }"u8.ToArray(),
                CreateSettings(),
                cancellationToken: TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        stream.Position = 0;
        ArchiveManifest before;
        using (var readArchive = FusionArchive.Open(stream, leaveOpen: true))
        {
            before = (await readArchive.GetManifestAsync(TestContext.Current.CancellationToken))!;
        }

        // act
        stream.Position = 0;
        using (var updateArchive = FusionArchive.Open(stream, FusionArchiveMode.Update, leaveOpen: true))
        {
            await updateArchive.SetSourceSchemaConfigurationAsync(
                "schema-a",
                "type A { id: ID! name: String! }"u8.ToArray(),
                CreateSettings(),
                cancellationToken: TestContext.Current.CancellationToken);
            await updateArchive.CommitAsync(TestContext.Current.CancellationToken);
        }

        stream.Position = 0;
        ArchiveManifest after;
        using (var readArchive = FusionArchive.Open(stream, leaveOpen: true))
        {
            after = (await readArchive.GetManifestAsync(TestContext.Current.CancellationToken))!;
        }

        // assert
        Assert.NotEqual(
            before.Files["source-schemas/schema-a/schema.graphqls"],
            after.Files["source-schemas/schema-a/schema.graphqls"]);
        Assert.NotEqual(before.Artifacts["source-schemas/schema-a"], after.Artifacts["source-schemas/schema-a"]);
        Assert.Equal(
            before.Files["source-schemas/schema-b/schema.graphqls"],
            after.Files["source-schemas/schema-b/schema.graphqls"]);
        Assert.Equal(before.Artifacts["source-schemas/schema-b"], after.Artifacts["source-schemas/schema-b"]);
        Assert.Equal(before.Artifacts["gateway/2.0.0"], after.Artifacts["gateway/2.0.0"]);
    }

    [Fact]
    public async Task ArtifactDigest_Should_MatchLineBasedAlgorithm_When_ComputedForTwoFileUnit()
    {
        // arrange
        var stream = CreateStream();

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(CreateMetadata(), TestContext.Current.CancellationToken);
            await archive.SetGatewayConfigurationAsync(
                "type Query { hello: String }",
                CreateSettings(),
                s_gatewayVersion,
                TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        var manifest = (await readArchive.GetManifestAsync(TestContext.Current.CancellationToken))!;

        // act
        var expected = ComputeExpectedArtifactDigest(
            [
                ("gateway/2.0.0/gateway.graphqls", manifest.Files["gateway/2.0.0/gateway.graphqls"]),
                ("gateway/2.0.0/gateway-settings.json", manifest.Files["gateway/2.0.0/gateway-settings.json"])
            ]);

        // assert
        Assert.Equal(expected, manifest.Artifacts["gateway/2.0.0"]);
    }

    [Fact]
    public async Task ArtifactDigest_Should_UseUtf8ByteOrder_When_MemberPathsHaveSupplementaryCharacters()
    {
        // arrange
        // The private-use character U+E000 sorts after U+1F600 in UTF-16 code-unit order but before it in
        // UTF-8 byte order, so sorting the member lines by path (UTF-16) diverges from the spec's byte order.
        var supplementary = char.ConvertFromUtf32(0x1F600);
        var stream = CreateStream();

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetRegoDataAsync(
                "",
                "{ }"u8.ToArray(),
                s_policyVersion,
                TestContext.Current.CancellationToken);
            await archive.SetRegoDataAsync(
                "",
                "{ }"u8.ToArray(),
                s_policyVersion,
                TestContext.Current.CancellationToken);
            await archive.SetRegoDataAsync(
                supplementary,
                "{ }"u8.ToArray(),
                s_policyVersion,
                TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        var manifest = (await readArchive.GetManifestAsync(TestContext.Current.CancellationToken))!;

        // act
        var members = manifest.Files
            .Where(file => file.Key.StartsWith("policies/rego/1.0.0/data/", StringComparison.Ordinal))
            .Select(file => (file.Key, file.Value));
        var expected = ComputeExpectedArtifactDigestByteOrdered(members);

        // assert
        Assert.Equal(expected, manifest.Artifacts["policies/rego/1.0.0/data"]);
    }

    private static string ComputeExpectedArtifactDigest(IEnumerable<(string Path, string Digest)> members)
    {
        var lines = members
            .Select(member => $"{member.Path}:{member.Digest}")
            .OrderBy(line => line, StringComparer.Ordinal);

        var builder = new StringBuilder();
        foreach (var line in lines)
        {
            builder.Append(line).Append('\n');
        }

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return "sha256:" + Convert.ToHexString(digest).ToLowerInvariant();
    }

    private static string ComputeExpectedArtifactDigestByteOrdered(IEnumerable<(string Path, string Digest)> members)
    {
        var lines = members
            .Select(member => Encoding.UTF8.GetBytes($"{member.Path}:{member.Digest}"))
            .ToList();
        lines.Sort(static (left, right) => left.AsSpan().SequenceCompareTo(right.AsSpan()));

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var line in lines)
        {
            hash.AppendData(line);
            hash.AppendData("\n"u8);
        }

        return "sha256:" + Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    private static async Task BuildArchiveAsync(Stream stream)
    {
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await archive.SetArchiveMetadataAsync(CreateMetadata(), TestContext.Current.CancellationToken);
        await archive.SetGatewayConfigurationAsync(
            "type Query { hello: String }",
            CreateSettings(),
            s_gatewayVersion,
            TestContext.Current.CancellationToken);
        await archive.SetSourceSchemaConfigurationAsync(
            "user-service",
            "type User { id: ID! }"u8.ToArray(),
            CreateSettings(),
            cancellationToken: TestContext.Current.CancellationToken);
        await archive.CommitAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<byte[]> ReadEntryAsync(Stream stream, string entryName)
    {
        stream.Position = 0;
#if NET10_0_OR_GREATER
        await using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
#else
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
#endif
        var entry = zip.GetEntry(entryName)
            ?? throw new InvalidOperationException($"The entry '{entryName}' is missing.");
        await using var entryStream = entry.Open();
        await using var memory = new MemoryStream();
        await entryStream.CopyToAsync(memory, TestContext.Current.CancellationToken);
        return memory.ToArray();
    }

    private static ArchiveMetadata CreateMetadata()
        => new()
        {
            SupportedGatewayFormats = [s_gatewayVersion],
            SourceSchemas = ["user-service"]
        };

    private static JsonDocument CreateSettings() => JsonDocument.Parse("""{ "enableNodeSpec": true }""");

    private Stream CreateStream()
    {
        var stream = new MemoryStream();
        _streamsToDispose.Add(stream);
        return stream;
    }

    private static X509Certificate2 CreateTestCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
    }

    public void Dispose()
    {
        foreach (var stream in _streamsToDispose)
        {
            stream.Dispose();
        }
    }
}
