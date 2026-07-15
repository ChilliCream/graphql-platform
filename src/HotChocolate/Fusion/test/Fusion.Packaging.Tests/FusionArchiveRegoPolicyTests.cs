using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace HotChocolate.Fusion.Packaging;

public class FusionArchiveRegoPolicyTests
{
    private static readonly Version s_version1 = new("1.0.0");
    private static readonly Version s_version2 = new("2.0.0");

    [Fact]
    public async Task SetAndGetRegoPolicies_Should_RoundTrip_When_ArchiveContainsMultipleFormats()
    {
        const string policy = "package authz\nallow := true";
        const string requirements = "fragment Requirements on Product { id }";
        await using var stream = new MemoryStream();

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetRegoPolicyAsync(
                "CanReadProduct",
                Encoding.UTF8.GetBytes(policy),
                Encoding.UTF8.GetBytes(requirements),
                s_version1,
                TestContext.Current.CancellationToken);
            await archive.SetRegoPolicyAsync(
                "CanReadPrice",
                "package price"u8.ToArray(),
                "fragment Price on Product { price }"u8.ToArray(),
                s_version1,
                TestContext.Current.CancellationToken);
            await archive.SetRegoPolicyAsync(
                "CanReadProduct",
                "package authz_v2"u8.ToArray(),
                "fragment RequirementsV2 on Product { id }"u8.ToArray(),
                s_version2,
                TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        stream.Position = 0;
#if NET10_0_OR_GREATER
        await using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true))
#else
        using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true))
