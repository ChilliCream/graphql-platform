using System.Text.Json;
using System.Threading.Channels;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion;

public sealed class GatewayFormatVersionTests
{
    [Fact]
    public async Task GatewayConfiguration_Should_Load_When_ProducedByComposition()
    {
        // arrange
        await using var stream = await ComposeGatewayArchiveAsync();

        // act
        var configuration = await LoadGatewayConfigurationThroughProviderAsync(stream);

        // assert
        Assert.Contains(
            configuration.Schema.Definitions,
            definition => definition is HotChocolate.Language.ObjectTypeDefinitionNode { Name.Value: "Query" });
    }

    [Fact]
    public async Task GatewayConfiguration_Should_Reject_When_OnlyGatewayFormatIs220()
    {
        // arrange
        await using var stream = await CreateGatewayArchiveAsync(new Version("2.2.0"));

        // act
        var exception = await LoadGatewayErrorThroughProviderAsync(stream);

        // assert
        Assert.IsType<InvalidOperationException>(exception);
        exception.Message.MatchInlineSnapshot(
            """
            The Fusion archive contains gateway formats 2.2.0, but this gateway supports up to 2.0.0. Upgrade the gateway to load this archive.
            """);
    }

    [Fact]
    public async Task GatewayConfiguration_Should_Reject_When_OnlyGatewayFormatIs300()
    {
        // arrange
        await using var stream = await CreateGatewayArchiveAsync(new Version("3.0.0"));

        // act
        var exception = await LoadGatewayErrorThroughProviderAsync(stream);

        // assert
        Assert.IsType<InvalidOperationException>(exception);
        exception.Message.MatchInlineSnapshot(
            """
            The Fusion archive contains gateway formats 3.0.0, but this gateway supports up to 2.0.0. Upgrade the gateway to load this archive.
            """);
    }

    [Fact]
    public async Task GatewayConfiguration_Should_LoadSupportedFormat_When_ArchiveContainsNewerFormat()
    {
        // arrange
        await using var stream = await CreateGatewayArchiveAsync(
            new Version("2.0.0"), additionalVersion: new Version("3.0.0"));

        // act
        var configuration = await LoadGatewayConfigurationThroughProviderAsync(stream);

        // assert
        Assert.Equal("type Query {\n  product: String\n}", configuration.Schema.ToString());
    }

    [Fact]
    public async Task FileSystemProvider_Should_Recover_When_UnsupportedInitialArchiveIsReplaced()
    {
        // arrange
        var fileName = CreateFileName();
        await using var unsupportedArchive = await CreateGatewayArchiveAsync(new Version("3.0.0"));
        await using var supportedArchive = await CreateGatewayArchiveAsync(new Version("2.0.0"));

        try
        {
            await WriteArchiveAsync(fileName, unsupportedArchive);
            await using var provider = new FileSystemFusionConfigurationProvider(fileName);
            var initialObserver = new ConfigurationObserver();
            using var initialSubscription = provider.Subscribe(initialObserver);
            var error = await initialObserver.Error.Task.WaitAsync(TestContext.Current.CancellationToken);

            // act
            await WriteArchiveAsync(fileName, supportedArchive);
            while (provider.Configuration is null)
            {
                await Task.Delay(25, TestContext.Current.CancellationToken);
            }

            var restoredObserver = new ConfigurationObserver();
            using var restoredSubscription = provider.Subscribe(restoredObserver);
            var configuration = await restoredObserver.ReadConfigurationAsync(TestContext.Current.CancellationToken);

            // assert
            Assert.IsType<InvalidOperationException>(error);
            Assert.Equal("type Query {\n  product: String\n}", configuration.Schema.ToString());
        }
        finally
        {
            File.Delete(fileName);
        }
    }

    [Fact]
    public async Task FileSystemProvider_Should_KeepExistingSubscription_When_GatewayFormatIsHigher()
    {
        // arrange
        var fileName = CreateFileName();
        await using var initialArchive = await CreateGatewayArchiveAsync(
            WellKnownVersions.LatestGatewayFormatVersion,
            "type Query { product: String }");
        await using var unsupportedArchive = await CreateGatewayArchiveAsync(
            new Version("3.0.0"),
            "type Query { product: String }");
        await using var updatedArchive = await CreateGatewayArchiveAsync(
            WellKnownVersions.LatestGatewayFormatVersion,
            "type Query { product: String name: String }");

        try
        {
            await WriteArchiveAsync(fileName, initialArchive);
            await using var provider = new FileSystemFusionConfigurationProvider(fileName);
            var observer = new ConfigurationObserver();
            using var subscription = provider.Subscribe(observer);
            await observer.ReadConfigurationAsync(TestContext.Current.CancellationToken);

            // act
            await WriteArchiveAsync(fileName, unsupportedArchive);
            await Task.Delay(TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken);
            await WriteArchiveAsync(fileName, updatedArchive);
            var configuration = await observer.ReadConfigurationAsync(TestContext.Current.CancellationToken);

            // assert
            Assert.False(observer.Error.Task.IsCompleted);
            Assert.Equal(
                "type Query {\n  product: String\n  name: String\n}",
                configuration.Schema.ToString());
        }
        finally
        {
            File.Delete(fileName);
        }
    }

