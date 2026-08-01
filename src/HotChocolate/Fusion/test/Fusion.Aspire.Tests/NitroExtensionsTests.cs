using System.Runtime.CompilerServices;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using HotChocolate.Fusion.Aspire.Nitro;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

public sealed class NitroExtensionsTests
{
    [Fact]
    public void AddNitro_Should_RegisterOneResourceAndComposition_When_CalledTwice()
    {
        var builder = DistributedApplication.CreateBuilder();

        var first = builder.AddNitro();
        var second = builder.AddNitro();

        Assert.Same(first.Resource, second.Resource);
        Assert.Single(builder.Resources.OfType<NitroResource>());
        DescribeCompositionRegistrations(builder).MatchInlineSnapshot(
            "IDistributedApplicationEventingSubscriber -> SchemaComposition (Singleton)");
    }

    [Fact]
    public void AddGraphQLOrchestrator_Should_DelegateToAddNitro()
    {
        var builder = DistributedApplication.CreateBuilder();

#pragma warning disable CS0618 // Verify the compatibility shim.
        var result = builder.AddGraphQLOrchestrator();
#pragma warning restore CS0618

        Assert.Same(builder, result);
        Assert.Single(builder.Resources.OfType<NitroResource>());
        DescribeCompositionRegistrations(builder).MatchInlineSnapshot(
            "IDistributedApplicationEventingSubscriber -> SchemaComposition (Singleton)");
    }

    [Fact]
    public void AddApiAndStage_Should_CreateTheDeclarativeResourceHierarchy()
    {
        var builder = DistributedApplication.CreateBuilder();
        var nitro = builder.AddNitro();

        var api = nitro
            .AddApi("products-api")
            .WithNitroApiId("QXBpCnByb2R1Y3Rz");
        var development = api.AddStage("dev");
        var production = api
            .AddStage("prod")
            .WithApproval(true)
            .WithForcePublish(true);

        $"""
        Nitro: {nitro.Resource.Name}
        API: {api.Resource.Name}|{api.Resource.ApiName}|{api.Resource.ApiId}
        API parent: {Assert.Single(api.Resource.Annotations.OfType<ResourceRelationshipAnnotation>()).Resource.Name}
        Stages: {string.Join(", ", builder.Resources.OfType<NitroStageResource>().Select(stage => $"{stage.Name}:{stage.StageName}"))}
        Development API: {development.Resource.Api.ApiName}
        Development parent: {Assert.Single(development.Resource.Annotations.OfType<ResourceRelationshipAnnotation>()).Resource.Name}
        Production policy: approval={production.Resource.WaitForApproval}, force={production.Resource.Force}
        """.MatchInlineSnapshot(
            """
            Nitro: nitro
            API: nitro-products-api|products-api|QXBpCnByb2R1Y3Rz
            API parent: nitro
            Stages: nitro-products-api-dev:dev, nitro-products-api-prod:prod
            Development API: products-api
            Development parent: nitro-products-api
            Production policy: approval=True, force=True
            """);
    }

    [Fact]
    public void AddApi_Should_Throw_When_TheApiIsDeclaredTwice()
    {
        var nitro = DistributedApplication.CreateBuilder().AddNitro();
        nitro.AddApi("products-api");

        var exception = Assert.Throws<InvalidOperationException>(
            () => nitro.AddApi("products-api"));

        Assert.Equal("Nitro already declares an API named 'products-api'.", exception.Message);
    }

    [Fact]
    public void AddStage_Should_Throw_When_TheStageIsDeclaredTwiceForTheApi()
    {
        var api = DistributedApplication.CreateBuilder()
            .AddNitro()
            .AddApi("products-api");
        api.AddStage("dev");

        var exception = Assert.Throws<InvalidOperationException>(
            () => api.AddStage("dev"));

        Assert.Equal(
            "Nitro API 'products-api' already declares the stage 'dev'.",
            exception.Message);
    }

    [Fact]
    public void WithNitroCompositionBase_Should_SelectTheStageExplicitly()
    {
        var builder = DistributedApplication.CreateBuilder();
        var stage = builder
            .AddNitro()
            .AddApi("products-api")
            .WithNitroApiId("QXBpCnByb2R1Y3Rz")
            .AddStage("dev");

        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition()
            .WithNitroCompositionBase(stage);

        Assert.Same(stage.Resource, gateway.Resource.GetNitroCompositionBase());
        Assert.Contains(
            gateway.Resource.Annotations.OfType<ResourceRelationshipAnnotation>(),
            relationship => ReferenceEquals(relationship.Resource, stage.Resource));
    }

    [Fact]
    public void WithNitroCompositionBase_Should_BeIdempotentForTheSameStage()
    {
        var builder = DistributedApplication.CreateBuilder();
        var stage = builder.AddNitro().AddApi("products-api").AddStage("dev");
        var gateway = builder.AddProject("gateway", GetTestProjectFile());

        gateway.WithNitroCompositionBase(stage).WithNitroCompositionBase(stage);

        Assert.Single(gateway.Resource.Annotations.OfType<NitroCompositionBaseAnnotation>());
    }

