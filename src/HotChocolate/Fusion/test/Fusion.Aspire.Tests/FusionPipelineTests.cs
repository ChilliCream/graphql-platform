using System.Net;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

#pragma warning disable ASPIREPIPELINES001

public sealed class FusionPipelineTests
{
    [Fact]
    public void SelectDeployments_Should_SelectOnlyMatchingEnvironment()
    {
        var builder = DistributedApplication.CreateBuilder();
        var nitro = builder
            .AddNitroPublishTarget("nitro")
            .WithNitroCloudUrl("https://api.chillicream.com")
            .WithNitroApiId("products");
        nitro
            .AddFusionDeployment("production")
            .ForEnvironment("Production")
            .ToStage("production")
            .WithConfigurationTag("release-1");
        nitro
            .AddFusionDeployment("staging")
            .ForEnvironment("Staging")
            .ToStage("staging")
            .WithConfigurationTag("release-1");
        var model = new DistributedApplicationModel(builder.Resources);

        var deployments = FusionPipeline.SelectDeployments(
            model,
            "Production");

        Assert.Equal(["production"], deployments.Select(x => x.Name));
    }

    [Fact]
    public void WithCloudUrl_Should_Fail_WhenUrlContainsCaseSensitivePath()
    {
        var builder = DistributedApplication.CreateBuilder();

        var exception = Assert.Throws<ArgumentException>(
            () => builder
                .AddNitroPublishTarget("nitro")
                .WithNitroCloudUrl(
                    "https://api.chillicream.com/CaseSensitivePath"));

        Assert.Equal(
            "The Nitro cloud URL must be an absolute HTTPS origin without "
            + "a path, query, fragment, or user information. (Parameter 'cloudUrl')",
            exception.Message);
    }

    [Fact]
    public void SelectDeployments_Should_ReturnEmpty_WhenEnvironmentDoesNotMatch()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder
            .AddNitroPublishTarget("nitro")
            .WithNitroCloudUrl("https://api.chillicream.com")
            .WithNitroApiId("products")
            .AddFusionDeployment("production")
            .ForEnvironment("Production")
            .ToStage("production")
            .WithConfigurationTag("release-1");
        var model = new DistributedApplicationModel(builder.Resources);

        var deployments = FusionPipeline.SelectDeployments(
            model,
            "Development");

