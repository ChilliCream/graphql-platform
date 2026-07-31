using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using ChilliCream.Nitro.Fusion;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Aspire;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Fusion.SourceSchema.Packaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChilliCream.Nitro.Aspire;

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES004

public sealed class FusionReleaseAcceptanceTests
{
    [Fact]
    public async Task Release_Should_UploadOnceAndPublishFromNitroAcrossRunners()
    {
        using var testDirectory = new TestDirectory();
        var sourceCheckout = Path.Combine(
            testDirectory.Path,
            "runner-a-checkout");
        var runnerAOutput = Path.Combine(
            testDirectory.Path,
            "runner-a-output");
        var runnerB = Path.Combine(testDirectory.Path, "unrelated-runner-b");
        var runnerC = Path.Combine(testDirectory.Path, "unrelated-runner-c");
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
        var gatewayDirectory = Path.Combine(sourceCheckout, "Gateway");
        Directory.CreateDirectory(gatewayDirectory);
        var gatewayProjectPath = Path.Combine(
            gatewayDirectory,
            "Gateway.csproj");
        await File.WriteAllTextAsync(
            gatewayProjectPath,
            "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            TestContext.Current.CancellationToken);

        var workflow = new RecordingFusionDeploymentWorkflow();
        var executor = FusionPipelineExecutor.Instance;
        var buildModel = CreateModel(
            productsProjectPath,
            reviewsProjectPath,
            gatewayProjectPath);
        var buildContext = CreateContext(
            buildModel,
            "Development",
            runnerAOutput,
            workflow);

        await executor.CreateArtifactsAsync(buildContext);
        await executor.UploadAsync(buildContext);

        Assert.Collection(
            workflow.Reconciliations.OrderBy(source => source.Name),
            products =>
            {
                Assert.Equal("products", products.Name);
                Assert.Equal("release-1", products.Version);
            },
            reviews =>
            {
                Assert.Equal("reviews", reviews.Name);
                Assert.Equal("release-1", reviews.Version);
            });

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
            workflow: workflow);
        using var stagingSession = new FusionPipelineSession(
            stagingContext.CancellationToken);
        await executor.PreflightAsync(stagingContext, stagingSession);

        var productionContext = CreateContext(
            CreateModel(
                runnerCProjects.ProductsProjectPath,
                runnerCProjects.ReviewsProjectPath,
                runnerCProjects.GatewayProjectPath),
            "Test",
            outputPath: null,
            workflow: workflow);
        using var productionSession = new FusionPipelineSession(
            productionContext.CancellationToken);
        await executor.PreflightAsync(productionContext, productionSession);

        Assert.Equal(1, stagingSession.DeploymentCount);
        Assert.Equal(1, productionSession.DeploymentCount);
        var stagingDeployment = Assert.Single(
            FusionPipeline.SelectDeployments(
                stagingModel,
                "Development"));
        using (var lease = stagingSession.Acquire(stagingDeployment))
        {
            Assert.Equal(0, lease.State.SourceArchiveBytes);
            Assert.Throws<InvalidOperationException>(
                () => lease.State.Sources);
        }

        await executor.DownloadAsync(stagingContext, stagingSession);
        await executor.ComposeAsync(stagingContext, stagingSession);
        await executor.DownloadAsync(productionContext, productionSession);
        await executor.ComposeAsync(productionContext, productionSession);
        await executor.PublishAsync(stagingContext, stagingSession);

        Assert.Equal(0, stagingSession.DeploymentCount);
        Assert.Equal(1, productionSession.DeploymentCount);

        await executor.PublishAsync(productionContext, productionSession);

        Assert.Equal(0, productionSession.DeploymentCount);

        Assert.Equal(2, workflow.Reconciliations.Count);
        Assert.Equal(
            [
                new FusionSourceSchemaVersion("products", "release-1"),
                new FusionSourceSchemaVersion("products", "release-1"),
                new FusionSourceSchemaVersion("products", "release-1"),
                new FusionSourceSchemaVersion("products", "release-1"),
                new FusionSourceSchemaVersion("reviews", "release-1"),
                new FusionSourceSchemaVersion("reviews", "release-1"),
                new FusionSourceSchemaVersion("reviews", "release-1"),
                new FusionSourceSchemaVersion("reviews", "release-1")
            ],
            workflow.Downloads.OrderBy(source => source.Name));
        Assert.Collection(
            workflow.Publications.OrderBy(
                publication => publication.Stage,
                StringComparer.Ordinal),
            development =>
            {
                Assert.Equal("development", development.Stage);
                Assert.Equal(
                    new Dictionary<string, string>
                    {
                        ["products"] = "https://products.development.example.com/graphql",
                        ["reviews"] = "https://reviews.development.example.com/graphql"
                    },
                    development.SourceUrls);
                Assert.Equal(
                    [
                        new FusionSourceSchemaVersion("products", "release-1"),
                        new FusionSourceSchemaVersion("reviews", "release-1")
                    ],
                    development.Sources);
                Assert.NotEmpty(development.FusionArchive);
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
                    test.SourceUrls);
                Assert.Equal(
                    [
                        new FusionSourceSchemaVersion("products", "release-1"),
                        new FusionSourceSchemaVersion("reviews", "release-1")
                    ],
                    test.Sources);
                Assert.NotEmpty(test.FusionArchive);
            });
        Assert.NotEqual(
            Convert.ToBase64String(workflow.Publications[0].FusionArchive),
            Convert.ToBase64String(workflow.Publications[1].FusionArchive));
        Assert.Empty(
            Directory.GetFiles(
                runnerB,
                "fusion-apply.json",
                SearchOption.AllDirectories));
        Assert.Empty(
            Directory.GetFiles(
                runnerC,
                "fusion-apply.json",
                SearchOption.AllDirectories));
        Assert.Equal(runnerBTree, SnapshotTree(runnerB));
        Assert.Equal(runnerCTree, SnapshotTree(runnerC));

