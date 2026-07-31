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
        Directory.Delete(sourceCheckout, recursive: true);
        Assert.False(Directory.Exists(sourceCheckout));

        var stagingContext = CreateContext(
            CreateModel(
                runnerBProjects.ProductsProjectPath,
                runnerBProjects.ReviewsProjectPath,
                runnerBProjects.GatewayProjectPath),
            "Development",
            Path.Combine(runnerB, "apply-output"),
            workflow);
        await executor.DownloadAsync(stagingContext);
        await executor.ComposeAsync(stagingContext);
        await executor.PublishAsync(stagingContext);

        var productionContext = CreateContext(
            CreateModel(
                runnerCProjects.ProductsProjectPath,
                runnerCProjects.ReviewsProjectPath,
                runnerCProjects.GatewayProjectPath),
            "Test",
            Path.Combine(runnerC, "different-apply-output"),
            workflow);
        await executor.DownloadAsync(productionContext);
        await executor.ComposeAsync(productionContext);
        await executor.PublishAsync(productionContext);

        Assert.Equal(2, workflow.Reconciliations.Count);
        Assert.Equal(
            [
                new FusionSourceSchemaVersion("products", "release-1"),
                new FusionSourceSchemaVersion("products", "release-1"),
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
            });
    }

    [Fact]
    public async Task Download_Should_FailWithoutApplyState_WhenExactSourceIsMissing()
    {
        using var testDirectory = new TestDirectory();
        var projects = await CreateAppHostProjectStubsAsync(
            testDirectory.Path);
        var output = Path.Combine(testDirectory.Path, "apply-output");
        var workflow = new RecordingFusionDeploymentWorkflow();
        var context = CreateContext(
            CreateModel(
                projects.ProductsProjectPath,
                projects.ReviewsProjectPath,
                projects.GatewayProjectPath),
            "Development",
            output,
            workflow);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => FusionPipelineExecutor.Instance.DownloadAsync(context));

        Assert.Equal(
            "Fusion source 'products' version 'release-1' does not exist on target 'products'.",
            exception.Message);
        Assert.False(
            File.Exists(
                Path.Combine(
                    output,
                    "fusion",
                    "apply",
                    "development",
                    "fusion-apply.json")));
        Assert.Empty(workflow.Reconciliations);
        Assert.Empty(workflow.Publications);
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
            CancellationToken cancellationToken)
        {
            Downloads.Add(source);
            _sources.TryGetValue(
                CreateKey(target, source.Name, source.Version),
                out var download);
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
                    sourceUrls));
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
        IReadOnlyDictionary<string, string> SourceUrls);

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