        Assert.Empty(deployments);
    }

    [Fact]
    public void GetSourceNames_Should_Fail_WhenEffectiveNamesAreDuplicated()
    {
        using var testDirectory = new TestDirectory();
        var productsProject = IOPath.Combine(
            testDirectory.Path,
            "Products.csproj");
        var reviewsProject = IOPath.Combine(
            testDirectory.Path,
            "Reviews.csproj");
        var gatewayProject = IOPath.Combine(
            testDirectory.Path,
            "Gateway.csproj");
        File.WriteAllText(productsProject, "<Project />");
        File.WriteAllText(reviewsProject, "<Project />");
        File.WriteAllText(gatewayProject, "<Project />");
        var builder = DistributedApplication.CreateBuilder();
        var products = builder
            .AddProject("products", productsProject)
            .WithGraphQLSchemaFile(sourceSchemaName: "shared");
        var reviews = builder
            .AddProject("reviews", reviewsProject)
            .WithGraphQLSchemaFile(sourceSchemaName: "shared");
        builder
            .AddProject("gateway", gatewayProject)
            .WithReference(products)
            .WithReference(reviews)
            .WithGraphQLSchemaComposition();
        var model = new DistributedApplicationModel(builder.Resources);

        var exception = Assert.Throws<InvalidOperationException>(
            () => FusionPipelineExecutor.GetSourceNames(model));

        Assert.Equal(
            "Multiple provider resources map to Fusion source 'shared'.",
            exception.Message);
    }

    [Fact]
    public void SelectDeployments_Should_Fail_WhenMappingIsAmbiguous()
    {
        var builder = DistributedApplication.CreateBuilder();
        var nitro = builder
            .AddNitroPublishTarget("nitro")
            .WithNitroCloudUrl("https://api.chillicream.com")
            .WithNitroApiId("products");
        nitro
            .AddFusionDeployment("production-a")
            .ForEnvironment("Production")
            .ToStage("production")
            .WithConfigurationTag("release-1");
        nitro
            .AddFusionDeployment("production-b")
            .ForEnvironment("Production")
            .ToStage("production")
            .WithConfigurationTag("release-1");
        var model = new DistributedApplicationModel(builder.Resources);

        var exception = Assert.Throws<InvalidOperationException>(
            () => FusionPipeline.SelectDeployments(model, "Production"));

        Assert.Equal(
            "Multiple Fusion deployments map environment 'Production' to Nitro "
            + "API 'products' stage 'production'.",
            exception.Message);
    }

    [Fact]
    public void CreateSteps_Should_WireArtifactAndRemoteRoots()
    {
        // arrange
        var resource = new FusionPipelineResource("fusion-pipeline");

        // act
        var steps = FusionPipeline.CreateSteps(
            CreatePipelineStepFactoryContext(resource),
            new FusionPipelineTopology());

        // assert
        string.Join(
            Environment.NewLine,
            steps.Select(step =>
                $"{step.Name}: depends=[{string.Join(", ", step.DependsOnSteps)}]; "
                + $"requiredBy=[{string.Join(", ", step.RequiredBySteps)}]"))
            .MatchInlineSnapshot(
                """
                fusion-artifacts: depends=[]; requiredBy=[publish]
                fusion-upload: depends=[fusion-artifacts]; requiredBy=[]
                fusion-download: depends=[]; requiredBy=[]
                fusion-compose: depends=[fusion-download]; requiredBy=[]
                fusion-readiness: depends=[fusion-compose]; requiredBy=[]
                fusion-publish-stage: depends=[fusion-readiness]; requiredBy=[]
                fusion-publish: depends=[fusion-publish-stage]; requiredBy=[deploy]
                """);
    }

    [Fact]
    public void CreateSteps_Should_IsolatePublishFromUploadSteps()
    {
        // arrange
        var resource = new FusionPipelineResource("fusion-pipeline");

        // act
        var steps = FusionPipeline.CreateSteps(
            CreatePipelineStepFactoryContext(resource),
            new FusionPipelineTopology());

        // assert
        var stepsByName = steps.ToDictionary(
            step => step.Name,
            StringComparer.Ordinal);
        string.Join(
                Environment.NewLine,
                GetTransitiveDependencies(
                        stepsByName,
                        FusionPipeline.PublishStepName)
                    .Order(StringComparer.Ordinal))
            .MatchInlineSnapshot(
                """
                fusion-compose
                fusion-download
                fusion-publish-stage
                fusion-readiness
                """);
    }

    [Fact]
    public void WireGatewayDeployment_Should_PublishStageBeforeGatewayStarts()
    {
        var resource = new FusionPipelineResource("fusion-pipeline");
        var stagePublication = CreatePipelineStep(
            FusionPipeline.PublishStageStepName,
            resource,
            "fusion");
        var gatewayDeployment = CreatePipelineStep(
            "deploy-gateway",
            resource,
            WellKnownPipelineTags.DeployCompute);
        var publication = CreatePipelineStep(
            FusionPipeline.PublishStepName,
            resource,
            "fusion");
        publication.DependsOn(stagePublication);

        FusionPipeline.WireGatewayDeployment(
            stagePublication,
            publication,
            [gatewayDeployment]);

        string.Join(
                Environment.NewLine,
                new[] { stagePublication, gatewayDeployment, publication }
                    .Select(step =>
                        $"{step.Name}: depends=[{string.Join(", ", step.DependsOnSteps)}]"))
            .MatchInlineSnapshot(
                """
                fusion-publish-stage: depends=[]
                deploy-gateway: depends=[fusion-publish-stage]
                fusion-publish: depends=[fusion-publish-stage, deploy-gateway]
                """);
    }

    [Fact]
    public void SelectResourceDeploymentSteps_Should_PreferDirectDeployComputeSteps()
    {
        var source = new FusionPipelineResource("products");
        var steps = new[]
        {
            CreatePipelineStep(
                "deploy-products",
                source,
                WellKnownPipelineTags.DeployCompute),
            CreatePipelineStep(
                "provision-products",
                source,
                WellKnownPipelineTags.ProvisionInfrastructure)
        };

        string.Join(
                Environment.NewLine,
                FusionPipeline
                    .SelectResourceDeploymentSteps(
                        CreatePipelineConfigurationContext(steps),
                        source)
                    .Select(step => step.Name))
            .MatchInlineSnapshot(
                """
                deploy-products
                """);
    }

    [Fact]
    public void SelectResourceDeploymentSteps_Should_UseDeploymentTargetDeployComputeSteps()
    {
        var source = new FusionPipelineResource("products");
        var deploymentTarget = new FusionPipelineResource(
            "products-containerapp");
        source.Annotations.Add(
            new DeploymentTargetAnnotation(deploymentTarget));
        var steps = new[]
        {
            CreatePipelineStep(
                "build-products",
                source,
                WellKnownPipelineTags.BuildCompute),
            CreatePipelineStep(
                "deploy-products",
                deploymentTarget,
                WellKnownPipelineTags.DeployCompute),
            CreatePipelineStep(
                "provision-products-containerapp",
                deploymentTarget,
                WellKnownPipelineTags.ProvisionInfrastructure)
        };

        string.Join(
                Environment.NewLine,
                FusionPipeline
                    .SelectResourceDeploymentSteps(
                        CreatePipelineConfigurationContext(steps),
                        source)
                    .Select(step => step.Name))
            .MatchInlineSnapshot(
                """
                deploy-products
                """);
    }

    [Fact]
    public void SelectResourceDeploymentSteps_Should_NotUseProvisionInfrastructureAsTerminal()
    {
        var source = new FusionPipelineResource("products");
        var deploymentTarget = new FusionPipelineResource(
            "products-containerapp");
        source.Annotations.Add(
            new DeploymentTargetAnnotation(deploymentTarget));
        var steps = new[]
        {
            CreatePipelineStep(
                "build-products",
                source,
                WellKnownPipelineTags.BuildCompute),
            CreatePipelineStep(
                "provision-products-containerapp",
                deploymentTarget,
                WellKnownPipelineTags.ProvisionInfrastructure)
        };

        string.Join(
                Environment.NewLine,
                FusionPipeline
                    .SelectResourceDeploymentSteps(
                        CreatePipelineConfigurationContext(steps),
                        source)
                    .Select(step => step.Name))
            .MatchInlineSnapshot(
                "");
    }

    [Fact]
    public void EnsureResourceDeploymentOrdering_Should_Fail_WhenExternalResourceHasNoDeploymentStep()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => FusionPipeline.EnsureResourceDeploymentOrdering(
                ["external-products"]));

        Assert.Equal(
            "Fusion publication cannot prove compute deployment ordering for resources: "
            + "external-products",
            exception.Message);
    }

    [Fact]
    public void ResolveSourceSchemaSettings_Should_UseDifferentEnvironmentOverrides_WhenSourceIsShared()
    {
        using var sourceSettings = JsonDocument.Parse(
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
            """);

        using var staging =
            FusionPipelineExecutor.ResolveSourceSchemaSettings(
                sourceSettings,
                "staging");
        using var production =
            FusionPipelineExecutor.ResolveSourceSchemaSettings(
                sourceSettings,
                "production");

        string.Join(
                Environment.NewLine,
                "source: products@release-1",
                $"staging: {GetHttpUrl(staging)}",
                $"production: {GetHttpUrl(production)}")
            .MatchInlineSnapshot(
                """
                source: products@release-1
                staging: https://products.staging.example.com/graphql
                production: https://products.example.com/graphql
                """);
    }

    [Fact]
    public void ResolveCompositionEnvironment_Should_UseStage_WhenNoOverrideExists()
    {
        var deployment = new FusionDeploymentResource(
            "production",
            new NitroPublishTargetResource("nitro"))
        {
            StageName = "production"
        };

        var environment =
            FusionPipelineExecutor.ResolveCompositionEnvironment(
                deployment,
                new GraphQLCompositionSettings());

        Assert.Equal("production", environment);
    }

    [Fact]
    public async Task VerifyFileDigestAsync_Should_Fail_WhenComposedArchiveChanges()
    {
        using var testDirectory = new TestDirectory();
        var farPath = IOPath.Combine(
            testDirectory.Path,
            "fusion-configuration.far");
        await File.WriteAllTextAsync(
            farPath,
            "tampered",
            TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => FusionPipelineExecutor.VerifyFileDigestAsync(
                farPath,
                new string('0', 64),
                "composed Fusion archive",
                TestContext.Current.CancellationToken));

        Assert.Equal(
            "The composed Fusion archive SHA-256 does not match prepared state.",
            exception.Message);
    }

    [Fact]
    public void BoundedMemoryStream_Should_RejectWritesBeyondConfiguredLimit()
    {
        using var stream = new BoundedMemoryStream(3);

        stream.Write(new byte[] { 1, 2, 3 });
        var exception = Assert.Throws<InvalidDataException>(
            () => stream.WriteByte(4));

        Assert.Equal(
            "The composed Fusion archive exceeds the 3-byte in-memory size limit.",
            exception.Message);
        Assert.Equal(3, stream.Length);
    }

    [Fact]
    public void BoundedMemoryStream_Should_ClearBackingBuffer_WhenDisposed()
    {
        var stream = new BoundedMemoryStream(3);
        stream.Write(new byte[] { 1, 2, 3 });
        var buffer = stream.GetBuffer();

        stream.Dispose();

        Assert.Equal(new byte[buffer.Length], buffer);
    }

    [Fact]
    public void TransferComposition_Should_ClearArchive_When_CanceledBeforeTransfer()
    {
        // arrange
        using var state = new FusionDeploymentSessionState(
            "release-1",
            "https://api.chillicream.com",
            "products",
            []);
        byte[] fusionArchive = [1, 2, 3];
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // act
        Assert.Throws<OperationCanceledException>(
            () => FusionPipelineExecutor.Instance.TransferComposition(
                state,
                "development",
                fusionArchive,
                cancellation.Token));

        // assert
        Assert.Equal(new byte[3], fusionArchive);
        Assert.Throws<InvalidOperationException>(() => state.FusionArchive);
    }

    [Fact]
    public void GetTransportEndpoint_Should_Fail_WhenProductionBindingIsMissing()
    {
        using var settings = JsonDocument.Parse(
            """
            {
              "name": "products"
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(
            () => FusionPipelineExecutor.GetTransportEndpoint(settings));

        Assert.Equal(
            "Fusion deployment settings must specify an absolute production "
            + "transports.http.url.",
            exception.Message);
    }

    [Fact]
    public async Task WaitForReadiness_Should_Retry_WhenEndpointIsTransientlyUnavailable()
    {
        using var handler = new StubHttpMessageHandler(
            (attempt, _, _) => Task.FromResult(
                new HttpResponseMessage(
                    attempt == 1
                        ? HttpStatusCode.ServiceUnavailable
                        : HttpStatusCode.NoContent)));
        using var httpClient = new HttpClient(handler);

        await FusionPipelineExecutor.WaitForReadinessAsync(
            httpClient,
            "products",
            new Uri("https://products.example.com/graphql"),
            TimeSpan.FromSeconds(1),
            TimeSpan.Zero,
            CancellationToken.None);

        Assert.Equal(2, handler.Attempts);
    }

    [Fact]
    public async Task WaitForReadiness_Should_FailWithContext_WhenDeadlineExpires()
    {
        using var handler = new StubHttpMessageHandler(
            async (_, _, cancellationToken) =>
            {
                await Task.Delay(
                    Timeout.InfiniteTimeSpan,
                    cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });
        using var httpClient = new HttpClient(handler);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => FusionPipelineExecutor.WaitForReadinessAsync(
                httpClient,
                "products",
                new Uri("https://products.example.com/graphql"),
                TimeSpan.FromMilliseconds(50),
                TimeSpan.Zero,
                CancellationToken.None));

        Assert.Equal(
            "Fusion source 'products' at 'https://products.example.com/graphql' "
            + "did not pass its production readiness check within 00:00:00.0500000.",
            exception.Message);
    }

    [Fact]
    public void ReplaceDirectoryAtomically_Should_RemoveSource_WhenSourceWasRemoved()
    {
        using var testDirectory = new TestDirectory();
        var destination = IOPath.Combine(testDirectory.Path, "production");
        var replacement = IOPath.Combine(testDirectory.Path, "replacement");
        WriteArtifactFile(destination, "sources/products/schema.graphqls");
        WriteArtifactFile(destination, "sources/reviews/schema.graphqls");
        WriteArtifactFile(destination, "nitro-deployment.json");
        WriteArtifactFile(replacement, "sources/products/schema.graphqls");
        WriteArtifactFile(replacement, "nitro-deployment-template.json");

        FusionPipelineExecutor.ReplaceDirectoryAtomically(
            replacement,
            destination);

        GetArtifactFiles(destination)
            .MatchInlineSnapshot(
                """
                nitro-deployment-template.json
                sources/products/schema.graphqls
                """);
    }

    [Fact]
    public void ReplaceDirectoryAtomically_Should_RemoveExtensions_WhenExtensionsWereRemoved()
    {
        using var testDirectory = new TestDirectory();
        var destination = IOPath.Combine(testDirectory.Path, "production");
        var replacement = IOPath.Combine(testDirectory.Path, "replacement");
        WriteArtifactFile(destination, "sources/products/schema.graphqls");
        WriteArtifactFile(
            destination,
            "sources/products/schema-extensions.graphqls");
        WriteArtifactFile(replacement, "sources/products/schema.graphqls");

        FusionPipelineExecutor.ReplaceDirectoryAtomically(
            replacement,
            destination);

        GetArtifactFiles(destination)
            .MatchInlineSnapshot("sources/products/schema.graphqls");
    }

    [Theory]
    [InlineData("release/1")]
    [InlineData(@"release\1")]
    [InlineData("release:1")]
    [InlineData("..")]
    public void ValidatePathSegment_Should_Fail_WhenValueIsNotPortable(
        string value)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => FusionPipelineExecutor.ValidatePathSegment(
                value,
                "configuration tag"));

        Assert.Equal(
            $"Fusion configuration tag '{value}' cannot be used as a portable path segment.",
            exception.Message);
    }

    private static void WriteArtifactFile(
        string root,
        string relativePath)
    {
        var path = IOPath.Combine(
            root,
            relativePath.Replace('/', IOPath.DirectorySeparatorChar));
        Directory.CreateDirectory(IOPath.GetDirectoryName(path)!);
        File.WriteAllText(path, "artifact");
    }

    private static string GetArtifactFiles(string root)
        => string.Join(
            Environment.NewLine,
            Directory
                .EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(path => IOPath.GetRelativePath(root, path).Replace('\\', '/'))
                .Order());

    private static HashSet<string> GetTransitiveDependencies(
        IReadOnlyDictionary<string, PipelineStep> steps,
        string stepName)
    {
        var dependencies = new HashSet<string>(StringComparer.Ordinal);

        Visit(stepName);
        return dependencies;

        void Visit(string current)
        {
            foreach (var dependency in steps[current].DependsOnSteps)
            {
                if (dependencies.Add(dependency))
                {
                    Visit(dependency);
                }
            }
        }
    }

    private static PipelineStep CreatePipelineStep(
        string name,
        IResource resource,
        string tag)
        => new()
        {
            Name = name,
            Description = name,
            Resource = resource,
            Tags = [tag],
            Action = _ => Task.CompletedTask
        };

    private static PipelineStepFactoryContext CreatePipelineStepFactoryContext(
        IResource resource)
    {
        var builder = DistributedApplication.CreateBuilder();
        var services = builder.Services.BuildServiceProvider();

        return new PipelineStepFactoryContext
        {
            PipelineContext = new PipelineContext(
                new DistributedApplicationModel(builder.Resources),
                new DistributedApplicationExecutionContext(
                    DistributedApplicationOperation.Publish),
                services,
                NullLogger.Instance,
                TestContext.Current.CancellationToken),
            Resource = resource
        };
    }

    private static PipelineConfigurationContext
        CreatePipelineConfigurationContext(
            IReadOnlyList<PipelineStep> steps)
    {
        var builder = DistributedApplication.CreateBuilder();

        return new PipelineConfigurationContext
        {
            Services = builder.Services.BuildServiceProvider(),
            Steps = steps,
            Model = new DistributedApplicationModel(builder.Resources)
        };
    }

    private static string GetHttpUrl(JsonDocument settings)
        => settings.RootElement
            .GetProperty("transports")
            .GetProperty("http")
            .GetProperty("url")
            .GetString()!;

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Path = IOPath.Combine(
                IOPath.GetTempPath(),
                "chilicream-nitro-aspire-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }

    private sealed class StubHttpMessageHandler(
        Func<
            int,
            HttpRequestMessage,
            CancellationToken,
            Task<HttpResponseMessage>> sendAsync)
        : HttpMessageHandler
    {
        private int _attempts;

        public int Attempts => _attempts;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => sendAsync(
                Interlocked.Increment(ref _attempts),
                request,
                cancellationToken);
    }
}

#pragma warning restore ASPIREPIPELINES001