        using var repeatedStagingSession = new FusionPipelineSession(
            stagingContext.CancellationToken);
        await executor.PreflightAsync(stagingContext, repeatedStagingSession);
        await executor.DownloadAsync(stagingContext, repeatedStagingSession);
        await executor.ComposeAsync(stagingContext, repeatedStagingSession);
        await executor.PublishAsync(stagingContext, repeatedStagingSession);

        Assert.Equal(0, repeatedStagingSession.DeploymentCount);
        Assert.Equal(3, workflow.Publications.Count);
        Assert.Equal(
            Convert.ToBase64String(
                workflow.Publications[0].FusionArchive),
            Convert.ToBase64String(
                workflow.Publications[2].FusionArchive));
        Assert.Equal(runnerBTree, SnapshotTree(runnerB));

        using var failingSession = new FusionPipelineSession(
            stagingContext.CancellationToken);
        await executor.PreflightAsync(stagingContext, failingSession);
        await executor.DownloadAsync(stagingContext, failingSession);
        await executor.ComposeAsync(stagingContext, failingSession);
        byte[][] sourceBuffers;
        byte[] farBuffer;
        using (var lease = failingSession.Acquire(stagingDeployment))
        {
            sourceBuffers = lease.State.Sources
                .Select(source => source.Archive)
                .ToArray();
            farBuffer = lease.State.FusionArchive;
        }
        workflow.ThrowOnPublish = true;