    [Fact]
    public void WithNitroCompositionBase_Should_Throw_When_ASecondStageIsSelected()
    {
        var builder = DistributedApplication.CreateBuilder();
        var api = builder.AddNitro().AddApi("products-api");
        var development = api.AddStage("dev");
        var production = api.AddStage("prod");
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroCompositionBase(development);

        var exception = Assert.Throws<InvalidOperationException>(
            () => gateway.WithNitroCompositionBase(production));

        Assert.Equal(
            "Resource 'gateway' already uses Nitro stage 'dev' as its composition base.",
            exception.Message);
    }

    [Fact]
    public void WithNitroCompositionBase_Should_RegisterAutoUpdateCommands()
    {
        var builder = DistributedApplication.CreateBuilder();
        var stage = builder.AddNitro().AddApi("products-api").AddStage("dev");

        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition()
            .WithNitroCompositionBase(stage);

        string.Join(
                Environment.NewLine,
                gateway.Resource.Annotations
                    .OfType<ResourceCommandAnnotation>()
                    .Select(command => $"{command.Name}: {command.DisplayName}")
                    .Order(StringComparer.Ordinal))
            .MatchInlineSnapshot(
                """
                disable-nitro-auto-update: Disable auto-update
                enable-nitro-auto-update: Enable auto-update
                recompose: Recompose
                """);
    }

    [Fact]
    public void WithGraphQLSchemaComposition_Should_RegisterAutoUpdateCommands_When_CalledLast()
    {
        var builder = DistributedApplication.CreateBuilder();
        var stage = builder.AddNitro().AddApi("products-api").AddStage("dev");
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroCompositionBase(stage);

        gateway.WithGraphQLSchemaComposition();

        Assert.Equal(
            2,
            gateway.Resource.Annotations
                .OfType<ResourceCommandAnnotation>()
                .Count(command => command.Name.Contains("nitro-auto-update", StringComparison.Ordinal)));
    }

    [Fact]
    public void AddNitro_Should_StorePortalAndSeedUpdateConfiguration()
    {
        var builder = DistributedApplication.CreateBuilder();
        var portalUrl = new Uri("https://portal.example.test/custom?tenant=abc");

        var nitro = builder.AddNitro(
            portalUrl,
            options =>
            {
                options.Enabled = false;
                options.AutoUpdate = false;
            });

        Assert.Same(portalUrl, nitro.Resource.PortalUrl);
        Assert.Equal(
            "False|False",
            $"{nitro.Resource.SeedUpdates.Enabled}|{nitro.Resource.SeedUpdates.AutoUpdate}");
    }

    [Fact]
    public void WithNitroApiKey_Should_StoreASecretParameter()
    {
        var builder = DistributedApplication.CreateBuilder();
        var apiKey = builder.AddParameter("nitro-api-key", secret: true);

        var nitro = builder.AddNitro().WithNitroApiKey(apiKey);

        Assert.Same(apiKey.Resource, nitro.Resource.ApiKey);
    }

    [Fact]
    public void WithNitroApiKey_Should_RejectANonSecretParameter()
    {
        var builder = DistributedApplication.CreateBuilder();
        var apiKey = builder.AddParameter("nitro-api-key", secret: false);

        var exception = Assert.Throws<ArgumentException>(
            () => builder.AddNitro().WithNitroApiKey(apiKey));

        Assert.Equal(
            "The Nitro API key parameter must be declared as a secret. (Parameter 'apiKey')",
            exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void WithNitroApiId_Should_Throw_When_TheApiIdIsNotAnId(string? apiId)
    {
        var api = DistributedApplication.CreateBuilder().AddNitro().AddApi("products-api");

        var exception = Record.Exception(() => api.WithNitroApiId(apiId!));

        Assert.Equal("apiId", Assert.IsAssignableFrom<ArgumentException>(exception).ParamName);
    }

    [Fact]
    public void WithNitroCloudUrl_Should_NormalizeTheOrigin()
    {
        var nitro = DistributedApplication.CreateBuilder()
            .AddNitro()
            .WithNitroCloudUrl("https://api.example.test:443/");

        Assert.Equal("https://api.example.test", nitro.Resource.CloudUrl);
    }

    [Fact]
    public void WithNitroCloudUrl_Should_RejectAPath()
    {
        var nitro = DistributedApplication.CreateBuilder().AddNitro();

        var exception = Assert.Throws<ArgumentException>(
            () => nitro.WithNitroCloudUrl("https://api.example.test/graphql"));

        Assert.Equal(
            "The Nitro cloud URL must be an absolute HTTPS origin without a path, query, "
            + "fragment, or user information. (Parameter 'cloudUrl')",
            exception.Message);
    }

    private static string DescribeCompositionRegistrations(IDistributedApplicationBuilder builder)
        => string.Join(
            Environment.NewLine,
            builder.Services
                .Where(descriptor => descriptor.ImplementationType == typeof(SchemaComposition))
                .Select(descriptor =>
                    $"{descriptor.ServiceType.Name} -> {descriptor.ImplementationType!.Name} "
                    + $"({descriptor.Lifetime})"));

    private static string GetTestProjectFile([CallerFilePath] string sourceFile = "")
        => IOPath.Combine(
            IOPath.GetDirectoryName(sourceFile)!,
            "HotChocolate.Fusion.Aspire.Tests.csproj");
}