    [Fact]
    public async Task FileSystemProvider_Should_ClearLoadError_When_SupportedConfigurationIsRestoredUnchanged()
    {
        // arrange
        var fileName = CreateFileName();
        await using var supportedArchive = await CreateGatewayArchiveAsync(
            WellKnownVersions.LatestGatewayFormatVersion);
        await using var unsupportedArchive = await CreateGatewayArchiveAsync(new Version("3.0.0"));

        try
        {
            await WriteArchiveAsync(fileName, supportedArchive);
            await using var provider = new FileSystemFusionConfigurationProvider(fileName);
            var existingObserver = new ConfigurationObserver();
            using var existingSubscription = provider.Subscribe(existingObserver);
            await existingObserver.ReadConfigurationAsync(TestContext.Current.CancellationToken);

            // act
            await WriteArchiveAsync(fileName, unsupportedArchive);
            await Task.Delay(TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken);
            await WriteArchiveAsync(fileName, supportedArchive);
            await Task.Delay(TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken);

            var restoredObserver = new ConfigurationObserver();
            using var restoredSubscription = provider.Subscribe(restoredObserver);
            var configuration = await restoredObserver.ReadConfigurationAsync(TestContext.Current.CancellationToken);

            // assert
            Assert.False(existingObserver.Error.Task.IsCompleted);
            Assert.Equal("type Query {\n  product: String\n}", configuration.Schema.ToString());
        }
        finally
        {
            File.Delete(fileName);
        }
    }

    private static async Task<FusionConfiguration> LoadGatewayConfigurationThroughProviderAsync(MemoryStream archive)
    {
        var fileName = CreateFileName();

        try
        {
            await WriteArchiveAsync(fileName, archive);
            await using var provider = new FileSystemFusionConfigurationProvider(fileName);
            var observer = new ConfigurationObserver();
            using var subscription = provider.Subscribe(observer);
            return await observer.ReadConfigurationAsync(TestContext.Current.CancellationToken);
        }
        finally
        {
            File.Delete(fileName);
        }
    }

    private static async Task<Exception> LoadGatewayErrorThroughProviderAsync(MemoryStream archive)
    {
        var fileName = CreateFileName();

        try
        {
            await WriteArchiveAsync(fileName, archive);
            await using var provider = new FileSystemFusionConfigurationProvider(fileName);
            var observer = new ConfigurationObserver();
            using var subscription = provider.Subscribe(observer);
            return await observer.Error.Task.WaitAsync(TestContext.Current.CancellationToken);
        }
        finally
        {
            File.Delete(fileName);
        }
    }

    private static async Task<MemoryStream> ComposeGatewayArchiveAsync()
    {
        var stream = new MemoryStream();
        using var settings = JsonDocument.Parse("""{ "name": "Products" }""");
        var sourceSchemas = new Dictionary<string, LocalSourceSchema>
        {
            ["Products"] = new(
                new SourceSchemaText("Products", "type Query { product: String }"),
                settings,
                urlOverride: null)
        };
        var log = new CompositionLog();

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            var result = await CompositionHelper.ComposeAsync(
                log,
                sourceSchemas,
                archive,
                "Development",
                preferDevUrls: false,
                compositionSettings: null,
                legacyArchive: null,
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess, string.Join(Environment.NewLine, log.Select(entry => entry.Message)));
        }

        stream.Position = 0;
        return stream;
    }

    private static async Task<MemoryStream> CreateGatewayArchiveAsync(
        Version gatewayFormatVersion,
        string schema = "type Query { product: String }",
        Version? additionalVersion = null)
    {
        var stream = new MemoryStream();
        var metadata = new ArchiveMetadata
        {
            SupportedGatewayFormats = additionalVersion is null ? [gatewayFormatVersion] : [gatewayFormatVersion, additionalVersion],
            SourceSchemas = []
        };
        using var settings = JsonDocument.Parse("{}");

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata, TestContext.Current.CancellationToken);
            await archive.SetGatewayConfigurationAsync(
                schema,
                settings,
                gatewayFormatVersion,
                TestContext.Current.CancellationToken);
            if (additionalVersion is not null)
            {
                await archive.SetGatewayConfigurationAsync(
                    "type Query { newer: String }",
                    settings,
                    additionalVersion,
                    TestContext.Current.CancellationToken);
            }
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        stream.Position = 0;
        return stream;
    }

    private static string CreateFileName()
        => IOPath.Combine(IOPath.GetTempPath(), $"{Guid.NewGuid()}.far");

    private static Task WriteArchiveAsync(string fileName, MemoryStream archive)
        => File.WriteAllBytesAsync(fileName, archive.ToArray(), TestContext.Current.CancellationToken);

    private sealed class ConfigurationObserver : IObserver<FusionConfiguration>
    {
        private readonly Channel<FusionConfiguration> _configurations = Channel.CreateUnbounded<FusionConfiguration>();

        public TaskCompletionSource<Exception> Error { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void OnCompleted()
            => Error.TrySetCanceled();

        public void OnError(Exception error)
            => Error.TrySetResult(error);

        public void OnNext(FusionConfiguration value)
            => _configurations.Writer.TryWrite(value);

        public ValueTask<FusionConfiguration> ReadConfigurationAsync(CancellationToken cancellationToken)
            => _configurations.Reader.ReadAsync(cancellationToken);
    }
}