        var publishException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => executor.PublishAsync(stagingContext, failingSession));

        Assert.Equal("Injected publication failure.", publishException.Message);
        Assert.Equal(0, failingSession.DeploymentCount);
        foreach (var sourceBuffer in sourceBuffers)
        {
            Assert.Equal(new byte[sourceBuffer.Length], sourceBuffer);
        }

        Assert.Equal(new byte[farBuffer.Length], farBuffer);
        Assert.Equal(runnerBTree, SnapshotTree(runnerB));

        workflow.ThrowOnPublish = false;
        using var publishCancellation = new CancellationTokenSource();
        var cancelingContext = CreateContext(
            stagingModel,
            "Development",
            outputPath: null,
            workflow: workflow,
            cancellationToken: publishCancellation.Token);
        using var cancelingSession = new FusionPipelineSession(
            publishCancellation.Token);
        await executor.PreflightAsync(cancelingContext, cancelingSession);
        await executor.DownloadAsync(cancelingContext, cancelingSession);
        await executor.ComposeAsync(cancelingContext, cancelingSession);
        byte[][] cancelingSourceBuffers;
        byte[] cancelingFarBuffer;
        using (var lease = cancelingSession.Acquire(stagingDeployment))
        {
            cancelingSourceBuffers = lease.State.Sources
                .Select(source => source.Archive)
                .ToArray();
            cancelingFarBuffer = lease.State.FusionArchive;
        }

        workflow.BlockNextPublication();
        var cancelingPublish = executor.PublishAsync(
            cancelingContext,
            cancelingSession);
        await workflow.WaitForBlockedPublicationAsync();

        publishCancellation.Cancel();

        Assert.All(
            cancelingSourceBuffers,
            buffer => Assert.Contains(buffer, value => value != 0));
        Assert.Contains(cancelingFarBuffer, value => value != 0);

        workflow.ContinueBlockedPublication();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cancelingPublish);

        Assert.True(workflow.BlockedPublicationObservedStableBytes);
        Assert.Equal(0, cancelingSession.DeploymentCount);
        Assert.All(
            cancelingSourceBuffers,
            buffer => Assert.Equal(new byte[buffer.Length], buffer));
        Assert.Equal(
            new byte[cancelingFarBuffer.Length],
            cancelingFarBuffer);
    }

    [Fact]
    public async Task Download_Should_ClearSession_WhenExactSourceIsMissing()
    {
        using var testDirectory = new TestDirectory();
        var projects = await CreateAppHostProjectStubsAsync(
            testDirectory.Path);
        var workflow = new RecordingFusionDeploymentWorkflow();
        var context = CreateContext(
            CreateModel(
                projects.ProductsProjectPath,
                projects.ReviewsProjectPath,
                projects.GatewayProjectPath),
            "Development",
            outputPath: null,
            workflow: workflow);
        using var session = new FusionPipelineSession(
            context.CancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => FusionPipelineExecutor.Instance.PreflightAsync(
                context,
                session));

        Assert.Equal(
            "Fusion source 'products' version 'release-1' does not exist on target 'products'.",
            exception.Message);
        Assert.Equal(0, session.DeploymentCount);
        Assert.Empty(
            Directory.GetFiles(
                testDirectory.Path,
                "fusion-apply.json",
                SearchOption.AllDirectories));
        Assert.Empty(workflow.Reconciliations);
        Assert.Empty(workflow.Publications);
    }

    [Fact]
    public async Task Download_Should_RejectSourceArchive_WhenPerSourceLimitIsExceeded()
    {
        using var testDirectory = new TestDirectory();
        var projects = await CreateAppHostProjectStubsAsync(
            testDirectory.Path);
        var workflow = new RecordingFusionDeploymentWorkflow();
        workflow.SeedSource(
            "products",
            "release-1",
            new byte[] { 1, 2, 3 });
        var context = CreateContext(
            CreateModel(
                projects.ProductsProjectPath,
                projects.ReviewsProjectPath,
                projects.GatewayProjectPath),
            "Development",
            outputPath: null,
            workflow: workflow);
        var limits = new FusionPipelineMemoryLimits(
            SourceArchiveBytes: 2,
            TotalSourceArchiveBytes: 100,
            FusionArchiveBytes: 100);
        var executor = new FusionPipelineExecutor(limits);
        using var session = new FusionPipelineSession(
            context.CancellationToken,
            limits);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => executor.PreflightAsync(context, session));

        Assert.Equal(
            "Downloaded Fusion source 'products@release-1' exceeds the "
            + "2-byte per-source in-memory size limit.",
            exception.Message);
        Assert.Equal(0, session.DeploymentCount);
    }

    [Fact]
    public async Task Download_Should_RejectSourceArchives_WhenAggregateLimitIsExceeded()
    {
        using var testDirectory = new TestDirectory();
        var projects = await CreateAppHostProjectStubsAsync(
            testDirectory.Path);
        var workflow = new RecordingFusionDeploymentWorkflow();
        workflow.SeedSource(
            "products",
            "release-1",
            new byte[] { 1, 2, 3 });
        var context = CreateContext(
            CreateModel(
                projects.ProductsProjectPath,
                projects.ReviewsProjectPath,
                projects.GatewayProjectPath),
            "Development",
            outputPath: null,
            workflow: workflow);
        var limits = new FusionPipelineMemoryLimits(
            SourceArchiveBytes: 100,
            TotalSourceArchiveBytes: 2,
            FusionArchiveBytes: 100);
        var executor = new FusionPipelineExecutor(limits);
        using var session = new FusionPipelineSession(
            context.CancellationToken,
            limits);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => executor.PreflightAsync(context, session));

        Assert.Equal(
            "The downloaded Fusion sources exceed the 2-byte aggregate "
            + "in-memory size limit.",
            exception.Message);
        Assert.Equal(0, session.DeploymentCount);
    }

    [Fact]
    public async Task Download_Should_FailBeforeComposition_WhenExactSourceChangesAfterPreflight()
    {
        using var testDirectory = new TestDirectory();
        var projects = await CreateAppHostProjectStubsAsync(
            testDirectory.Path);
        var workflow = new RecordingFusionDeploymentWorkflow();
        workflow.SeedSource(
            await CreateSourceDownloadAsync(
                "products",
                "Product"));
        workflow.SeedSource(
            await CreateSourceDownloadAsync(
                "reviews",
                "Review"));
        var context = CreateContext(
            CreateModel(
                projects.ProductsProjectPath,
                projects.ReviewsProjectPath,
                projects.GatewayProjectPath),
            "Development",
            outputPath: null,
            workflow: workflow);
        using var session = new FusionPipelineSession(
            context.CancellationToken);
        var executor = FusionPipelineExecutor.Instance;
        await executor.PreflightAsync(context, session);
        workflow.OverrideSource(
            await CreateSourceDownloadAsync(
                "products",
                "ChangedProduct"));

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => executor.DownloadAsync(context, session));

        Assert.Equal(
            "Fusion source 'products@release-1' changed between preflight and composition.",
            exception.Message);
        Assert.Equal(0, session.DeploymentCount);
        Assert.Empty(workflow.Publications);
    }

    [Fact]
    public async Task Download_Should_ClearPartialSources_WhenLaterSourceIsMissing()
    {
        using var testDirectory = new TestDirectory();
        var projects = await CreateAppHostProjectStubsAsync(
            testDirectory.Path);
        var workflow = new RecordingFusionDeploymentWorkflow();
        workflow.SeedSource(
            await CreateSourceDownloadAsync(
                "products",
                "Product"));
        workflow.SeedSource(
            await CreateSourceDownloadAsync(
                "reviews",
                "Review"));
        var context = CreateContext(
            CreateModel(
                projects.ProductsProjectPath,
                projects.ReviewsProjectPath,
                projects.GatewayProjectPath),
            "Development",
            outputPath: null,
            workflow: workflow);
        using var session = new FusionPipelineSession(
            context.CancellationToken);
        await FusionPipelineExecutor.Instance.PreflightAsync(
            context,
            session);
        workflow.RemoveSource("reviews", "release-1");
        var clearedBuffers = new List<byte[]>();
        var executor = new FusionPipelineExecutor(
            bufferCleared: clearedBuffers.Add);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => executor.DownloadAsync(context, session));

        Assert.Equal(
            "Fusion source 'reviews' version 'release-1' does not exist on target 'products'.",
            exception.Message);
        var clearedBuffer = Assert.Single(clearedBuffers);
        Assert.Equal(new byte[clearedBuffer.Length], clearedBuffer);
        Assert.Equal(0, session.DeploymentCount);
    }

    [Fact]
    public async Task Download_Should_ClearCurrentSource_WhenCanonicalDigestIsInvalid()
    {
        using var testDirectory = new TestDirectory();
        var projects = await CreateAppHostProjectStubsAsync(
            testDirectory.Path);
        var workflow = new RecordingFusionDeploymentWorkflow();
        var products = await CreateSourceDownloadAsync(
            "products",
            "Product");
        var invalidProducts = new FusionSourceSchemaDownload(
            products.Name,
            products.Version,
            products.Archive.ToArray(),
            new string('0', 64));
        workflow.SeedSource(products);
        workflow.SeedSource(
            await CreateSourceDownloadAsync(
                "reviews",
                "Review"));
        var context = CreateContext(
            CreateModel(
                projects.ProductsProjectPath,
                projects.ReviewsProjectPath,
                projects.GatewayProjectPath),
            "Development",
            outputPath: null,
            workflow: workflow);
        using var session = new FusionPipelineSession(
            context.CancellationToken);
        await FusionPipelineExecutor.Instance.PreflightAsync(
            context,
            session);
        workflow.OverrideSource(
            invalidProducts);
        var clearedBuffers = new List<byte[]>();
        var executor = new FusionPipelineExecutor(
            bufferCleared: clearedBuffers.Add);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => executor.DownloadAsync(context, session));

        Assert.Equal(
            "Downloaded Fusion source 'products@release-1' content does not match its "
            + "canonical digest.",
            exception.Message);
        var clearedBuffer = Assert.Single(clearedBuffers);
        Assert.Equal(new byte[clearedBuffer.Length], clearedBuffer);
        Assert.Equal(0, session.DeploymentCount);
    }

    [Fact]
    public async Task Session_Should_RejectAndClearState_WhenCancellationPrecedesTransfer()
    {
        using var testDirectory = new TestDirectory();
        var projects = await CreateAppHostProjectStubsAsync(
            testDirectory.Path);
        var model = CreateModel(
            projects.ProductsProjectPath,
            projects.ReviewsProjectPath,
            projects.GatewayProjectPath);
        var deployment = Assert.Single(
            FusionPipeline.SelectDeployments(model, "Development"));
        using var cancellationSource = new CancellationTokenSource();
        using var session = new FusionPipelineSession(
            cancellationSource.Token);
        var sourceArchive = new byte[] { 1, 2, 3 };
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

        cancellationSource.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => session.SetAll([(deployment, state)]));
        Assert.Equal(0, session.DeploymentCount);
        Assert.Equal(new byte[3], sourceArchive);
    }

    [Fact]
    public async Task Session_Should_ClearOwnedBuffers_WhenCanceledAfterTransfer()
    {
        using var testDirectory = new TestDirectory();
        var projects = await CreateAppHostProjectStubsAsync(
            testDirectory.Path);
        var model = CreateModel(
            projects.ProductsProjectPath,
            projects.ReviewsProjectPath,
            projects.GatewayProjectPath);
        var deployment = Assert.Single(
            FusionPipeline.SelectDeployments(model, "Development"));
        using var cancellationSource = new CancellationTokenSource();
        using var session = new FusionPipelineSession(
            cancellationSource.Token);
        var sourceArchive = new byte[] { 1, 2, 3 };
        var fusionArchive = new byte[] { 4, 5, 6 };
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
        state.SetComposition(
            "development",
            fusionArchive,
            "far-digest");
        session.SetAll([(deployment, state)]);

        var lease = session.Acquire(deployment);

        cancellationSource.Cancel();

        Assert.Equal(1, session.DeploymentCount);
        Assert.Equal(new byte[] { 1, 2, 3 }, sourceArchive);
        Assert.Equal(new byte[] { 4, 5, 6 }, fusionArchive);

        lease.Dispose();

        Assert.Equal(0, session.DeploymentCount);
        Assert.Equal(new byte[3], sourceArchive);
        Assert.Equal(new byte[3], fusionArchive);
    }

    private static DistributedApplicationModel CreateModel(
        string productsProjectPath,
        string reviewsProjectPath,
        string gatewayProjectPath)
    {
        var builder = DistributedApplication.CreateBuilder();
        var tag = builder.AddParameter("tag", "release-1");
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
            .AddNitroTarget("nitro")
            .WithCloudUrl("https://api.chillicream.com")
            .WithApiId("products")
            .WithApiKey(apiKey);
        nitro
            .AddFusionDeployment("development")
            .ForEnvironment("Development")
            .ToStage("development")
            .WithCompositionEnvironment("development")
            .WithConfigurationTag(tag);
        nitro
            .AddFusionDeployment("test")
            .ForEnvironment("Test")
            .ToStage("test")
            .WithCompositionEnvironment("test")
            .WithConfigurationTag(tag);

        return new DistributedApplicationModel(builder.Resources);
    }

    private static PipelineStepContext CreateContext(
        DistributedApplicationModel model,
        string environmentName,
        string? outputPath,
        IFusionDeploymentWorkflow workflow,
        CancellationToken? cancellationToken = null)
    {
        var services = new ServiceCollection()
            .AddSingleton<IHostEnvironment>(
                new TestHostEnvironment(environmentName))
            .AddSingleton(workflow)
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
        var sourceDirectory = Path.Combine(checkout, projectName);
        Directory.CreateDirectory(sourceDirectory);
        var projectPath = Path.Combine(
            sourceDirectory,
            $"{projectName}.csproj");
        await File.WriteAllTextAsync(
            projectPath,
            "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(sourceDirectory, "schema.graphqls"),
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
            Path.Combine(sourceDirectory, "schema-settings.json"),
            settings,
            TestContext.Current.CancellationToken);
        return projectPath;
    }

    private static async Task<FusionSourceSchemaDownload>
        CreateSourceDownloadAsync(
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
                System.Text.Encoding.UTF8.GetBytes(
                    $"type Query {{ value: {typeName} }} type {typeName} {{ id: ID! }}"),
                TestContext.Current.CancellationToken);
            using var settings = System.Text.Json.JsonDocument.Parse(
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

        var archiveBytes = stream.ToArray();
        var digest = await FusionSourceSchemaContent.ComputeSha256Async(
            archiveBytes,
            sourceName,
            TestContext.Current.CancellationToken);
        return new FusionSourceSchemaDownload(
            sourceName,
            "release-1",
            archiveBytes,
            digest);
    }

    private static async Task<(
        string ProductsProjectPath,
        string ReviewsProjectPath,
        string GatewayProjectPath)> CreateAppHostProjectStubsAsync(
            string runnerPath)
    {
        var appHostDirectory = Path.Combine(runnerPath, "apphost-model");
        Directory.CreateDirectory(appHostDirectory);
        var productsDirectory = Path.Combine(appHostDirectory, "Products");
        var reviewsDirectory = Path.Combine(appHostDirectory, "Reviews");
        var gatewayDirectory = Path.Combine(appHostDirectory, "Gateway");
        Directory.CreateDirectory(productsDirectory);
        Directory.CreateDirectory(reviewsDirectory);
        Directory.CreateDirectory(gatewayDirectory);
        var productsProjectPath = Path.Combine(
            productsDirectory,
            "Products.csproj");
        var reviewsProjectPath = Path.Combine(
            reviewsDirectory,
            "Reviews.csproj");
        var gatewayProjectPath = Path.Combine(
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
        Assert.Empty(
            Directory.GetFiles(
                runnerPath,
                "*manifest*",
                SearchOption.AllDirectories));
        Assert.Empty(
            Directory.GetDirectories(
                runnerPath,
                ".git",
                SearchOption.AllDirectories));
        return (
            productsProjectPath,
            reviewsProjectPath,
            gatewayProjectPath);
    }

    private static IReadOnlyList<string> SnapshotTree(string path)
        => Directory.EnumerateFileSystemEntries(
                path,
                "*",
                SearchOption.AllDirectories)
            .Select(entry => Path.GetRelativePath(path, entry))
            .Order(StringComparer.Ordinal)
            .ToArray();

    private sealed class RecordingFusionDeploymentWorkflow
        : IFusionDeploymentWorkflow
    {
        private readonly Dictionary<
            (string CloudUrl, string ApiId, string Name, string Version),
            StoredSourceDownload> _sources = [];
        private TaskCompletionSource? _publishStarted;
        private TaskCompletionSource? _continuePublish;

        public List<FusionSourceSchemaUpload> Reconciliations { get; } = [];

        public List<FusionSourceSchemaVersion> Downloads { get; } = [];

        public List<RecordedPublication> Publications { get; } = [];

        public bool ThrowOnPublish { get; set; }

        public bool BlockedPublicationObservedStableBytes { get; private set; }

        public void BlockNextPublication()
        {
            _publishStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _continuePublish = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            BlockedPublicationObservedStableBytes = false;
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

        public void SeedSource(
            string name,
            string version,
            byte[] archive)
        {
            var target = new FusionTarget(
                new Uri("https://api.chillicream.com"),
                "products",
                "test-api-key");
            _sources.Add(
                CreateKey(target, name, version),
                new StoredSourceDownload(
                    name,
                    version,
                    archive,
                    "unused-before-bound-validation"));
        }

        public void SeedSource(FusionSourceSchemaDownload source)
        {
            var target = CreateTestTarget();
            _sources.Add(
                CreateKey(target, source.Name, source.Version),
                TakeOwnership(source));
        }

        public void OverrideSource(FusionSourceSchemaDownload source)
        {
            var target = CreateTestTarget();
            var key = CreateKey(target, source.Name, source.Version);
            var replacement = TakeOwnership(source);
            if (_sources.TryGetValue(key, out var previous))
            {
                Array.Clear(previous.Archive);
            }

            _sources[key] = replacement;
        }

        public void RemoveSource(string name, string version)
        {
            var target = CreateTestTarget();
            if (_sources.Remove(
                    CreateKey(target, name, version),
                    out var removed))
            {
                Array.Clear(removed.Archive);
            }
        }

        public async Task ReconcileSourceSchemaAsync(
            FusionTarget target,
            FusionSourceSchemaUpload source,
            CancellationToken cancellationToken)
        {
            var archive = await File.ReadAllBytesAsync(
                source.ArchivePath,
                cancellationToken);
            var contentSha256 =
                await FusionSourceSchemaContent.ComputeSha256Async(
                    source.ArchivePath,
                    source.Name,
                    cancellationToken);
            _sources.Add(
                CreateKey(target, source.Name, source.Version),
                new StoredSourceDownload(
                    source.Name,
                    source.Version,
                    archive,
                    contentSha256));
            Reconciliations.Add(source);
        }

        public Task<FusionSourceSchemaDownload?> DownloadSourceSchemaAsync(
            FusionTarget target,
            FusionSourceSchemaVersion source,
            CancellationToken cancellationToken)
        {
            Downloads.Add(source);
            _sources.TryGetValue(
                CreateKey(target, source.Name, source.Version),
                out var download);
            return Task.FromResult(
                download is null
                    ? null
                    : new FusionSourceSchemaDownload(
                        download.Name,
                        download.Version,
                        download.Archive.ToArray(),
                        download.ContentSha256));
        }

        private static StoredSourceDownload TakeOwnership(
            FusionSourceSchemaDownload source)
        {
            using (source)
            {
                return new StoredSourceDownload(
                    source.Name,
                    source.Version,
                    source.Archive.ToArray(),
                    source.ContentSha256);
            }
        }

        public async Task PublishAsync(
            FusionPublicationRequest request,
            ReadOnlyMemory<byte> fusionArchive,
            CancellationToken cancellationToken)
        {
            if (ThrowOnPublish)
            {
                throw new InvalidOperationException(
                    "Injected publication failure.");
            }

            if (_publishStarted is not null
                && _continuePublish is not null)
            {
                var beforeCancellation = fusionArchive.ToArray();
                _publishStarted.TrySetResult();
                await _continuePublish.Task;
                BlockedPublicationObservedStableBytes =
                    fusionArchive.Span.SequenceEqual(beforeCancellation);
                cancellationToken.ThrowIfCancellationRequested();
            }

            var fusionArchiveBytes = fusionArchive.ToArray();
            await using var fusionArchiveStream = new MemoryStream(
                fusionArchiveBytes,
                writable: false);
            using var archive = FusionArchive.Open(fusionArchiveStream);
            using var configuration =
                await archive.TryGetGatewayConfigurationAsync(
                    WellKnownVersions.LatestGatewayFormatVersion,
                    cancellationToken)
                ?? throw new InvalidDataException(
                    "The composed Fusion archive has no gateway configuration.");
            var sourceUrls = configuration.Settings.RootElement
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
            Publications.Add(
                new RecordedPublication(
                    request.Stage,
                    request.SourceSchemas.ToArray(),
                    sourceUrls,
                    fusionArchiveBytes));
        }

        private static (
            string CloudUrl,
            string ApiId,
            string Name,
            string Version) CreateKey(
                FusionTarget target,
                string name,
                string version)
            => (
                target.CloudUrl.AbsoluteUri.TrimEnd('/'),
                target.ApiId,
                name,
                version);

        private static FusionTarget CreateTestTarget()
            => new(
                new Uri("https://api.chillicream.com"),
                "products",
                "test-api-key");

        private sealed record StoredSourceDownload(
            string Name,
            string Version,
            byte[] Archive,
            string ContentSha256);
    }

    private sealed record RecordedPublication(
        string Stage,
        IReadOnlyList<FusionSourceSchemaVersion> Sources,
        IReadOnlyDictionary<string, string> SourceUrls,
        byte[] FusionArchive);

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
            => Path.Combine(outputPath, resource.Name);

        public string GetTempDirectory()
            => Path.Combine(outputPath, ".temp");

        public string GetTempDirectory(IResource resource)
            => Path.Combine(outputPath, ".temp", resource.Name);
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
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
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
