using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using System.Text.Json;

namespace ChilliCream.Nitro.Aspire;

#pragma warning disable ASPIREPIPELINES001

public sealed class FusionPipelineTests
{
    [Fact]
    public void SelectDeployments_Should_SelectOnlyMatchingEnvironment()
    {
        var builder = DistributedApplication.CreateBuilder();
        var nitro = builder
            .AddNitro("nitro")
            .WithCloudUrl("https://api.chillicream.com")
            .WithApiId("products");
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
    public void SelectDeployments_Should_ReturnEmpty_WhenEnvironmentDoesNotMatch()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder
            .AddNitro("nitro")
            .WithCloudUrl("https://api.chillicream.com")
            .WithApiId("products")
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
    public void SelectDeployments_Should_Fail_WhenMappingIsAmbiguous()
    {
        var builder = DistributedApplication.CreateBuilder();
        var nitro = builder
            .AddNitro("nitro")
            .WithCloudUrl("https://api.chillicream.com")
            .WithApiId("products");
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
    public void CreateStepDefinitions_Should_WireArtifactAndRemoteRoots()
    {
        var resource = new FusionPipelineResource("fusion-pipeline");

        var steps = FusionPipeline.CreateStepDefinitionsForTest(resource);

        string.Join(
            Environment.NewLine,
            steps.Select(step =>
                $"{step.Name}: depends=[{string.Join(", ", step.DependsOnSteps)}]; "
                + $"requiredBy=[{string.Join(", ", step.RequiredBySteps)}]"))
            .MatchInlineSnapshot(
                """
                fusion-artifacts: depends=[]; requiredBy=[publish]
                fusion-readiness: depends=[fusion-artifacts]; requiredBy=[]
                fusion-upload: depends=[fusion-artifacts]; requiredBy=[]
                fusion-publish: depends=[fusion-upload, fusion-readiness]; requiredBy=[deploy]
                """);
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
    public void ReplaceDirectoryAtomically_Should_RemoveSource_WhenSourceWasRemoved()
    {
        using var testDirectory = new TestDirectory();
        var destination = Path.Combine(testDirectory.Path, "production");
        var replacement = Path.Combine(testDirectory.Path, "replacement");
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
        var destination = Path.Combine(testDirectory.Path, "production");
        var replacement = Path.Combine(testDirectory.Path, "replacement");
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
        var path = Path.Combine(
            root,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "artifact");
    }

    private static string GetArtifactFiles(string root)
        => string.Join(
            Environment.NewLine,
            Directory
                .EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
                .Order());

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
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
}

#pragma warning restore ASPIREPIPELINES001
