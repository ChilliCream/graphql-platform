using System.Reactive;
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
        // successfully. Otherwise, rewriting the exact same malformed bytes a second
        // time would be treated as already handled and silently skipped forever.
        var fileName = IOPath.Combine(_directory, "gateway.far");
        var malformedBytes = "this is not a fusion archive"u8.ToArray();
        await File.WriteAllBytesAsync(fileName, malformedBytes, TestContext.Current.CancellationToken);
        var diagnosticEvents = new RecordingDiagnosticEvents();

        // act
        await using var provider = new FileSystemFusionConfigurationProvider(fileName, diagnosticEvents);
        var firstError = await ReadWithTimeoutAsync(diagnosticEvents.ConfigurationReadErrors.Reader);

        await File.WriteAllBytesAsync(fileName, malformedBytes, TestContext.Current.CancellationToken);
        var secondError = await ReadWithTimeoutAsync(diagnosticEvents.ConfigurationReadErrors.Reader);

        // assert
        Assert.NotNull(firstError);
        Assert.NotNull(secondError);
        Assert.Null(provider.Configuration);
    }

    [Fact]
    public async Task Provider_Should_RetryReadingUnchangedBytes_When_PackageSchemaIsMalformed()
    {
        // arrange
        // The package hash must only be committed once the schema, settings, and policy
        // content have all been read and parsed successfully. Otherwise, rewriting the exact
        // same malformed bytes a second time would be treated as already handled and silently
        // skipped forever, even though the archive itself opens without error.
        var fileName = IOPath.Combine(_directory, "gateway.far");
        await CreateArchiveWithMalformedSchemaAsync(fileName);
        var malformedBytes = await File.ReadAllBytesAsync(fileName, TestContext.Current.CancellationToken);
        var diagnosticEvents = new RecordingDiagnosticEvents();

        // act
        await using var provider = new FileSystemFusionConfigurationProvider(fileName, diagnosticEvents);
        var firstError = await ReadWithTimeoutAsync(diagnosticEvents.ConfigurationReadErrors.Reader);

        await File.WriteAllBytesAsync(fileName, malformedBytes, TestContext.Current.CancellationToken);
        var secondError = await ReadWithTimeoutAsync(diagnosticEvents.ConfigurationReadErrors.Reader);

        // assert
        Assert.NotNull(firstError);
        Assert.NotNull(secondError);
        Assert.Null(provider.Configuration);
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

    private static async Task<Exception?> ReadWithTimeoutAsync(ChannelReader<Exception> reader)
    {
        using var cts = new CancellationTokenSource(s_timeout);

        try
        {
            return await reader.ReadAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
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

    private sealed class RecordingDiagnosticEvents : FusionExecutionDiagnosticEventListener
    {
        public Channel<Exception> ConfigurationReadErrors { get; } = Channel.CreateUnbounded<Exception>();

        public override void ConfigurationReadError(Exception error)
            => ConfigurationReadErrors.Writer.TryWrite(error);
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
