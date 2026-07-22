using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace HotChocolate.Fusion.Packaging;

public class FusionArchiveSignatureTests : IDisposable
{
    private static readonly Version s_gatewayVersion = new("2.0.0");
    private readonly List<Stream> _streamsToDispose = [];
    private readonly List<X509Certificate2> _certificatesToDispose = [];

    [Fact]
    public async Task VerifySignature_Should_ReturnValid_When_SignedArchiveIsCommittedAndReopened()
    {
        // arrange
        var stream = CreateStream();
        using var certificate = CreateTestCertificate();
        using var publicCertificate = ToPublicCertificate(certificate);

        // act
        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await BuildGatewayAsync(archive);
            await archive.SignArchiveAsync(certificate, TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        // assert
        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        var result = await readArchive.VerifySignatureAsync(publicCertificate, TestContext.Current.CancellationToken);
        Assert.Equal(SignatureVerificationResult.Valid, result);
    }

    [Fact]
    public async Task VerifySignature_Should_ReturnFilesModified_When_EntryIsTamperedThroughZip()
    {
        // arrange
        var stream = CreateStream();
        using var certificate = CreateTestCertificate();
        using var publicCertificate = ToPublicCertificate(certificate);

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await BuildGatewayAsync(archive);
            await archive.SignArchiveAsync(certificate, TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        // act
        // Rewrite a listed entry directly through the zip, bypassing the manifest regeneration.
        stream.Position = 0;
#if NET10_0_OR_GREATER
        await using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
#else
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
#endif
        {
            zip.GetEntry("gateway/2.0.0/gateway.graphqls")!.Delete();
            var entry = zip.CreateEntry("gateway/2.0.0/gateway.graphqls");
            await using var entryStream = entry.Open();
            await entryStream.WriteAsync(
                "type Query { tampered: String }"u8.ToArray(),
                TestContext.Current.CancellationToken);
        }

        // assert
        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        var result = await readArchive.VerifySignatureAsync(publicCertificate, TestContext.Current.CancellationToken);
        Assert.Equal(SignatureVerificationResult.FilesModified, result);
    }

    [Fact]
    public async Task VerifySignature_Should_ReturnUnlistedFile_When_RogueEntryIsAddedThroughZip()
    {
        // arrange
        var stream = CreateStream();
        using var certificate = CreateTestCertificate();
        using var publicCertificate = ToPublicCertificate(certificate);

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await BuildGatewayAsync(archive);
            await archive.SignArchiveAsync(certificate, TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        // act
        // Add an entry that is not listed in the manifest, bypassing the manifest regeneration.
        stream.Position = 0;
#if NET10_0_OR_GREATER
        await using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
#else
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
#endif
        {
            var entry = zip.CreateEntry("composition-settings.json");
            await using var entryStream = entry.Open();
            await entryStream.WriteAsync("{ }"u8.ToArray(), TestContext.Current.CancellationToken);
        }

        // assert
        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        var result = await readArchive.VerifySignatureAsync(publicCertificate, TestContext.Current.CancellationToken);
        Assert.Equal(SignatureVerificationResult.UnlistedFile, result);
    }

    [Fact]
    public async Task VerifySignature_Should_ReturnValid_When_SourceSchemasAreStrippedAfterSigning()
    {
        // arrange
        var stream = CreateStream();
        using var certificate = CreateTestCertificate();
        using var publicCertificate = ToPublicCertificate(certificate);

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await BuildGatewayAsync(archive);
            await archive.SetSourceSchemaConfigurationAsync(
                "user-service",
                "type User { id: ID! }"u8.ToArray(),
                CreateSettings(),
                cancellationToken: TestContext.Current.CancellationToken);
            await archive.SignArchiveAsync(certificate, TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        // act
        stream.Position = 0;
        using (var updateArchive = FusionArchive.Open(stream, FusionArchiveMode.Update, leaveOpen: true))
        {
            await updateArchive.StripAsync(
                FusionArchiveComponents.SourceSchemas,
                TestContext.Current.CancellationToken);
        }

        // assert
        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        var result = await readArchive.VerifySignatureAsync(publicCertificate, TestContext.Current.CancellationToken);
        var stripped = await readArchive.TryGetSourceSchemaConfigurationAsync(
            "user-service",
            TestContext.Current.CancellationToken);
        var manifest = (await readArchive.GetManifestAsync(TestContext.Current.CancellationToken))!;
        Assert.Equal(SignatureVerificationResult.Valid, result);
        Assert.Null(stripped);
        Assert.Contains("source-schemas/user-service/schema.graphqls", manifest.Files.Keys);
    }

    [Fact]
    public async Task VerifySignature_Should_ReturnUnlistedFile_When_DirectoryLikeEntryCarriesContent()
    {
        // arrange
        var stream = CreateStream();
        using var certificate = CreateTestCertificate();
        using var publicCertificate = ToPublicCertificate(certificate);

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await BuildGatewayAsync(archive);
            await archive.SignArchiveAsync(certificate, TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        // act
        // Smuggle content behind an entry name that ends with a slash, mimicking a directory placeholder.
        stream.Position = 0;
#if NET10_0_OR_GREATER
        await using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
#else
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
#endif
        {
            var entry = zip.CreateEntry("payload.bin/");
            await using var entryStream = entry.Open();
            await entryStream.WriteAsync(
                "malicious payload"u8.ToArray(),
                TestContext.Current.CancellationToken);
        }

        // assert
        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        var result = await readArchive.VerifySignatureAsync(publicCertificate, TestContext.Current.CancellationToken);
        Assert.Equal(SignatureVerificationResult.UnlistedFile, result);
    }

    [Fact]
    public async Task VerifySignature_Should_PropagateCancellation_When_TokenIsCanceled()
    {
        // arrange
        var stream = CreateStream();
        using var certificate = CreateTestCertificate();
        using var publicCertificate = ToPublicCertificate(certificate);

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await BuildGatewayAsync(archive);
            await archive.SignArchiveAsync(certificate, TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        // Prime the session cache so the cancellation is observed inside the verification body
        // rather than while first extracting the manifest.
        await readArchive.VerifySignatureAsync(publicCertificate, TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // act & assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => readArchive.VerifySignatureAsync(publicCertificate, cts.Token));
    }

    [Fact]
    public async Task VerifySignature_Should_ReturnNotSigned_When_UnsignedArchiveIsIntact()
    {
        // arrange
        var stream = CreateStream();
        using var certificate = CreateTestCertificate();
        using var publicCertificate = ToPublicCertificate(certificate);

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await BuildGatewayAsync(archive);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        // act
        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        var result = await readArchive.VerifySignatureAsync(publicCertificate, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(SignatureVerificationResult.NotSigned, result);
    }

    [Fact]
    public async Task VerifySignature_Should_ReturnFilesModified_When_UnsignedArchiveFileTampered()
    {
        // arrange
        var stream = CreateStream();
        using var certificate = CreateTestCertificate();
        using var publicCertificate = ToPublicCertificate(certificate);

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await BuildGatewayAsync(archive);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        // act
        // An unsigned archive is still integrity-checked against its manifest.
        stream.Position = 0;
#if NET10_0_OR_GREATER
        await using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
#else
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
#endif
        {
            zip.GetEntry("gateway/2.0.0/gateway.graphqls")!.Delete();
            var entry = zip.CreateEntry("gateway/2.0.0/gateway.graphqls");
            await using var entryStream = entry.Open();
            await entryStream.WriteAsync(
                "type Query { tampered: String }"u8.ToArray(),
                TestContext.Current.CancellationToken);
        }

        // assert
        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        var result = await readArchive.VerifySignatureAsync(publicCertificate, TestContext.Current.CancellationToken);
        Assert.Equal(SignatureVerificationResult.FilesModified, result);
    }

    [Fact]
    public async Task VerifySignature_Should_ReturnUnlistedFile_When_UnsignedArchiveHasRogueEntry()
    {
        // arrange
        var stream = CreateStream();
        using var certificate = CreateTestCertificate();
        using var publicCertificate = ToPublicCertificate(certificate);

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await BuildGatewayAsync(archive);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        // act
        stream.Position = 0;
#if NET10_0_OR_GREATER
        await using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
#else
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
#endif
        {
            var entry = zip.CreateEntry("composition-settings.json");
            await using var entryStream = entry.Open();
            await entryStream.WriteAsync("{ }"u8.ToArray(), TestContext.Current.CancellationToken);
        }

        // assert
        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        var result = await readArchive.VerifySignatureAsync(publicCertificate, TestContext.Current.CancellationToken);
        Assert.Equal(SignatureVerificationResult.UnlistedFile, result);
    }

    [Fact]
    public async Task VerifySignature_Should_ReturnFilesModified_When_ManifestDigestIsUppercase()
    {
        // arrange
        var stream = CreateStream();
        using var certificate = CreateTestCertificate();
        using var publicCertificate = ToPublicCertificate(certificate);

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await BuildGatewayAsync(archive);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        string digest;
        stream.Position = 0;
        using (var readArchive = FusionArchive.Open(stream, leaveOpen: true))
        {
            var manifest = (await readArchive.GetManifestAsync(TestContext.Current.CancellationToken))!;
            digest = manifest.Files["gateway/2.0.0/gateway.graphqls"];
        }

        // act
        // The spec renders digests as lowercase hexadecimal, so an uppercase digest must not match.
        var uppercase = "sha256:" + digest["sha256:".Length..].ToUpperInvariant();
        await RewriteEntryAsync(stream, "manifest.json", text => text.Replace(digest, uppercase));

        // assert
        stream.Position = 0;
        using var readArchive2 = FusionArchive.Open(stream, leaveOpen: true);
        var result = await readArchive2.VerifySignatureAsync(publicCertificate, TestContext.Current.CancellationToken);
        Assert.Equal(SignatureVerificationResult.FilesModified, result);
    }

    [Fact]
    public async Task VerifySignature_Should_ReturnUnsupportedAlgorithm_When_ManifestAlgorithmIsNotSha256()
    {
        // arrange
        var stream = CreateStream();
        using var certificate = CreateTestCertificate();
        using var publicCertificate = ToPublicCertificate(certificate);

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await BuildGatewayAsync(archive);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        // act
        await RewriteEntryAsync(
            stream,
            "manifest.json",
            text => text.Replace("\"algorithm\":\"sha256\"", "\"algorithm\":\"sha512\""));

        // assert
        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        var result = await readArchive.VerifySignatureAsync(publicCertificate, TestContext.Current.CancellationToken);
        Assert.Equal(SignatureVerificationResult.UnsupportedAlgorithm, result);
    }

    [Fact]
    public async Task Strip_Should_Throw_When_PendingUncommittedChangesExist()
    {
        // arrange
        var stream = CreateStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await BuildGatewayAsync(archive);

        // act & assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.StripAsync(FusionArchiveComponents.SourceSchemas, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetSignatureInfo_Should_ReturnSigningTime_When_ArchiveIsSigned()
    {
        // arrange
        var stream = CreateStream();
        using var certificate = CreateTestCertificate();
        var before = DateTimeOffset.UtcNow.AddMinutes(-1);

        // act
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        await BuildGatewayAsync(archive);
        await archive.SignArchiveAsync(certificate, TestContext.Current.CancellationToken);
        var info = await archive.GetSignatureInfoAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(info);
        Assert.True(info.IsValid);
        Assert.NotNull(info.Timestamp);
        Assert.True(info.Timestamp >= before);
    }

    private static async Task BuildGatewayAsync(FusionArchive archive)
    {
        await archive.SetArchiveMetadataAsync(
            new ArchiveMetadata
            {
                SupportedGatewayFormats = [s_gatewayVersion],
                SourceSchemas = ["user-service"]
            },
            TestContext.Current.CancellationToken);
        await archive.SetGatewayConfigurationAsync(
            "type Query { hello: String }",
            CreateSettings(),
            s_gatewayVersion,
            TestContext.Current.CancellationToken);
    }

    private static async Task RewriteEntryAsync(Stream stream, string entryName, Func<string, string> transform)
    {
        stream.Position = 0;
        string original;
#if NET10_0_OR_GREATER
        await using var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true);
#else
        using var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true);
#endif
        var entry = zip.GetEntry(entryName)
            ?? throw new InvalidOperationException($"The entry '{entryName}' is missing.");

        await using (var readStream = entry.Open())
        using (var reader = new StreamReader(readStream))
        {
            original = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        }

        entry.Delete();

        var replacement = zip.CreateEntry(entryName);
        await using var writeStream = replacement.Open();
        var bytes = System.Text.Encoding.UTF8.GetBytes(transform(original));
        await writeStream.WriteAsync(bytes, TestContext.Current.CancellationToken);
    }

    private static JsonDocument CreateSettings() => JsonDocument.Parse("{ }");

    private static X509Certificate2 ToPublicCertificate(X509Certificate2 certificate)
#if NET9_0_OR_GREATER
        => X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));
#else
        => new(certificate.Export(X509ContentType.Cert));
#endif

    private Stream CreateStream()
    {
        var stream = new MemoryStream();
        _streamsToDispose.Add(stream);
        return stream;
    }

    private X509Certificate2 CreateTestCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
        _certificatesToDispose.Add(certificate);
        return certificate;
    }

    public void Dispose()
    {
        foreach (var stream in _streamsToDispose)
        {
            stream.Dispose();
        }

        foreach (var certificate in _certificatesToDispose)
        {
            certificate.Dispose();
        }
    }
}
