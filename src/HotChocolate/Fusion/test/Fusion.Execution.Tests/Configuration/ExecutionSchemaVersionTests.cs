using System.Text.Json;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion;

public sealed class ExecutionSchemaVersionTests
{
    [Fact]
    public async Task GatewayConfiguration_Should_SupportExecutionSchemaVersion_When_ProducedByComposition()
    {
        // arrange
        await using var stream = await ComposeGatewayArchiveAsync();
        Version executionSchemaVersion;

        stream.Position = 0;
        using (var readArchive = FusionArchive.Open(stream, leaveOpen: true))
        {
            var metadata = Assert.IsType<ArchiveMetadata>(
                await readArchive.GetArchiveMetadataAsync(TestContext.Current.CancellationToken));
            executionSchemaVersion = metadata.ExecutionSchemaVersion;
        }

        Assert.True(
            executionSchemaVersion <= WellKnownVersions.MaxSupportedExecutionSchemaVersion,
            "Confirm that the gateway understands the produced execution schema format, then raise "
            + "MaxSupportedExecutionSchemaVersion in WellKnownVersions.");

        // act
        stream.Position = 0;
        using var archive = FusionArchive.Open(stream, leaveOpen: true);
        using var configuration = await FileSystemFusionConfigurationProvider.LoadGatewayConfigurationAsync(
            archive,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(WellKnownVersions.LatestGatewayFormatVersion, configuration?.Version);
    }

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("0.9.0")]
    public async Task GatewayConfiguration_Should_Load_When_ExecutionSchemaVersionIsSupported(string version)
    {
        // arrange
        await using var stream = await CreateGatewayArchiveAsync(new Version(version));
        stream.Position = 0;
        using var archive = FusionArchive.Open(stream, leaveOpen: true);

        // act
        using var configuration = await FileSystemFusionConfigurationProvider.LoadGatewayConfigurationAsync(
            archive,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(WellKnownVersions.LatestGatewayFormatVersion, configuration?.Version);
    }

    [Theory]
    [InlineData("2.0.0")]
    [InlineData("1.1.0")]
    [InlineData("1.0.1")]
    public async Task GatewayConfiguration_Should_Reject_When_ExecutionSchemaVersionIsHigher(string version)
    {
        // arrange
        await using var stream = await CreateGatewayArchiveAsync(new Version(version));
        stream.Position = 0;
        using var archive = FusionArchive.Open(stream, leaveOpen: true);

        // act
        Task Act() => FileSystemFusionConfigurationProvider.LoadGatewayConfigurationAsync(
            archive,
            TestContext.Current.CancellationToken);

        // assert
        await Assert.ThrowsAsync<InvalidOperationException>(Act);
    }

    [Fact]
    public async Task GatewayConfiguration_Should_DescribeSupportedVersion_When_ExecutionSchemaVersionIsHigher()
    {
        // arrange
        await using var stream = await CreateGatewayArchiveAsync(new Version("1.0.1"));
        stream.Position = 0;
        using var archive = FusionArchive.Open(stream, leaveOpen: true);

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => FileSystemFusionConfigurationProvider.LoadGatewayConfigurationAsync(
                archive,
                TestContext.Current.CancellationToken));

        // assert
        exception.Message.MatchInlineSnapshot(
            """
            The Fusion archive requires execution schema version 1.0.1, but this gateway supports up to 1.0.0. Upgrade the gateway to load this archive.
            """);
    }

    [Fact]
    public async Task FileSystemProvider_Should_ExposeError_When_ExecutionSchemaVersionIsHigher()
    {
        // arrange
        var fileName = IOPath.Combine(IOPath.GetTempPath(), $"{Guid.NewGuid()}.far");

        try
        {
            await using var stream = await CreateGatewayArchiveAsync(new Version("1.0.1"));
            await File.WriteAllBytesAsync(
                fileName,
                stream.ToArray(),
                TestContext.Current.CancellationToken);

            await using (var provider = new FileSystemFusionConfigurationProvider(fileName))
            {
                var initialObserver = new ErrorObserver();
                var observer = new ErrorObserver();

                // act
                using var initialSubscription = provider.Subscribe(initialObserver);
                await initialObserver.Error.Task.WaitAsync(TestContext.Current.CancellationToken);

                using var subscription = provider.Subscribe(observer);
                var exception = await observer.Error.Task.WaitAsync(TestContext.Current.CancellationToken);

                // assert
                exception.Message.MatchInlineSnapshot(
                    """
                    The Fusion archive requires execution schema version 1.0.1, but this gateway supports up to 1.0.0. Upgrade the gateway to load this archive.
                    """);
            }
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

    private static async Task<MemoryStream> CreateGatewayArchiveAsync(Version executionSchemaVersion)
    {
        var stream = new MemoryStream();
        var metadata = new ArchiveMetadata
        {
            ExecutionSchemaVersion = executionSchemaVersion,
            SupportedGatewayFormats = [WellKnownVersions.LatestGatewayFormatVersion],
            SourceSchemas = []
        };
        using var settings = JsonDocument.Parse("{}");

        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(metadata, TestContext.Current.CancellationToken);
            await archive.SetGatewayConfigurationAsync(
                "type Query { product: String }",
                settings,
                WellKnownVersions.LatestGatewayFormatVersion,
                TestContext.Current.CancellationToken);
            await archive.CommitAsync(TestContext.Current.CancellationToken);
        }

        stream.Position = 0;
        return stream;
    }

    private sealed class ErrorObserver : IObserver<FusionConfiguration>
    {
        public TaskCompletionSource<Exception> Error { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void OnCompleted()
            => Error.TrySetCanceled();

        public void OnError(Exception error)
            => Error.TrySetResult(error);

        public void OnNext(FusionConfiguration value)
            => Error.TrySetException(new InvalidOperationException("The archive should not load."));
    }
}
