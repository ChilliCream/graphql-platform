using System.IO.Compression;
using System.Reactive;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Channels;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Language;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Configuration;

public sealed class FileSystemFusionConfigurationProviderTests : IDisposable
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(15);

    private readonly string _directory =
        Directory.CreateTempSubdirectory("fusion-config-provider-tests").FullName;

    [Fact]
    public async Task Provider_Should_ExposeConfiguration_When_SchemaFileIsValid()
    {
        // arrange
        var fileName = IOPath.Combine(_directory, "schema.graphql");
        await File.WriteAllTextAsync(
            fileName, "type Query { hello: String }", TestContext.Current.CancellationToken);

        await using var provider = new FileSystemFusionConfigurationProvider(fileName, diagnosticEvents: null);

        // act
        var configuration = await WaitForConfigurationAsync(provider);

        // assert
        var queryType = Assert.IsType<ObjectTypeDefinitionNode>(Assert.Single(configuration.Schema.Definitions));
        Assert.Equal("Query", queryType.Name.Value);
    }

    [Fact]
    public async Task Provider_Should_RaiseConfigurationReadError_When_SchemaFileIsMalformed()
    {
        // arrange
        var fileName = IOPath.Combine(_directory, "schema.graphql");
        await File.WriteAllTextAsync(fileName, "type Query { hello: ", TestContext.Current.CancellationToken);
        var diagnosticEvents = new RecordingDiagnosticEvents();

        // act
        await using var provider = new FileSystemFusionConfigurationProvider(fileName, diagnosticEvents);
        var error = await ReadWithTimeoutAsync(diagnosticEvents.ConfigurationReadErrors.Reader);

        // assert
        Assert.NotNull(error);
        Assert.Null(provider.Configuration);
    }

    [Fact]
    public async Task Provider_Should_ExposeConfiguration_When_PackageArchiveIsValid()
    {
        // arrange
        var fileName = IOPath.Combine(_directory, "gateway.far");
        await CreateValidArchiveAsync(fileName);

        await using var provider = new FileSystemFusionConfigurationProvider(fileName, diagnosticEvents: null);

        // act
        var configuration = await WaitForConfigurationAsync(provider);

        // assert
        var queryType = Assert.IsType<ObjectTypeDefinitionNode>(Assert.Single(configuration.Schema.Definitions));
        Assert.Equal("Query", queryType.Name.Value);
    }

    [Fact]
    public async Task Provider_Should_RetryReadingUnchangedBytes_When_PackageArchiveIsMalformed()
    {
        // arrange
        // The package hash must only be committed once the archive has been read
        // successfully. Otherwise, a second signal for the unchanged malformed bytes
        // would be treated as already handled and silently skipped forever.
        var fileName = IOPath.Combine(_directory, "gateway.far");
        var malformedBytes = "this is not a fusion archive"u8.ToArray();
        await File.WriteAllBytesAsync(fileName, malformedBytes, TestContext.Current.CancellationToken);
        var diagnosticEvents = new RecordingDiagnosticEvents();

        // act
        await using var provider = new FileSystemFusionConfigurationProvider(fileName, diagnosticEvents);
        var firstError = await ReadWithTimeoutAsync(diagnosticEvents.ConfigurationReadErrors.Reader);

        provider.SignalChange();
        var secondError = await ReadWithTimeoutAsync(diagnosticEvents.ConfigurationReadErrors.Reader);

        // assert
        Assert.Equal(firstError.GetType(), secondError.GetType());
        Assert.Null(provider.Configuration);
    }

    [Fact]
    public async Task Provider_Should_RetryReadingUnchangedBytes_When_PackageSchemaIsMalformed()
    {
        // arrange
        // The package hash must only be committed once the schema, settings, and policy
        // content have all been read and parsed successfully. Otherwise, a second signal for
        // unchanged malformed bytes would be treated as already handled and silently skipped.
        var fileName = IOPath.Combine(_directory, "gateway.far");
        await CreateArchiveWithMalformedSchemaAsync(fileName);
        var diagnosticEvents = new RecordingDiagnosticEvents();

        // act
        await using var provider = new FileSystemFusionConfigurationProvider(fileName, diagnosticEvents);
        var firstError = await ReadWithTimeoutAsync(diagnosticEvents.ConfigurationReadErrors.Reader);

        provider.SignalChange();
        var secondError = await ReadWithTimeoutAsync(diagnosticEvents.ConfigurationReadErrors.Reader);

        // assert
        Assert.Equal(firstError.GetType(), secondError.GetType());
        Assert.Null(provider.Configuration);
    }

    [Fact]
    public async Task Provider_Should_RejectPackage_When_ArchiveIsTampered()
    {
        // arrange
        var fileName = IOPath.Combine(_directory, "gateway.far");
        await CreateValidArchiveAsync(fileName);
        await TamperGatewaySchemaEntryAsync(fileName);
        var diagnosticEvents = new RecordingDiagnosticEvents();

        // act
        await using var provider = new FileSystemFusionConfigurationProvider(fileName, diagnosticEvents);
        var result = await ReadWithTimeoutAsync(diagnosticEvents.ConfigurationVerificationFailures.Reader);

        // assert
        Assert.Equal(SignatureVerificationResult.FilesModified, result);
        Assert.Null(provider.Configuration);
    }

    [Fact]
    public async Task Provider_Should_RejectPackage_When_ArchiveIsUnsignedAndTrustRootIsConfigured()
    {
        // arrange
        var fileName = IOPath.Combine(_directory, "gateway.far");
        await CreateValidArchiveAsync(fileName);
        using var trustedCertificate = CreateTestCertificate();
        var diagnosticEvents = new RecordingDiagnosticEvents();
        var options = new FileSystemConfigurationOptions
        {
            TrustedSigningCertificates = [ToPublicCertificate(trustedCertificate)]
        };

        // act
        await using var provider = new FileSystemFusionConfigurationProvider(fileName, diagnosticEvents, options);
        var result = await ReadWithTimeoutAsync(diagnosticEvents.ConfigurationVerificationFailures.Reader);

        // assert
        Assert.Equal(SignatureVerificationResult.NotSigned, result);
        Assert.Null(provider.Configuration);
    }

    [Fact]
    public async Task Provider_Should_ExposeConfiguration_When_ArchiveIsUnsignedAndNoTrustRootIsConfigured()
    {
        // arrange
        var fileName = IOPath.Combine(_directory, "gateway.far");
        await CreateValidArchiveAsync(fileName);
        var options = new FileSystemConfigurationOptions();

        await using var provider = new FileSystemFusionConfigurationProvider(fileName, diagnosticEvents: null, options);

        // act
        var configuration = await WaitForConfigurationAsync(provider);

        // assert
        var queryType = Assert.IsType<ObjectTypeDefinitionNode>(Assert.Single(configuration.Schema.Definitions));
        Assert.Equal("Query", queryType.Name.Value);
    }

    [Fact]
    public async Task Provider_Should_ExposeConfiguration_When_ArchiveIsSignedByATrustedCertificate()
    {
        // arrange
        var fileName = IOPath.Combine(_directory, "gateway.far");
        using var signingCertificate = CreateTestCertificate();
        using var unrelatedCertificate = CreateTestCertificate();
        await CreateSignedArchiveAsync(fileName, signingCertificate);
        var options = new FileSystemConfigurationOptions
        {
            TrustedSigningCertificates =
            [
                ToPublicCertificate(unrelatedCertificate),
                ToPublicCertificate(signingCertificate)
            ]
        };

        await using var provider = new FileSystemFusionConfigurationProvider(fileName, diagnosticEvents: null, options);

        // act
        var configuration = await WaitForConfigurationAsync(provider);

        // assert
        var queryType = Assert.IsType<ObjectTypeDefinitionNode>(Assert.Single(configuration.Schema.Definitions));
        Assert.Equal("Query", queryType.Name.Value);
    }

    private static async Task<FusionConfiguration> WaitForConfigurationAsync(
        FileSystemFusionConfigurationProvider provider)
    {
        var tcs = new TaskCompletionSource<FusionConfiguration>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var subscription = provider.Subscribe(
            Observer.Create<FusionConfiguration>(
                onNext: c => tcs.TrySetResult(c),
                onError: ex => tcs.TrySetException(ex),
                onCompleted: () => tcs.TrySetCanceled()));

        using var cts = new CancellationTokenSource(s_timeout);
        await using var registration = cts.Token.Register(() => tcs.TrySetCanceled());

        return await tcs.Task;
    }

    private static async Task<Exception> ReadWithTimeoutAsync(ChannelReader<Exception> reader)
    {
        using var cts = new CancellationTokenSource(s_timeout);
        return await reader.ReadAsync(cts.Token);
    }

    private static async Task<SignatureVerificationResult> ReadWithTimeoutAsync(
        ChannelReader<SignatureVerificationResult> reader)
    {
        using var cts = new CancellationTokenSource(s_timeout);
        return await reader.ReadAsync(cts.Token);
    }

    private static async Task CreateValidArchiveAsync(string fileName)
    {
        using var archive = FusionArchive.Create(fileName);

        await archive.SetArchiveMetadataAsync(
            new ArchiveMetadata
            {
                SupportedGatewayFormats = [WellKnownVersions.LatestGatewayFormatVersion],
                SourceSchemas = []
            });

        await archive.SetGatewayConfigurationAsync(
            "type Query { hello: String }",
            JsonDocument.Parse("{ }"),
            WellKnownVersions.LatestGatewayFormatVersion);

        await archive.CommitAsync();
    }

    private static async Task CreateArchiveWithMalformedSchemaAsync(string fileName)
    {
        using var archive = FusionArchive.Create(fileName);

        await archive.SetArchiveMetadataAsync(
            new ArchiveMetadata
            {
                SupportedGatewayFormats = [WellKnownVersions.LatestGatewayFormatVersion],
                SourceSchemas = []
            });

        // SetGatewayConfigurationAsync writes the schema text as-is, without parsing it, so
        // this produces a well-formed archive whose schema fails to parse when read back.
        await archive.SetGatewayConfigurationAsync(
            "type Query { hello: ",
            JsonDocument.Parse("{ }"),
            WellKnownVersions.LatestGatewayFormatVersion);

        await archive.CommitAsync();
    }

    private static async Task CreateSignedArchiveAsync(string fileName, X509Certificate2 certificate)
    {
        using var archive = FusionArchive.Create(fileName);

        await archive.SetArchiveMetadataAsync(
            new ArchiveMetadata
            {
                SupportedGatewayFormats = [WellKnownVersions.LatestGatewayFormatVersion],
                SourceSchemas = []
            });

        await archive.SetGatewayConfigurationAsync(
            "type Query { hello: String }",
            JsonDocument.Parse("{ }"),
            WellKnownVersions.LatestGatewayFormatVersion);

        await archive.SignArchiveAsync(certificate);
        await archive.CommitAsync();
    }

    private static async Task TamperGatewaySchemaEntryAsync(string fileName)
    {
        // Rewrite a listed entry directly through the zip, bypassing manifest regeneration, so
        // the file digest recorded in the manifest no longer matches the entry content.
        var entryName = $"gateway/{WellKnownVersions.LatestGatewayFormatVersion}/gateway.graphqls";
#if NET10_0_OR_GREATER
        await using var zip = ZipFile.Open(fileName, ZipArchiveMode.Update);
#else
        using var zip = ZipFile.Open(fileName, ZipArchiveMode.Update);
#endif
        zip.GetEntry(entryName)!.Delete();
        var entry = zip.CreateEntry(entryName);
        await using var entryStream = entry.Open();
        await entryStream.WriteAsync("type Query { tampered: String }"u8.ToArray());
    }

    private static X509Certificate2 CreateTestCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
    }

    private static X509Certificate2 ToPublicCertificate(X509Certificate2 certificate)
#if NET9_0_OR_GREATER
        => X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));
#else
        => new(certificate.Export(X509ContentType.Cert));
#endif

    private sealed class RecordingDiagnosticEvents : FusionExecutionDiagnosticEventListener
    {
        public Channel<Exception> ConfigurationReadErrors { get; } = Channel.CreateUnbounded<Exception>();

        public Channel<SignatureVerificationResult> ConfigurationVerificationFailures { get; } =
            Channel.CreateUnbounded<SignatureVerificationResult>();

        public override void ConfigurationReadError(Exception error)
            => ConfigurationReadErrors.Writer.TryWrite(error);

        public override void ConfigurationVerificationFailed(SignatureVerificationResult result)
            => ConfigurationVerificationFailures.Writer.TryWrite(result);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // best effort cleanup, the OS may still hold a handle open briefly
        }
    }
}