#endif
        {
            Assert.Equal(
                [
                    "policies/rego/1.0.0/CanReadPrice.graphql",
                    "policies/rego/1.0.0/CanReadPrice.rego",
                    "policies/rego/1.0.0/CanReadProduct.graphql",
                    "policies/rego/1.0.0/CanReadProduct.rego",
                    "policies/rego/2.0.0/CanReadProduct.graphql",
                    "policies/rego/2.0.0/CanReadProduct.rego"
                ],
                zipArchive.Entries.Select(t => t.FullName).Order(StringComparer.Ordinal));
        }

        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        Assert.Equal([s_version2, s_version1], readArchive.GetSupportedRegoPolicyFormats());
        Assert.Equal(
            ["CanReadPrice", "CanReadProduct"],
            readArchive.GetRegoPolicyNames(s_version1));

        var policies = await readArchive.GetRegoPoliciesAsync(
            s_version1,
            TestContext.Current.CancellationToken);
        Assert.Equal(["CanReadPrice", "CanReadProduct"], policies.Select(t => t.Name));
        Assert.All(policies, t => Assert.Equal(s_version1, t.FormatVersion));

        var retrieved = await readArchive.TryGetRegoPolicyAsync(
            "CanReadProduct",
            s_version1,
            TestContext.Current.CancellationToken);
        Assert.NotNull(retrieved);
        Assert.Equal(policy, await ReadToEndAsync(await retrieved.OpenReadPolicyAsync(
            TestContext.Current.CancellationToken)));
        Assert.Equal(requirements, await ReadToEndAsync(await retrieved.OpenReadRequirementsAsync(
            TestContext.Current.CancellationToken)));
    }

    [Fact]
    public async Task TryGetRegoPolicy_Should_ReturnNull_When_ArchiveHasNoPolicies()
    {
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream);

        var policy = await archive.TryGetRegoPolicyAsync(
            "CanReadProduct",
            s_version1,
            TestContext.Current.CancellationToken);

        Assert.Null(policy);
        Assert.Empty(archive.GetSupportedRegoPolicyFormats());
        Assert.Empty(archive.GetRegoPolicyNames(s_version1));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("directory/policy")]
    [InlineData("directory\\policy")]
    public async Task SetRegoPolicy_Should_Throw_When_NameIsNotSafePathSegment(string policyName)
    {
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream);

        await Assert.ThrowsAsync<ArgumentException>(
            () => archive.SetRegoPolicyAsync(
                policyName,
                "package authz"u8.ToArray(),
                "fragment Requirements on Product { id }"u8.ToArray(),
                s_version1,
                TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1.0.0.0")]
    public async Task SetRegoPolicy_Should_Throw_When_VersionIsNotThreeParts(string version)
    {
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream);

        await Assert.ThrowsAsync<ArgumentException>(
            () => archive.SetRegoPolicyAsync(
                "CanReadProduct",
                "package authz"u8.ToArray(),
                "fragment Requirements on Product { id }"u8.ToArray(),
                new Version(version),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetRegoPolicy_Should_Throw_When_EitherPayloadIsEmpty()
    {
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => archive.SetRegoPolicyAsync(
                "CanReadProduct",
                ReadOnlyMemory<byte>.Empty,
                "fragment Requirements on Product { id }"u8.ToArray(),
                s_version1,
                TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => archive.SetRegoPolicyAsync(
                "CanReadProduct",
                "package authz"u8.ToArray(),
                ReadOnlyMemory<byte>.Empty,
                s_version1,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetRegoPolicy_Should_Throw_When_NameDiffersOnlyByCase()
    {
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream);
        await archive.SetRegoPolicyAsync(
            "CanReadProduct",
            "package authz"u8.ToArray(),
            "fragment Requirements on Product { id }"u8.ToArray(),
            s_version1,
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.SetRegoPolicyAsync(
                "canreadproduct",
                "package other"u8.ToArray(),
                "fragment Other on Product { id }"u8.ToArray(),
                s_version1,
                TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("policies/rego/1.0.0/CanReadProduct.rego")]
    [InlineData("policies/rego/1.0.0/CanReadProduct.graphql")]
    public async Task GetRegoPolicyNames_Should_Throw_When_PairIsIncomplete(string path)
    {
        await using var stream = CreateArchive((path, "content"));
        using var archive = FusionArchive.Open(stream, leaveOpen: true);

        Assert.Throws<InvalidDataException>(() => archive.GetRegoPolicyNames(s_version1));
    }

    [Theory]
    [InlineData("policies/rego/1.0/CanReadProduct.rego")]
    [InlineData("policies/rego/01.0.0/CanReadProduct.rego")]
    [InlineData("policies/rego/1.0.0/nested/CanReadProduct.rego")]
    [InlineData("policies/rego/1.0.0/nested\\CanReadProduct.rego")]
    [InlineData("policies/rego/1.0.0/CanReadProduct.REGO")]
    public async Task GetSupportedRegoPolicyFormats_Should_Throw_When_PathIsInvalid(string path)
    {
        await using var stream = CreateArchive((path, "package authz"));
        using var archive = FusionArchive.Open(stream, leaveOpen: true);

        Assert.Throws<InvalidDataException>(() => archive.GetSupportedRegoPolicyFormats());
    }

    [Fact]
    public async Task GetRegoPolicyNames_Should_Throw_When_NamesDifferOnlyByCase()
    {
        await using var stream = CreateArchive(
            ("policies/rego/1.0.0/CanReadProduct.rego", "package first"),
            ("policies/rego/1.0.0/CanReadProduct.graphql", "fragment First on Product { id }"),
            ("policies/rego/1.0.0/canreadproduct.rego", "package second"),
            ("policies/rego/1.0.0/canreadproduct.graphql", "fragment Second on Product { id }"));
        using var archive = FusionArchive.Open(stream, leaveOpen: true);

        Assert.Throws<InvalidDataException>(() => archive.GetRegoPolicyNames(s_version1));
    }

    [Fact]
    public async Task GetRegoPolicyNames_Should_Throw_When_ArchiveContainsDuplicatePath()
    {
        await using var stream = CreateArchive(
            ("policies/rego/1.0.0/CanReadProduct.rego", "package first"),
            ("policies/rego/1.0.0/CanReadProduct.rego", "package second"),
            ("policies/rego/1.0.0/CanReadProduct.graphql", "fragment Requirements on Product { id }"));
        using var archive = FusionArchive.Open(stream, leaveOpen: true);

        Assert.Throws<InvalidDataException>(() => archive.GetRegoPolicyNames(s_version1));
    }

    [Fact]
    public async Task GetRegoPolicies_Should_Throw_When_PolicyExceedsConfiguredSize()
    {
        await using var stream = CreateArchive(
            ("policies/rego/1.0.0/CanReadProduct.rego", "12345"),
            ("policies/rego/1.0.0/CanReadProduct.graphql", "fragment Requirements on Product { id }"));
        using var archive = FusionArchive.Open(
            stream,
            leaveOpen: true,
            options: new FusionArchiveOptions { MaxAllowedPolicySize = 4 });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => archive.GetRegoPoliciesAsync(s_version1, TestContext.Current.CancellationToken));
        Assert.Equal("File is too large and exceeds the allowed size of 4.", exception.Message);
    }

    [Fact]
    public async Task VerifySignature_Should_ReturnFilesModified_When_PolicyIsAddedAfterSigning()
    {
        await using var stream = new MemoryStream();
        using var certificate = CreateTestCertificate();
#if NET9_0_OR_GREATER
        using var publicCertificate = X509CertificateLoader.LoadCertificate(
            certificate.Export(X509ContentType.Cert));
#else
        using var publicCertificate = new X509Certificate2(certificate.Export(X509ContentType.Cert));
#endif

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetRegoPolicyAsync(
                "CanReadProduct",
                "package authz"u8.ToArray(),
                "fragment Requirements on Product { id }"u8.ToArray(),
                s_version1,
                TestContext.Current.CancellationToken);
            await archive.SignArchiveAsync(certificate, TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        stream.Position = 0;
        using (var archive = FusionArchive.Open(stream, leaveOpen: true))
        {
            var validResult = await archive.VerifySignatureAsync(
                publicCertificate,
                TestContext.Current.CancellationToken);
            Assert.Equal(SignatureVerificationResult.Valid, validResult);
        }

        stream.Position = 0;
        using (var archive = FusionArchive.Open(
            stream,
            FusionArchiveMode.Update,
            leaveOpen: true))
        {
            await archive.SetRegoPolicyAsync(
                "CanReadPrice",
                "package price"u8.ToArray(),
                "fragment Requirements on Product { price }"u8.ToArray(),
                s_version1,
                TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        var result = await readArchive.VerifySignatureAsync(
            publicCertificate,
            TestContext.Current.CancellationToken);
        Assert.Equal(SignatureVerificationResult.FilesModified, result);
    }

    [Fact]
    public async Task VerifySignature_Should_ReturnFilesModified_When_SignatureDirectoryFileIsAddedAfterSigning()
    {
        await using var stream = new MemoryStream();
        using var certificate = CreateTestCertificate();

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetRegoPolicyAsync(
                "CanReadProduct",
                "package authz"u8.ToArray(),
                "fragment Requirements on Product { id }"u8.ToArray(),
                s_version1,
                TestContext.Current.CancellationToken);
            await archive.SignArchiveAsync(certificate, TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        stream.Position = 0;
#if NET10_0_OR_GREATER
        await using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
#else
        using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
#endif
        {
            var entry = zipArchive.CreateEntry(".signature/extra.txt");
            await using var writer = new StreamWriter(entry.Open(), Encoding.UTF8, leaveOpen: false);
            await writer.WriteAsync("not signed");
        }

        stream.Position = 0;
        using var readArchive = FusionArchive.Open(stream, leaveOpen: true);
        var result = await readArchive.VerifySignatureAsync(
            certificate,
            TestContext.Current.CancellationToken);
        Assert.Equal(SignatureVerificationResult.FilesModified, result);
    }

    private static MemoryStream CreateArchive(params (string Path, string Content)[] files)
    {
        var stream = new MemoryStream();

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var entry = archive.CreateEntry(file.Path);
                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8, leaveOpen: false);
                writer.Write(file.Content);
            }
        }

        stream.Position = 0;
        return stream;
    }

    private static async Task<string> ReadToEndAsync(Stream stream)
    {
        await using (stream)
        {
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        }
    }

    private static X509Certificate2 CreateTestCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
    }
}
