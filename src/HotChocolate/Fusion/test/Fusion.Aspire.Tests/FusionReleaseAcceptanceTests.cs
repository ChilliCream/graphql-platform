using System.Net;
using System.Text;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using HotChocolate.Fusion.Aspire.Nitro;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Fusion.SourceSchema.Packaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES004

public sealed class FusionReleaseAcceptanceTests
{
    [Fact]
    public async Task Release_Should_UploadOnceAndPublishFromNitroAcrossRunners()
    {
        // arrange
        using var testDirectory = new TestDirectory();
        var sourceCheckout = IOPath.Combine(
            testDirectory.Path,
            "runner-a-checkout");
        var runnerAOutput = IOPath.Combine(
            testDirectory.Path,
            "runner-a-output");
        var runnerB = IOPath.Combine(testDirectory.Path, "unrelated-runner-b");
        var runnerC = IOPath.Combine(testDirectory.Path, "unrelated-runner-c");
        Directory.CreateDirectory(sourceCheckout);

        var productsProjectPath = await CreateSourceCheckoutAsync(
            sourceCheckout,
            "Products",
            "products",
            "Product");
        var reviewsProjectPath = await CreateSourceCheckoutAsync(
            sourceCheckout,
            "Reviews",
            "reviews",
            "Review");
        var gatewayDirectory = IOPath.Combine(sourceCheckout, "Gateway");
        Directory.CreateDirectory(gatewayDirectory);
        var gatewayProjectPath = IOPath.Combine(
            gatewayDirectory,
            "Gateway.csproj");
        await File.WriteAllTextAsync(
            gatewayProjectPath,
            "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            TestContext.Current.CancellationToken);

        using var nitro = new FakeNitro();
        var executor = FusionPipelineExecutor.Instance;
        var buildModel = CreateModel(
            productsProjectPath,
            reviewsProjectPath,
            gatewayProjectPath);
        var buildContext = CreateContext(
            buildModel,
            "Development",
            runnerAOutput,
            nitro);

        // act
        await executor.CreateArtifactsAsync(buildContext);
        await executor.UploadAsync(buildContext);

        // assert
        Assert.Equal(
            [
                new FusionSourceSchemaVersion("products", "release-1"),
                new FusionSourceSchemaVersion("reviews", "release-1")
            ],
            nitro.Uploads.OrderBy(source => source.Name, StringComparer.Ordinal));

        var runnerBProjects = await CreateAppHostProjectStubsAsync(runnerB);
        var runnerCProjects = await CreateAppHostProjectStubsAsync(runnerC);
        var runnerBTree = SnapshotTree(runnerB);
        var runnerCTree = SnapshotTree(runnerC);
        Directory.Delete(sourceCheckout, recursive: true);
        Assert.False(Directory.Exists(sourceCheckout));

        var stagingModel = CreateModel(
            runnerBProjects.ProductsProjectPath,
            runnerBProjects.ReviewsProjectPath,
            runnerBProjects.GatewayProjectPath);
        var stagingContext = CreateContext(
            stagingModel,
            "Development",
            outputPath: null,
            nitro: nitro);
        using var stagingSession = new FusionPipelineSession(
            stagingContext.CancellationToken);
        await executor.PreflightAsync(stagingContext, stagingSession);

        var productionContext = CreateContext(
            CreateModel(
                runnerCProjects.ProductsProjectPath,
                runnerCProjects.ReviewsProjectPath,
                runnerCProjects.GatewayProjectPath,
                stageName: "test"),
            "Test",
            outputPath: null,
            nitro: nitro);
        using var productionSession = new FusionPipelineSession(
            productionContext.CancellationToken);
        await executor.PreflightAsync(productionContext, productionSession);

        Assert.Equal(1, stagingSession.DeploymentCount);
        Assert.Equal(1, productionSession.DeploymentCount);
        var stagingDeployment = Assert.Single(
            await FusionPipeline.SelectStagesAsync(
                stagingModel,
                TestContext.Current.CancellationToken));
        var preflightState = stagingSession.GetState(stagingDeployment);
        Assert.Equal(0, preflightState.SourceArchiveBytes);
        Assert.Throws<InvalidOperationException>(
            () => preflightState.Sources);

        await executor.DownloadAsync(stagingContext, stagingSession);
        await executor.ComposeAsync(stagingContext, stagingSession);
        await executor.DownloadAsync(productionContext, productionSession);
        await executor.ComposeAsync(productionContext, productionSession);
        await executor.PublishAsync(stagingContext, stagingSession);

        Assert.Equal(0, stagingSession.DeploymentCount);
        Assert.Equal(1, productionSession.DeploymentCount);

        await executor.PublishAsync(productionContext, productionSession);

        Assert.Equal(0, productionSession.DeploymentCount);

        // every Fusion source is read back from Nitro once per reconciliation and
        // once per preflight and composition of both deployments.
        $"""
        Uploads: {nitro.Uploads.Count}
        Downloads: {DescribeDownloads(nitro)}
        """.MatchInlineSnapshot(
            """
            Uploads: 2
            Downloads: products@release-1 x5, reviews@release-1 x5
            """);

        var publications = nitro.Publications
            .OrderBy(publication => publication.Stage, StringComparer.Ordinal)
            .ToArray();
        Assert.Collection(
            publications,
            development =>
            {
                Assert.Equal("development", development.Stage);
                Assert.Equal(
                    new Dictionary<string, string>
                    {
                        ["products"] = "https://products.development.example.com/graphql",
                        ["reviews"] = "https://reviews.development.example.com/graphql"
                    },
                    ReadSourceUrls(development.FusionArchive));
                Assert.Equal(
                    [
                        new FusionSourceSchemaVersion("products", "release-1"),
                        new FusionSourceSchemaVersion("reviews", "release-1")
                    ],
                    development.Sources);
            },
            test =>
            {
                Assert.Equal("test", test.Stage);
                Assert.Equal(
                    new Dictionary<string, string>
                    {
                        ["products"] = "https://products.test.example.com/graphql",
                        ["reviews"] = "https://reviews.test.example.com/graphql"
                    },
                    ReadSourceUrls(test.FusionArchive));
                Assert.Equal(
                    [
                        new FusionSourceSchemaVersion("products", "release-1"),
                        new FusionSourceSchemaVersion("reviews", "release-1")
                    ],
                    test.Sources);
            });
        Assert.NotEqual(
            Convert.ToBase64String(nitro.Publications[0].FusionArchive),
            Convert.ToBase64String(nitro.Publications[1].FusionArchive));
        Assert.Equal(runnerBTree, SnapshotTree(runnerB));
        Assert.Equal(runnerCTree, SnapshotTree(runnerC));

        using var repeatedStagingSession = new FusionPipelineSession(
            stagingContext.CancellationToken);
        await executor.PreflightAsync(stagingContext, repeatedStagingSession);
        await executor.DownloadAsync(stagingContext, repeatedStagingSession);
        await executor.ComposeAsync(stagingContext, repeatedStagingSession);
        await executor.PublishAsync(stagingContext, repeatedStagingSession);

        Assert.Equal(0, repeatedStagingSession.DeploymentCount);
        Assert.Equal(3, nitro.Publications.Count);
        Assert.Equal(
            Convert.ToBase64String(nitro.Publications[0].FusionArchive),
            Convert.ToBase64String(nitro.Publications[2].FusionArchive));
        Assert.Equal(runnerBTree, SnapshotTree(runnerB));
    }

    [Fact]
    public async Task Release_Should_PublishToTheStage_That_TheParameterSupplies()
    {
        // arrange
        // one deployment declaration that is not bound to an environment publishes to the stage
        // that each publish supplies, so the same AppHost serves every stage.
        using var testDirectory = new TestDirectory();
        using var nitro = await CreateSeededNitroAsync();
        var executor = FusionPipelineExecutor.Instance;
        var projects = await CreateAppHostProjectStubsAsync(testDirectory.Path);

        // act
        (string Stage, string Environment)[] publishes =
        [
            ("development", "Development"),
            ("test", "Production")
        ];

        foreach (var (stage, environment) in publishes)
        {
            var context = CreateContext(
                CreateModel(projects, stage),
                environment,
                outputPath: null,
                nitro: nitro);
            using var session = new FusionPipelineSession(
                context.CancellationToken);
            await executor.PreflightAsync(context, session);
            await executor.DownloadAsync(context, session);
            await executor.ComposeAsync(context, session);
            await executor.PublishAsync(context, session);
        }

        // assert
        Assert.Equal(
            ["development", "test"],
            nitro.Publications.Select(publication => publication.Stage));
        Assert.All(
            nitro.Publications,
            publication => Assert.Equal(
                [
                    new FusionSourceSchemaVersion("products", "release-1"),
                    new FusionSourceSchemaVersion("reviews", "release-1")
                ],
                publication.Sources));
    }

    [Fact]
    public async Task CreateArtifacts_Should_WriteAnOutputDirectoryThatUploadReadsBack()
    {
        // arrange
        // the source checkout is deleted before the upload so the upload can only
        // succeed by reading the publish output written by the artifacts step.
        using var testDirectory = new TestDirectory();
        var sourceCheckout = IOPath.Combine(testDirectory.Path, "checkout");
        var output = IOPath.Combine(testDirectory.Path, "output");
        Directory.CreateDirectory(sourceCheckout);
        var productsProjectPath = await CreateSourceCheckoutAsync(
            sourceCheckout,
            "Products",
            "products",
            "Product");
        var reviewsProjectPath = await CreateSourceCheckoutAsync(
            sourceCheckout,
            "Reviews",
            "reviews",
            "Review");
        var gatewayDirectory = IOPath.Combine(sourceCheckout, "Gateway");
        Directory.CreateDirectory(gatewayDirectory);
        var gatewayProjectPath = IOPath.Combine(
            gatewayDirectory,
            "Gateway.csproj");
        await File.WriteAllTextAsync(
            gatewayProjectPath,
            "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            TestContext.Current.CancellationToken);
        using var nitro = new FakeNitro();
        var executor = FusionPipelineExecutor.Instance;
        var model = CreateModel(
            productsProjectPath,
            reviewsProjectPath,
            gatewayProjectPath);

        // act
        await executor.CreateArtifactsAsync(
            CreateContext(model, "Development", output, nitro));
        var artifactTree = SnapshotTree(output);
        Directory.Delete(sourceCheckout, recursive: true);
        await executor.UploadAsync(
            CreateContext(model, "Development", output, nitro));

        // assert
        NormalizeTree(artifactTree).MatchInlineSnapshot(
            """
            fusion
            fusion/nitro
            fusion/nitro/nitro-deployment-template.json
            fusion/nitro/sources
            fusion/nitro/sources/products
            fusion/nitro/sources/products/provenance.json
            fusion/nitro/sources/products/schema-settings.template.json
            fusion/nitro/sources/products/schema.graphqls
            fusion/nitro/sources/reviews
            fusion/nitro/sources/reviews/provenance.json
            fusion/nitro/sources/reviews/schema-settings.template.json
            fusion/nitro/sources/reviews/schema.graphqls
            """);
        NormalizeTree(SnapshotTree(output)).MatchInlineSnapshot(
            """
            fusion
            fusion/nitro
            fusion/nitro/materialized
            fusion/nitro/materialized/products-release-1.zip
            fusion/nitro/materialized/reviews-release-1.zip
            fusion/nitro/nitro-deployment-template.json
            fusion/nitro/sources
            fusion/nitro/sources/products
            fusion/nitro/sources/products/provenance.json
            fusion/nitro/sources/products/schema-settings.template.json
            fusion/nitro/sources/products/schema.graphqls
            fusion/nitro/sources/reviews
            fusion/nitro/sources/reviews/provenance.json
            fusion/nitro/sources/reviews/schema-settings.template.json
            fusion/nitro/sources/reviews/schema.graphqls
            """);
        Assert.Equal(
            [
                new FusionSourceSchemaVersion("products", "release-1"),
                new FusionSourceSchemaVersion("reviews", "release-1")
            ],
            nitro.Uploads.OrderBy(source => source.Name, StringComparer.Ordinal));
    }

    [Fact]
    public async Task Publish_Should_ClearSessionBuffers_WhenNitroFailsToOpenTheRequest()
    {
        // arrange
        using var testDirectory = new TestDirectory();
        using var nitro = await CreateSeededNitroAsync();
        var context = CreateContext(
            CreateModel(
                await CreateAppHostProjectStubsAsync(testDirectory.Path)),
            "Development",
            outputPath: null,
            nitro: nitro);
        using var session = new FusionPipelineSession(context.CancellationToken);
        var executor = FusionPipelineExecutor.Instance;
        await executor.PreflightAsync(context, session);
        await executor.DownloadAsync(context, session);
        await executor.ComposeAsync(context, session);
        var deployment = Assert.Single(
            await FusionPipeline.SelectStagesAsync(
                context.Model,
                TestContext.Current.CancellationToken));
        var state = session.GetState(deployment);
        var sourceBuffers = state.Sources
            .Select(source => source.Archive)
            .ToArray();
        var fusionArchiveBuffer = state.FusionArchive;
        nitro.BeginException = new HttpRequestException("connection refused");

        // act
        var exception =
            await Assert.ThrowsAsync<FusionIndeterminateStateException>(
                () => executor.PublishAsync(context, session));

        // assert
        Assert.Equal(
            "The Fusion publication request may have been created, but Nitro did "
            + "not return a verifiable result.",
            exception.Message);
        Assert.Equal(0, session.DeploymentCount);
        Assert.All(
            sourceBuffers,
            buffer => Assert.Equal(new byte[buffer.Length], buffer));
        Assert.Equal(
            new byte[fusionArchiveBuffer.Length],
            fusionArchiveBuffer);
    }

    [Fact]
    public async Task Publish_Should_ClearSessionBuffers_WhenThePipelineIsCanceled()
    {
        // arrange
        using var testDirectory = new TestDirectory();
        using var nitro = await CreateSeededNitroAsync();
        using var publishCancellation = new CancellationTokenSource();
        var context = CreateContext(
            CreateModel(
                await CreateAppHostProjectStubsAsync(testDirectory.Path)),
            "Development",
            outputPath: null,
            nitro: nitro,
            cancellationToken: publishCancellation.Token);
        using var session = new FusionPipelineSession(publishCancellation.Token);
        var executor = FusionPipelineExecutor.Instance;
        await executor.PreflightAsync(context, session);
        await executor.DownloadAsync(context, session);
        await executor.ComposeAsync(context, session);
        var deployment = Assert.Single(
            await FusionPipeline.SelectStagesAsync(
                context.Model,
                TestContext.Current.CancellationToken));
        var state = session.GetState(deployment);
        var sourceBuffers = state.Sources
            .Select(source => source.Archive)
            .ToArray();
        var fusionArchiveBuffer = state.FusionArchive;
        nitro.BlockNextPublication();

        // act
        var publish = executor.PublishAsync(context, session);
        await nitro.WaitForBlockedPublicationAsync();
        await publishCancellation.CancelAsync();
        nitro.ContinueBlockedPublication();

        // assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => publish);
        Assert.Empty(nitro.Publications);
        Assert.Equal(0, session.DeploymentCount);
        Assert.All(
            sourceBuffers,
            buffer => Assert.Equal(new byte[buffer.Length], buffer));
        Assert.Equal(
            new byte[fusionArchiveBuffer.Length],
            fusionArchiveBuffer);
    }

    [Fact]
    public async Task Download_Should_ClearSession_WhenExactSourceIsMissing()
    {
        // arrange
        using var testDirectory = new TestDirectory();
        using var nitro = new FakeNitro();
        var context = CreateContext(
            CreateModel(
                await CreateAppHostProjectStubsAsync(testDirectory.Path)),
            "Development",
            outputPath: null,
            nitro: nitro);
        using var session = new FusionPipelineSession(
            context.CancellationToken);

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => FusionPipelineExecutor.Instance.PreflightAsync(
                context,
                session));

        // assert
        Assert.Equal(
            "Fusion source 'products' version 'release-1' does not exist on target 'products'.",
            exception.Message);
        Assert.Equal(0, session.DeploymentCount);
        Assert.Empty(nitro.Uploads);
        Assert.Empty(nitro.Publications);
    }

    [Fact]
    public async Task Download_Should_RejectSourceArchive_WhenPerSourceLimitIsExceeded()
    {
        // arrange
        using var testDirectory = new TestDirectory();
        using var nitro = await CreateSeededNitroAsync();
        var context = CreateContext(
            CreateModel(
                await CreateAppHostProjectStubsAsync(testDirectory.Path)),
            "Development",
            outputPath: null,
            nitro: nitro);
        var limits = new FusionPipelineMemoryLimits(
            SourceArchiveBytes: 2,
            TotalSourceArchiveBytes: 100);
        var executor = new FusionPipelineExecutor(limits);
        using var session = new FusionPipelineSession(
            context.CancellationToken,
            limits);

        // act
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => executor.PreflightAsync(context, session));

        // assert
        Assert.Equal(
            "Downloaded Fusion source 'products@release-1' exceeds the "
            + "2-byte per-source in-memory size limit.",
            exception.Message);
        Assert.Equal(0, session.DeploymentCount);
    }

