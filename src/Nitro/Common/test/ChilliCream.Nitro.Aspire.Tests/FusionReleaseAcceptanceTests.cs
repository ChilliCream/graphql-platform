using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using ChilliCream.Nitro.Fusion;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Aspire;
using HotChocolate.Fusion.Packaging;
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
    public async Task Release_Should_BuildOnceAndApplyFromOnlyManifestAcrossRunners()
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

        var sourceProjectPath = Path.Combine(
            sourceCheckout,
            "Products.csproj");
        var gatewayProjectPath = Path.Combine(
            sourceCheckout,
            "Gateway.csproj");
        await File.WriteAllTextAsync(
            sourceProjectPath,
            "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            gatewayProjectPath,
            "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(sourceCheckout, "schema.graphqls"),
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              name: String!
            }
            """,
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(sourceCheckout, "schema-settings.json"),
            """
            {
              "name": "products",
              "transports": {
                "http": {
                  "url": "{{PRODUCTS_URL}}/graphql"
                }
              },
              "environments": {
                "staging": {
                  "PRODUCTS_URL": "https://products.staging.example.com"
                },
                "production": {
                  "PRODUCTS_URL": "https://products.example.com"
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        var workflow = new RecordingFusionDeploymentWorkflow();
        var executor = FusionPipelineExecutor.Instance;
        var buildManifestPath = Path.Combine(
            runnerAOutput,
            "fusion",
            "releases",
            "release-1",
            "fusion-release.json");
        var buildModel = CreateModel(
            sourceProjectPath,
            gatewayProjectPath,
            buildManifestPath);
        var buildContext = CreateContext(
            buildModel,
            "Release",
            runnerAOutput,
            workflow);

        await executor.CreateArtifactsAsync(buildContext);
        await executor.UploadAsync(buildContext);

        var reconcile = Assert.Single(workflow.Reconciliations);
        Assert.Equal("products", reconcile.Name);
        Assert.Equal("release-1", reconcile.Version);
        Assert.True(File.Exists(buildManifestPath));
        var manifest = await FusionReleaseStore.ReadFinalAsync(
            buildManifestPath,
            TestContext.Current.CancellationToken);
        Assert.Equal(
            FusionReleaseCompatibility.CompositionToolVersion,
            manifest.CompositionToolVersion);

        var runnerBManifestPath = CopyOnlyFinalManifest(
            buildManifestPath,
            runnerB);
        var runnerCManifestPath = CopyOnlyFinalManifest(
            buildManifestPath,
            runnerC);
        var runnerBProjects = await CreateAppHostProjectStubsAsync(runnerB);
        var runnerCProjects = await CreateAppHostProjectStubsAsync(runnerC);
        Directory.Delete(sourceCheckout, recursive: true);
        Assert.False(Directory.Exists(sourceCheckout));

        var stagingContext = CreateContext(
            CreateModel(
                runnerBProjects.SourceProjectPath,
                runnerBProjects.GatewayProjectPath,
                runnerBManifestPath),
            "Staging",
            Path.Combine(runnerB, "apply-output"),
            workflow);
        await executor.PrepareReleaseAsync(stagingContext);
        await executor.ComposeReleaseAsync(stagingContext);
        await executor.PublishAsync(stagingContext);

        var productionContext = CreateContext(
            CreateModel(
                runnerCProjects.SourceProjectPath,
                runnerCProjects.GatewayProjectPath,
                runnerCManifestPath),
            "Production",
            Path.Combine(runnerC, "different-apply-output"),
            workflow);
        await executor.PrepareReleaseAsync(productionContext);
        await executor.ComposeReleaseAsync(productionContext);
        await executor.PublishAsync(productionContext);

        Assert.Single(workflow.Reconciliations);
        Assert.Equal(2, workflow.Downloads.Count);
        Assert.Collection(
            workflow.Publications.OrderBy(
                publication => publication.Stage,
                StringComparer.Ordinal),
            production =>
            {
                Assert.Equal("production", production.Stage);
                Assert.Equal(
                    "https://products.example.com/graphql",
                    production.SourceUrl);
                Assert.Equal(
                    [new FusionSourceSchemaVersion("products", "release-1")],
                    production.Sources);
            },
            staging =>
            {
                Assert.Equal("staging", staging.Stage);
                Assert.Equal(
                    "https://products.staging.example.com/graphql",
                    staging.SourceUrl);
                Assert.Equal(
                    [new FusionSourceSchemaVersion("products", "release-1")],
                    staging.Sources);
            });
    }

    private static DistributedApplicationModel CreateModel(
        string sourceProjectPath,
        string gatewayProjectPath,
        string manifestPath)
    {
        var builder = DistributedApplication.CreateBuilder();
        var manifest = builder.AddParameter(
            "fusionReleaseManifest",
            manifestPath);
        var apiKey = builder.AddParameter(
            "nitroApiKey",
            "test-api-key",
            secret: true);
        var products = builder
            .AddProject("products", sourceProjectPath)
            .WithGraphQLSchemaFile();
        builder
            .AddProject("gateway", gatewayProjectPath)
            .WithReference(products)
            .WithGraphQLSchemaComposition();
        var nitro = builder
            .AddNitroTarget("nitro")
            .WithCloudUrl("https://api.chillicream.com")
            .WithApiId("products")
            .WithApiKey(apiKey);
        nitro
            .AddFusionDeployment("staging")
            .ForEnvironment("Staging")
            .ToStage("staging")
            .WithCompositionEnvironment("staging")
            .WithConfigurationTag("release-1")
            .WithFusionReleaseManifest(manifest);
        nitro
            .AddFusionDeployment("production")
            .ForEnvironment("Production")
            .ToStage("production")
            .WithCompositionEnvironment("production")
            .WithConfigurationTag("release-1")
            .WithFusionReleaseManifest(manifest);

        return new DistributedApplicationModel(builder.Resources);
    }

    private static PipelineStepContext CreateContext(
        DistributedApplicationModel model,
        string environmentName,
        string outputPath,
        IFusionDeploymentWorkflow workflow)
    {
        var services = new ServiceCollection()
            .AddSingleton<IHostEnvironment>(
                new TestHostEnvironment(environmentName))
            .AddSingleton<IPipelineOutputService>(
                new TestPipelineOutputService(outputPath))
            .AddSingleton(workflow)
            .AddSingleton<IConfiguration>(
                new ConfigurationBuilder().Build());
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
            TestContext.Current.CancellationToken);

        return new PipelineStepContext
        {
            PipelineContext = pipelineContext,
            ReportingStep = null!
        };
    }

    private static string CopyOnlyFinalManifest(
        string sourceManifestPath,
        string runnerPath)
    {
        var promotedDirectory = Path.Combine(runnerPath, "promoted");
        Directory.CreateDirectory(promotedDirectory);
        var destination = Path.Combine(
            promotedDirectory,
            "fusion-release.json");
        File.Copy(sourceManifestPath, destination);

        Assert.Equal(
            [destination],
            Directory.GetFiles(
                promotedDirectory,
                "*",
                SearchOption.AllDirectories));
        return destination;
    }

    private static async Task<(
        string SourceProjectPath,
        string GatewayProjectPath)> CreateAppHostProjectStubsAsync(
            string runnerPath)
    {
        var appHostDirectory = Path.Combine(runnerPath, "apphost-model");
        Directory.CreateDirectory(appHostDirectory);
        var sourceProjectPath = Path.Combine(
            appHostDirectory,
            "Products.csproj");
        var gatewayProjectPath = Path.Combine(
            appHostDirectory,
            "Gateway.csproj");
        await File.WriteAllTextAsync(
            sourceProjectPath,
            "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            gatewayProjectPath,
            "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            TestContext.Current.CancellationToken);

        Assert.False(
            File.Exists(Path.Combine(appHostDirectory, "schema.graphqls")));
        Assert.False(
            File.Exists(
                Path.Combine(
                    appHostDirectory,
                    "schema-settings.json")));
        return (sourceProjectPath, gatewayProjectPath);
    }

    private sealed class RecordingFusionDeploymentWorkflow
        : IFusionDeploymentWorkflow
    {
        private readonly Dictionary<
            (string CloudUrl, string ApiId, string Name, string Version),
            FusionSourceSchemaDownload> _sources = [];

        public List<FusionSourceSchemaUpload> Reconciliations { get; } = [];

        public List<FusionSourceSchemaVersion> Downloads { get; } = [];

        public List<RecordedPublication> Publications { get; } = [];

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
                new FusionSourceSchemaDownload(
                    source.Name,
                    source.Version,
                    archive,
                    contentSha256));
            Reconciliations.Add(source);
        }

        public Task<FusionSourceSchemaDownload?> DownloadSourceSchemaAsync(
            FusionTarget target,
            FusionSourceSchemaVersion source,
            string expectedContentSha256,
            CancellationToken cancellationToken)
        {
            Downloads.Add(source);
            _sources.TryGetValue(
                CreateKey(target, source.Name, source.Version),
                out var download);
            if (download is not null
                && !string.Equals(
                    download.ContentSha256,
                    expectedContentSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "The requested source content digest does not match.");
            }

            return Task.FromResult(download);
        }

        public async Task PublishAsync(
            FusionPublicationRequest request,
            string fusionArchivePath,
            CancellationToken cancellationToken)
        {
            using var archive = FusionArchive.Open(fusionArchivePath);
            using var configuration =
                await archive.TryGetGatewayConfigurationAsync(
                    WellKnownVersions.LatestGatewayFormatVersion,
                    cancellationToken)
                ?? throw new InvalidDataException(
                    "The composed Fusion archive has no gateway configuration.");
            var sourceUrl = configuration.Settings.RootElement
                .GetProperty("sourceSchemas")
                .GetProperty("products")
                .GetProperty("transports")
                .GetProperty("http")
                .GetProperty("url")
                .GetString()
                ?? throw new InvalidDataException(
                    "The composed source URL is missing.");
            Publications.Add(
                new RecordedPublication(
                    request.Stage,
                    request.SourceSchemas.ToArray(),
                    sourceUrl));
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
    }

    private sealed record RecordedPublication(
        string Stage,
        IReadOnlyList<FusionSourceSchemaVersion> Sources,
        string SourceUrl);

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