    [Fact]
    public async Task Download_Should_RejectSourceArchives_WhenAggregateLimitIsExceeded()
    {
        // arrange
        using var testDirectory = new TestDirectory();
        using var nitro = await CreateSeededNitroAsync();
        var context = CreateContext(
            CreateModel(
                await CreateAppHostProjectStubsAsync(testDirectory.Path)),
            "Development",
            outputPath: null,
            nitro: nitro);
        var limits = new FusionPipelineMemoryLimits(
            SourceArchiveBytes: 100_000,
            TotalSourceArchiveBytes: 2);
        var executor = new FusionPipelineExecutor(limits);
        using var session = new FusionPipelineSession(
            context.CancellationToken,
            limits);

        // act
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => executor.PreflightAsync(context, session));

        // assert
        Assert.Equal(
            "The downloaded Fusion sources exceed the 2-byte aggregate "
            + "in-memory size limit.",
            exception.Message);
        Assert.Equal(0, session.DeploymentCount);
    }

    [Fact]
    public async Task Download_Should_FailBeforeComposition_WhenExactSourceChangesAfterPreflight()
    {
        // arrange
        using var testDirectory = new TestDirectory();
        using var nitro = await CreateSeededNitroAsync();
        var context = CreateContext(
            CreateModel(
                await CreateAppHostProjectStubsAsync(testDirectory.Path)),
            "Development",
            outputPath: null,
            nitro: nitro);
        using var session = new FusionPipelineSession(
            context.CancellationToken);
        var executor = FusionPipelineExecutor.Instance;
        await executor.PreflightAsync(context, session);
        nitro.Seed(
            "products",
            "release-1",
            await CreateSourceArchiveAsync("products", "ChangedProduct"));

        // act
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => executor.DownloadAsync(context, session));

        // assert
        Assert.Equal(
            "Fusion source 'products@release-1' changed between preflight and composition.",
            exception.Message);
        Assert.Equal(0, session.DeploymentCount);
        Assert.Empty(nitro.Publications);
    }

    [Fact]
    public async Task Download_Should_ClearSession_WhenLaterSourceIsMissing()
    {
        // arrange
        using var testDirectory = new TestDirectory();
        using var nitro = await CreateSeededNitroAsync();
        var context = CreateContext(
            CreateModel(
                await CreateAppHostProjectStubsAsync(testDirectory.Path)),
            "Development",
            outputPath: null,
            nitro: nitro);
        using var session = new FusionPipelineSession(
            context.CancellationToken);
        var executor = FusionPipelineExecutor.Instance;
        await executor.PreflightAsync(context, session);
        nitro.Remove("reviews", "release-1");

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => executor.DownloadAsync(context, session));

        // assert
        Assert.Equal(
            "Fusion source 'reviews' version 'release-1' does not exist on target 'products'.",
            exception.Message);
        Assert.Equal(0, session.DeploymentCount);
    }

    [Fact]
    public async Task SetAll_Should_ClearOwnedBuffers_WhenCancellationPrecedesTransfer()
    {
        // arrange
        using var testDirectory = new TestDirectory();
        var model = CreateModel(
            await CreateAppHostProjectStubsAsync(testDirectory.Path));
        var deployment = Assert.Single(
            await FusionPipeline.SelectStagesAsync(
                model,
                TestContext.Current.CancellationToken));
        using var cancellationSource = new CancellationTokenSource();
        using var session = new FusionPipelineSession(
            cancellationSource.Token);
        var sourceArchive = new byte[] { 1, 2, 3 };
        var state = CreateSessionState(sourceArchive, fusionArchive: null);

        // act
        await cancellationSource.CancelAsync();

        // assert
        Assert.Throws<OperationCanceledException>(
            () => session.SetAll([(deployment, state)]));
        Assert.Equal(0, session.DeploymentCount);
        Assert.Equal(new byte[3], sourceArchive);
    }

    [Fact]
    public async Task Cancel_Should_ClearOwnedBuffers_WhenStateWasTransferred()
    {
        // arrange
        using var testDirectory = new TestDirectory();
        var model = CreateModel(
            await CreateAppHostProjectStubsAsync(testDirectory.Path));
        var deployment = Assert.Single(
            await FusionPipeline.SelectStagesAsync(
                model,
                TestContext.Current.CancellationToken));
        using var cancellationSource = new CancellationTokenSource();
        using var session = new FusionPipelineSession(
            cancellationSource.Token);
        var sourceArchive = new byte[] { 1, 2, 3 };
        var fusionArchive = new byte[] { 4, 5, 6 };
        session.SetAll(
            [(deployment, CreateSessionState(sourceArchive, fusionArchive))]);

        // act
        await cancellationSource.CancelAsync();

        // assert
        Assert.Equal(0, session.DeploymentCount);
        Assert.Equal(new byte[3], sourceArchive);
        Assert.Equal(new byte[3], fusionArchive);
        Assert.Throws<OperationCanceledException>(
            () => session.GetState(deployment));
    }

    private static FusionDeploymentSessionState CreateSessionState(
        byte[] sourceArchive,
        byte[]? fusionArchive)
    {
        var state = new FusionDeploymentSessionState(
            "release-1",
            "https://api.chillicream.com",
            "products",
            [
                new FusionSessionSourceIdentity(
                    "products",
                    "release-1",
                    "digest")
            ]);
        state.SetSources(
            [
                new FusionSessionSource(
                    "products",
                    "release-1",
                    sourceArchive,
                    "digest")
            ]);

        if (fusionArchive is not null)
        {
            state.SetComposition(
                "development",
                fusionArchive,
                "far-digest");
        }

        return state;
    }

    private static async Task<FakeNitro> CreateSeededNitroAsync()
    {
        var nitro = new FakeNitro();
        nitro.Seed(
            "products",
            "release-1",
            await CreateSourceArchiveAsync("products", "Product"));
        nitro.Seed(
            "reviews",
            "release-1",
            await CreateSourceArchiveAsync("reviews", "Review"));
        return nitro;
    }

    private static string DescribeDownloads(FakeNitro nitro)
        => string.Join(
            ", ",
            nitro.Downloads
                .GroupBy(source => $"{source.Name}@{source.Version}")
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => $"{group.Key} x{group.Count()}"));

    private static IReadOnlyDictionary<string, string> ReadSourceUrls(
        byte[] fusionArchive)
    {
        using var stream = new MemoryStream(fusionArchive, writable: false);
        using var archive = FusionArchive.Open(stream);
        using var configuration = archive
            .TryGetGatewayConfigurationAsync(
                WellKnownVersions.LatestGatewayFormatVersion,
                TestContext.Current.CancellationToken)
            .GetAwaiter()
            .GetResult()
            ?? throw new InvalidDataException(
                "The composed Fusion archive has no gateway configuration.");

        return configuration.Settings.RootElement
            .GetProperty("sourceSchemas")
            .EnumerateObject()
            .ToDictionary(
                source => source.Name,
                source => source.Value
                    .GetProperty("transports")
                    .GetProperty("http")
                    .GetProperty("url")
                    .GetString()
                    ?? throw new InvalidDataException(
                        "The composed source URL is missing."),
                StringComparer.Ordinal);
    }

    private static DistributedApplicationModel CreateModel(
        (string ProductsProjectPath,
        string ReviewsProjectPath,
        string GatewayProjectPath) projects,
        string stageName = "development")
        => CreateModel(
            projects.ProductsProjectPath,
            projects.ReviewsProjectPath,
            projects.GatewayProjectPath,
            stageName);

    private static DistributedApplicationModel CreateModel(
        string productsProjectPath,
        string reviewsProjectPath,
        string gatewayProjectPath,
        string stageName = "development")
    {
        var builder = DistributedApplication.CreateBuilder();
        var tag = builder.AddParameter("tag", "release-1");
        var stage = builder.AddParameter("stage", stageName);
        var apiKey = builder.AddParameter(
            "nitroApiKey",
            "test-api-key",
            secret: true);
        var products = builder
            .AddProject("products", productsProjectPath)
            .WithGraphQLSchemaFile();
        var reviews = builder
            .AddProject("reviews", reviewsProjectPath)
            .WithGraphQLSchemaFile();
        builder
            .AddProject("gateway", gatewayProjectPath)
            .WithReference(products)
            .WithReference(reviews)
            .WithGraphQLSchemaComposition();
        var nitro = builder
            .AddNitroPublishTarget("nitro")
            .WithNitroCloudUrl("https://api.chillicream.com")
            .WithNitroApiId("products")
            .WithNitroApiKey(apiKey)
            .WithStageParameter(stage)
            .WithConfigurationTag(tag);
        nitro
            .AddStage("development")
            .WithCompositionEnvironment("development");
        nitro
            .AddStage("test")
            .WithCompositionEnvironment("test");

        return new DistributedApplicationModel(builder.Resources);
    }

    private static PipelineStepContext CreateContext(
        DistributedApplicationModel model,
        string environmentName,
        string? outputPath,
        FakeNitro nitro,
        CancellationToken? cancellationToken = null)
    {
        var services = new ServiceCollection()
            .AddSingleton<IHostEnvironment>(
                new TestHostEnvironment(environmentName))
            .AddSingleton(nitro.Workflow)
            .AddSingleton<IConfiguration>(
                new ConfigurationBuilder().Build());
        services.AddSingleton<IPipelineOutputService>(
            outputPath is null
                ? new ThrowingPipelineOutputService()
                : new TestPipelineOutputService(outputPath));
        var schemaCompositionType =
            typeof(GraphQLCompositionSettings).Assembly.GetType(
                "HotChocolate.Fusion.Aspire.SchemaComposition",
                throwOnError: true)!;
        var loggerType = typeof(ILogger<>).MakeGenericType(
            schemaCompositionType);
        var nullLogger = Activator.CreateInstance(
            typeof(NullLogger<>).MakeGenericType(
                schemaCompositionType))!;
        services.AddSingleton(loggerType, nullLogger);
        var serviceProvider = services.BuildServiceProvider();
        var pipelineContext = new PipelineContext(
            model,
            new DistributedApplicationExecutionContext(
                DistributedApplicationOperation.Publish),
            serviceProvider,
            NullLogger.Instance,
            cancellationToken
                ?? TestContext.Current.CancellationToken);

        return new PipelineStepContext
        {
            PipelineContext = pipelineContext,
            ReportingStep = null!
        };
    }

    private static async Task<string> CreateSourceCheckoutAsync(
        string checkout,
        string projectName,
        string sourceName,
        string typeName)
    {
        var sourceDirectory = IOPath.Combine(checkout, projectName);
        Directory.CreateDirectory(sourceDirectory);
        var projectPath = IOPath.Combine(
            sourceDirectory,
            $"{projectName}.csproj");
        await File.WriteAllTextAsync(
            projectPath,
            "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            IOPath.Combine(sourceDirectory, "schema.graphqls"),
            $$"""
            type Query {
              {{sourceName}}: {{typeName}}
            }

            type {{typeName}} {
              id: ID!
            }
            """,
            TestContext.Current.CancellationToken);
        var settings = """
            {
              "name": "SOURCE_NAME",
              "transports": {
                "http": {
                  "url": "{{SOURCE_URL}}/graphql"
                }
              },
              "environments": {
                "development": {
                  "SOURCE_URL": "https://SOURCE_NAME.development.example.com"
                },
                "test": {
                  "SOURCE_URL": "https://SOURCE_NAME.test.example.com"
                }
              }
            }
            """.Replace(
                "SOURCE_NAME",
                sourceName,
                StringComparison.Ordinal);
        await File.WriteAllTextAsync(
            IOPath.Combine(sourceDirectory, "schema-settings.json"),
            settings,
            TestContext.Current.CancellationToken);
        return projectPath;
    }

    private static async Task<byte[]> CreateSourceArchiveAsync(
        string sourceName,
        string typeName)
    {
        await using var stream = new MemoryStream();
        using (var archive = FusionSourceSchemaArchive.Create(
            stream,
            leaveOpen: true))
        {
            await archive.SetArchiveMetadataAsync(
                new HotChocolate.Fusion.SourceSchema.Packaging.ArchiveMetadata(),
                TestContext.Current.CancellationToken);
            await archive.SetSchemaAsync(
                Encoding.UTF8.GetBytes(
                    $"type Query {{ {sourceName}: {typeName} }} "
                    + $"type {typeName} {{ id: ID! }}"),
                TestContext.Current.CancellationToken);
            using var settings = JsonDocument.Parse(
                $$"""
                {
                  "name": "{{sourceName}}",
                  "transports": {
                    "http": {
                      "url": "https://{{sourceName}}.example.com/graphql"
                    }
                  }
                }
                """);
            await archive.SetSettingsAsync(
                settings,
                TestContext.Current.CancellationToken);
            await archive.CommitAsync(
                TestContext.Current.CancellationToken);
        }

        return stream.ToArray();
    }

    private static async Task<(
        string ProductsProjectPath,
        string ReviewsProjectPath,
        string GatewayProjectPath)> CreateAppHostProjectStubsAsync(
            string runnerPath)
    {
        var appHostDirectory = IOPath.Combine(runnerPath, "apphost-model");
        Directory.CreateDirectory(appHostDirectory);
        var productsDirectory = IOPath.Combine(appHostDirectory, "Products");
        var reviewsDirectory = IOPath.Combine(appHostDirectory, "Reviews");
        var gatewayDirectory = IOPath.Combine(appHostDirectory, "Gateway");
        Directory.CreateDirectory(productsDirectory);
        Directory.CreateDirectory(reviewsDirectory);
        Directory.CreateDirectory(gatewayDirectory);
        var productsProjectPath = IOPath.Combine(
            productsDirectory,
            "Products.csproj");
        var reviewsProjectPath = IOPath.Combine(
            reviewsDirectory,
            "Reviews.csproj");
        var gatewayProjectPath = IOPath.Combine(
            gatewayDirectory,
            "Gateway.csproj");
        await File.WriteAllTextAsync(
            productsProjectPath,
            "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            reviewsProjectPath,
            "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            gatewayProjectPath,
            "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            TestContext.Current.CancellationToken);

        Assert.Empty(
            Directory.GetFiles(
                runnerPath,
                "schema.graphqls",
                SearchOption.AllDirectories));
        Assert.Empty(
            Directory.GetFiles(
                runnerPath,
                "schema-settings.json",
                SearchOption.AllDirectories));
        return (
            productsProjectPath,
            reviewsProjectPath,
            gatewayProjectPath);
    }

    private static string NormalizeTree(IReadOnlyList<string> tree)
        => string.Join('\n', tree).Replace('\\', '/');

    private static IReadOnlyList<string> SnapshotTree(string path)
        => Directory.EnumerateFileSystemEntries(
                path,
                "*",
                SearchOption.AllDirectories)
            .Select(entry => IOPath.GetRelativePath(path, entry))
            .Order(StringComparer.Ordinal)
            .ToArray();

    private sealed record RecordedPublication(
        string Stage,
        IReadOnlyList<FusionSourceSchemaVersion> Sources,
        byte[] FusionArchive);

    /// <summary>
    /// An in-memory Nitro API that stores immutable source schema versions and answers the
    /// publication protocol with a successful terminal result.
    /// </summary>
    private sealed class FakeNitro : HttpMessageHandler
    {
        private readonly Dictionary<string, byte[]> _sources =
            new(StringComparer.Ordinal);
        private readonly HttpClient _httpClient;
        private readonly NitroFusionApi _api;
        private (string Name, string Version)? _missingVersion;
        private (string Stage, IReadOnlyList<FusionSourceSchemaVersion> Sources)? _openRequest;
        private TaskCompletionSource? _publishStarted;
        private TaskCompletionSource? _continuePublish;

        public FakeNitro()
        {
            _httpClient = new HttpClient(this, disposeHandler: false)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
            _api = new NitroFusionApi(_httpClient, disposeHttpClient: false);
            Workflow = new FusionDeploymentWorkflow(_api);
        }

        public FusionDeploymentWorkflow Workflow { get; }

        public List<FusionSourceSchemaVersion> Downloads { get; } = [];

        public List<FusionSourceSchemaVersion> Uploads { get; } = [];

        public List<RecordedPublication> Publications { get; } = [];

        public Exception? BeginException { get; set; }

        /// <summary>
        /// Gets or sets the composition settings that the stage declares, as JSON.
        /// </summary>
        public string StageCompositionSettingsJson { get; set; } = "null";

        public void Seed(string name, string version, byte[] archive)
            => _sources[$"{name}@{version}"] = archive;

        public void Remove(string name, string version)
            => _sources.Remove($"{name}@{version}");

        public void BlockNextPublication()
        {
            _publishStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _continuePublish = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task WaitForBlockedPublicationAsync()
            => _publishStarted?.Task
                ?? throw new InvalidOperationException(
                    "No publication is blocked.");

        public void ContinueBlockedPublication()
            => (_continuePublish
                ?? throw new InvalidOperationException(
                    "No publication is blocked."))
                .TrySetResult();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get)
            {
                return Download(request);
            }

            var (operationName, variables, file) =
                await ReadRequestAsync(request, cancellationToken);

            switch (operationName)
            {
                case NitroOperationDocuments.UploadSourceSchemaOperationName:
                    return Upload(variables, file);

                case NitroOperationDocuments.BeginDeploymentOperationName:
                    await BlockAsync(cancellationToken);
                    if (BeginException is not null)
                    {
                        throw BeginException;
                    }

                    return Begin(variables);

                case NitroOperationDocuments.WatchDeploymentOperationName:
                    return Watch();

                case NitroOperationDocuments.ClaimDeploymentOperationName:
                    return Json(CommandResult("startFusionConfigurationComposition"));

                case NitroOperationDocuments.ValidateDeploymentOperationName:
                    AssertFileName(file, "gateway.fgp");
                    return Json(CommandResult("validateFusionConfigurationComposition"));

                case NitroOperationDocuments.CommitDeploymentOperationName:
                    AssertFileName(file, "gateway.far");
                    return Commit(file);

                case NitroOperationDocuments.ReleaseDeploymentOperationName:
                    return Json(CommandResult("cancelFusionConfigurationComposition"));

                case NitroOperationDocuments.GetStageCompositionSettingsOperationName:
                    return Json(StageCompositionSettings(StageCompositionSettingsJson));

                default:
                    throw new InvalidOperationException(
                        $"The Nitro operation '{operationName}' is not scripted.");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _api.Dispose();
                _httpClient.Dispose();
            }

            base.Dispose(disposing);
        }

        private async Task BlockAsync(CancellationToken cancellationToken)
        {
            if (_publishStarted is null || _continuePublish is null)
            {
                return;
            }

            _publishStarted.TrySetResult();
            await _continuePublish.Task;
            cancellationToken.ThrowIfCancellationRequested();
        }

        private HttpResponseMessage Download(HttpRequestMessage request)
        {
            var segments = request.RequestUri!.AbsolutePath.Split('/');
            var name = Uri.UnescapeDataString(segments[6]);
            var version = Uri.UnescapeDataString(segments[8]);
            Downloads.Add(new FusionSourceSchemaVersion(name, version));

            if (!_sources.TryGetValue($"{name}@{version}", out var archive))
            {
                _missingVersion = (name, version);
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(string.Empty)
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(archive.ToArray())
            };
        }

        private HttpResponseMessage Upload(
            JsonElement variables,
            UploadedFile? file)
        {
            AssertFileName(file, "source-schema.zip");
            var version = variables
                .GetProperty("input")
                .GetProperty("tag")
                .GetString()!;
            var name = _missingVersion?.Name
                ?? throw new InvalidOperationException(
                    "Nitro received an upload for a version that was never read back.");
            _sources[$"{name}@{version}"] = file!.Content;
            Uploads.Add(new FusionSourceSchemaVersion(name, version));

            return Json(
                """
                {"data":{"uploadFusionSubgraph":{"fusionSubgraphVersion":{"id":"version-1"},"errors":[]}}}
                """);
        }

        private HttpResponseMessage Begin(JsonElement variables)
        {
            var input = variables.GetProperty("input");
            _openRequest = (
                input.GetProperty("stageName").GetString()!,
                input.GetProperty("subgraphs")
                    .EnumerateArray()
                    .Select(source => new FusionSourceSchemaVersion(
                        source.GetProperty("name").GetString()!,
                        source.GetProperty("tag").GetString()!))
                    .ToArray());

            return Json(
                """
                {"data":{"beginFusionConfigurationPublish":{"requestId":"request-1","errors":[]}}}
                """);
        }

        private HttpResponseMessage Commit(UploadedFile? file)
        {
            var openRequest = _openRequest
                ?? throw new InvalidOperationException(
                    "Nitro received a commit without an open publication request.");
            Publications.Add(
                new RecordedPublication(
                    openRequest.Stage,
                    openRequest.Sources,
                    file!.Content));
            _openRequest = null;

            return Json(CommandResult("commitFusionConfigurationPublish"));
        }

        private static HttpResponseMessage Watch()
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    BuildEventStream(
                        "ProcessingTaskIsReady",
                        "FusionConfigurationValidationSuccess",
                        "FusionConfigurationPublishingSuccess"),
                    Encoding.UTF8,
                    "text/event-stream")
            };

        private static string BuildEventStream(params string[] typeNames)
        {
            var builder = new StringBuilder();

            foreach (var typeName in typeNames)
            {
                builder
                    .Append("event: next\n")
                    .Append("data: ")
                    .Append(
                        "{\"data\":{\"onFusionConfigurationPublishingTaskChanged\":{"
                        + "\"__typename\":\"" + typeName + "\","
                        + "\"state\":\"PROCESSING\",\"errors\":[]}}}")
                    .Append("\n\n");
            }

            return builder.Append("event: complete\n\n").ToString();
        }

        private static void AssertFileName(UploadedFile? file, string expected)
        {
            Assert.NotNull(file);
            Assert.Equal(expected, file.FileName);
        }

        private static string StageCompositionSettings(string settingsJson)
            => "{\"data\":{\"apiById\":{\"stage\":{\"compositionSettings\":"
                + settingsJson
                + "}}}}";

        private static string CommandResult(string fieldName)
            => "{\"data\":{\"" + fieldName + "\":{\"errors\":[]}}}";

        private static HttpResponseMessage Json(string json)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

        private static async Task<(
            string OperationName,
            JsonElement Variables,
            UploadedFile? File)> ReadRequestAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
        {
            string operations;
            UploadedFile? file = null;

            if (request.Content is MultipartFormDataContent multipart)
            {
                operations = string.Empty;
                foreach (var part in multipart)
                {
                    var disposition = part.Headers.ContentDisposition;
                    if (disposition?.FileName is { } fileName)
                    {
                        file = new UploadedFile(
                            fileName.Trim('"'),
                            await part.ReadAsByteArrayAsync(cancellationToken));
                    }
                    else if (disposition?.Name?.Trim('"') is "operations")
                    {
                        operations = await part.ReadAsStringAsync(cancellationToken);
                    }
                }
            }
            else
            {
                operations = await request.Content!.ReadAsStringAsync(
                    cancellationToken);
            }

            using var document = JsonDocument.Parse(operations);
            var operationName = document.RootElement
                .GetProperty("operationName")
                .GetString()!;
            var variables = document.RootElement
                .GetProperty("variables")
                .Clone();

            return (operationName, variables, file);
        }

        private sealed record UploadedFile(string FileName, byte[] Content);
    }

    private sealed class ThrowingPipelineOutputService
        : IPipelineOutputService
    {
        private static InvalidOperationException CreateException()
            => new("Publish-only Fusion steps must not resolve pipeline output directories.");

        public string GetOutputDirectory() => throw CreateException();

        public string GetOutputDirectory(IResource resource)
            => throw CreateException();

        public string GetTempDirectory() => throw CreateException();

        public string GetTempDirectory(IResource resource)
            => throw CreateException();
    }

    private sealed class TestPipelineOutputService(string outputPath)
        : IPipelineOutputService
    {
        public string GetOutputDirectory() => outputPath;

        public string GetOutputDirectory(IResource resource)
            => IOPath.Combine(outputPath, resource.Name);

        public string GetTempDirectory()
            => IOPath.Combine(outputPath, ".temp");

        public string GetTempDirectory(IResource resource)
            => IOPath.Combine(outputPath, ".temp", resource.Name);
    }

    private sealed class TestHostEnvironment(string environmentName)
        : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } =
            nameof(FusionReleaseAcceptanceTests);

        public string ContentRootPath { get; set; } =
            Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Path = IOPath.Combine(
                IOPath.GetTempPath(),
                "chilicream-nitro-aspire-acceptance-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}

#pragma warning restore ASPIREPIPELINES001
#pragma warning restore ASPIREPIPELINES004
