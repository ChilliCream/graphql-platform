using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using HotChocolate.Fusion.Aspire.Nitro;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Aspire;

public sealed class GraphQLResourceBuilderExtensionsTests
{
    [Theory]
    [InlineData("validation", null, "gateway.far")]
    [InlineData("validation", "gateway.far", "gateway.far")]
    [InlineData("validation", "gateway.fgp", "gateway.fgp")]
    [InlineData("validation", "graph.far", "graph.far")]
    [InlineData("validation", "archives/custom.far", "archives/custom.far")]
    [InlineData("settings", null, "gateway.far")]
    [InlineData("settings", "gateway.far", "gateway.far")]
    [InlineData("settings", "gateway.fgp", "gateway.fgp")]
    [InlineData("settings", "graph.far", "graph.far")]
    [InlineData("settings", "archives/custom.far", "archives/custom.far")]
    [InlineData("legacy", null, "gateway.far")]
    [InlineData("legacy", "gateway.fgp", "gateway.fgp")]
    [InlineData("legacy", "archives/custom.far", "archives/custom.far")]
    public async Task WithNitroComposition_Should_ComposeReadableArchive_When_OutputFileNameIsConfigured(
        string overload,
        string? outputFileName,
        string expectedFileName)
    {
        // arrange
        using var directory = new NitroTestDirectory();
        var routerProjectFile = directory.WriteFile("router.csproj", "<Project />");
        var productsDirectory = Directory.CreateDirectory(directory.GetPath("products-resource"));
        var productsProjectFile = System.IO.Path.Combine(productsDirectory.FullName, "products.csproj");
        await File.WriteAllTextAsync(
            productsProjectFile, "<Project />", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            System.IO.Path.Combine(productsDirectory.FullName, "schema-settings.json"),
            """{ "name": "products" }""",
            TestContext.Current.CancellationToken);
        Directory.CreateDirectory(directory.GetPath("archives"));
        await using var productsServer = await SchemaEndpointServer.StartAsync(
            "/graphql/schema.graphql",
            "type Query { product: String }");

        var builder = DistributedApplication.CreateBuilder();
        var products = builder
            .AddProject("products-resource", productsProjectFile)
            .WithHttpEndpoint(name: "http")
            .WithGraphQLHttpEndpoint();
        var router = builder.AddProject("storefront", routerProjectFile);
        var settings = new GraphQLCompositionSettings();
        var configured = (overload, outputFileName) switch
        {
            ("validation", null) => router.WithNitroComposition(),
            ("validation", _) => router.WithNitroComposition(outputFileName: outputFileName),
            ("settings", null) => router.WithNitroComposition(settings),
            ("settings", _) => router.WithNitroComposition(settings, outputFileName),
#pragma warning disable CS0618 // Verify the retained composition alias.
            ("legacy", null) => router.WithGraphQLSchemaComposition(settings),
            ("legacy", _) => router.WithGraphQLSchemaComposition(settings, outputFileName),
#pragma warning restore CS0618
            _ => throw new ArgumentOutOfRangeException(nameof(overload))
        };
        configured.WithReference(products);
        var model = new DistributedApplicationModel(builder.Resources);
        var resource = Assert.Single(model.GetGraphQLCompositionResources());
        var harness = CompositionHarness.Create(coordinator: null, waitForRunningState: true);
        products.Resource.AllocateHttpEndpoint(productsServer.Port);
        await harness.Notifications.PublishUpdateAsync(
            products.Resource,
            snapshot => snapshot with { State = KnownResourceStates.Running });
        using var gate = new SemaphoreSlim(1, 1);

        // act
        await harness.Composition.ComposeOnGatewayStartAsync(
            resource,
            model,
            gate,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Same(router, configured);
        Assert.Equal(expectedFileName, resource.GetCompositionSettings()!.OutputFileName);
        var archivePath = directory.GetPath(expectedFileName);
        using var archive = FusionArchive.Open(archivePath);
        using var configuration = await archive.TryGetRouterConfigurationAsync(
            WellKnownVersions.LatestRouterFormatVersion,
            TestContext.Current.CancellationToken);
        Assert.NotNull(configuration);
        await using var schemaStream = await configuration.OpenReadSchemaAsync(
            TestContext.Current.CancellationToken);
        using var reader = new StreamReader(schemaStream);
        var schema = Utf8GraphQLParser.Parse(
            await reader.ReadToEndAsync(TestContext.Current.CancellationToken));
        await using var zip = ZipFile.OpenRead(archivePath);
        // composition-settings.json is only written for the overloads that pass an explicit
        // GraphQLCompositionSettings, its presence is not what this test verifies.
        var entries = string.Join(
            "\n",
            zip.Entries
                .Select(entry => entry.FullName)
                .Where(name => name != "composition-settings.json")
                .Order(StringComparer.Ordinal));
        var query = schema.Definitions.OfType<ObjectTypeDefinitionNode>().Single(type => type.Name.Value == "Query");

        $"""
        Resource: {resource.Name}
        Archive entries:
        {entries}
        Query:
        {query}
        """.MatchInlineSnapshot(
            """
            Resource: storefront
            Archive entries:
            archive-metadata.json
            gateway/2.0.0/gateway-settings.json
            gateway/2.0.0/gateway.graphqls
            source-schemas/products/schema-settings.json
            source-schemas/products/schema.graphqls
            Query:
            type Query @fusion__type(schema: PRODUCTS) {
              product: String @fusion__field(schema: PRODUCTS)
            }
            """);
    }

    [Fact]
    public void WithGraphQLHttpEndpoint_Should_UseDefaultPaths_When_PathsAreOmitted()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        var resource = builder
            .AddProject("products", GetTestProjectFile())
            .WithGraphQLHttpEndpoint();

        // assert
        var annotation = Assert.Single(
            resource.Resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
        Assert.Equal("/graphql", annotation.GraphQLPath);
        Assert.Equal("/graphql/schema.graphql", annotation.SchemaPath);
        Assert.Equal("http", annotation.EndpointName);
        Assert.Equal(SourceSchemaLocationType.SchemaEndpoint, annotation.Location);
    }

    [Fact]
    public void WithGraphQLHttpEndpoint_Should_PreserveConfiguration_When_ArgumentsAreProvided()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        var resource = builder
            .AddProject("products", GetTestProjectFile())
            .WithGraphQLHttpEndpoint(
                path: "/api/graphql",
                schemaPath: "/api/schema.graphql",
                endpointName: "https",
                sourceSchemaName: "Products");

        // assert
        var annotation = Assert.Single(
            resource.Resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
        Assert.Equal("/api/graphql", annotation.GraphQLPath);
        Assert.Equal("/api/schema.graphql", annotation.SchemaPath);
        Assert.Equal("https", annotation.EndpointName);
        Assert.Equal("Products", annotation.SourceSchemaName);
    }

    [Fact]
    public void WithGraphQLHttpEndpoint_Should_KeepSchemaPathNull_When_SchemaPathIsNull()
    {
        // arrange
        // a null schema path must survive as null, which an Apollo Federation source schema needs
        // because it serves its schema through the GraphQL endpoint.
        var builder = DistributedApplication.CreateBuilder();

        // act
        var resource = builder
            .AddProject("products", GetTestProjectFile())
            .WithGraphQLHttpEndpoint(path: "/api/graphql", schemaPath: null);

        // assert
        var annotation = Assert.Single(
            resource.Resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
        Assert.Null(annotation.SchemaPath);
        Assert.Equal("/api/graphql", annotation.GraphQLPath);
    }

    [Fact]
    public void WithGraphQLHttpEndpoint_Should_RejectPath_When_PathIsNotRooted()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        var resource = builder.AddProject("products", GetTestProjectFile());

        // act
        var exception = Assert.Throws<ArgumentException>(
            () => resource.WithGraphQLHttpEndpoint(path: "graphql"));

        // assert
        Assert.Equal(
            "The GraphQL endpoint path must start with '/'. (Parameter 'path')",
            exception.Message);
    }

    [Fact]
    public void WithGraphQLHttpEndpoint_Should_RejectSchemaPath_When_SchemaPathIsNotRooted()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        var resource = builder.AddProject("products", GetTestProjectFile());

        // act
        var exception = Assert.Throws<ArgumentException>(
            () => resource.WithGraphQLHttpEndpoint(schemaPath: "schema.graphql"));

        // assert
        Assert.Equal(
            "The GraphQL schema endpoint path must start with '/'. (Parameter 'schemaPath')",
            exception.Message);
    }

#pragma warning disable CS0618 // Verify the obsolete API.
    [Fact]
    public void WithGraphQLSchemaEndpoint_Should_KeepPathImplicit_When_PathIsOmitted()
    {
        var builder = DistributedApplication.CreateBuilder();
        var resource = builder
            .AddProject("products", GetTestProjectFile())
            .WithGraphQLSchemaEndpoint();

        var annotation = Assert.Single(
            resource.Resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
        Assert.Null(annotation.SchemaPath);
        Assert.Null(annotation.GraphQLPath);
        Assert.Equal(SourceSchemaLocationType.SchemaEndpoint, annotation.Location);
    }

    [Fact]
    public void WithGraphQLSchemaEndpoint_Should_PreserveExplicitPath_When_PathIsConfigured()
    {
        var builder = DistributedApplication.CreateBuilder();
        var resource = builder
            .AddProject("products", GetTestProjectFile())
            .WithGraphQLSchemaEndpoint(path: "/custom/schema");

        var annotation = Assert.Single(
            resource.Resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
        Assert.Equal("/custom/schema", annotation.SchemaPath);
        Assert.Null(annotation.GraphQLPath);
    }

    [Fact]
    public void WithGraphQLSchemaEndpoint_Should_RejectPath_When_PathIsNotRooted()
    {
        var builder = DistributedApplication.CreateBuilder();
        var resource = builder.AddProject("products", GetTestProjectFile());

        var exception = Assert.Throws<ArgumentException>(
            () => resource.WithGraphQLSchemaEndpoint(path: "graphql"));

        Assert.Equal(
            "The GraphQL schema endpoint path must start with '/'. (Parameter 'path')",
            exception.Message);
    }
#pragma warning restore CS0618

    [Theory]
    [InlineData(null, null, "GraphQL")]
    [InlineData("1.0", "Version1", "ApolloFederation")]
    [InlineData("2.0", "Version2", "ApolloFederation")]
    public void ReadEndpointConfiguration_Should_SelectProtocol_When_SettingsAreValid(
        string? version,
        string? expectedVersion,
        string expectedProtocol)
    {
        using var settings = CreateSettings("Products", version);

        var configuration = SchemaComposition.ReadEndpointConfiguration(
            "products-resource",
            configuredSourceSchemaName: null,
            settings);

        Assert.Equal("Products", configuration.SourceSchemaName);
        Assert.Equal(expectedVersion, configuration.ApolloFederationVersion?.ToString());
        Assert.Equal(expectedProtocol, configuration.Protocol.ToString());
    }

    [Fact]
    public void ReadEndpointConfiguration_Should_AcceptConfiguredName_When_NameMatchesExactly()
    {
        using var settings = CreateSettings("Products", version: null);

        var configuration = SchemaComposition.ReadEndpointConfiguration(
            "products-resource",
            "Products",
            settings);

        Assert.Equal("Products", configuration.SourceSchemaName);
    }

    [Fact]
    public void ReadEndpointConfiguration_Should_RejectConfiguredName_When_NameDoesNotMatchExactly()
    {
        using var settings = CreateSettings("Products", version: null);

        var exception = Assert.Throws<InvalidOperationException>(
            () => SchemaComposition.ReadEndpointConfiguration(
                "products-resource",
                "products",
                settings));

        Assert.Equal(
            "The configured source schema name 'products' for resource 'products-resource' "
            + "does not match schema-settings.json name 'Products'.",
            exception.Message);
    }

    private static JsonDocument CreateSettings(string name, string? version)
        => JsonDocument.Parse(
            version is null
                ? $$"""
                  {
                    "name": "{{name}}"
                  }
                  """
                : $$"""
                  {
                    "name": "{{name}}",
                    "extensions": {
                      "chillicream": {
                        "apolloFederationSupport": {
                          "version": "{{version}}"
                        }
                      }
                    }
                  }
                  """);

    private static string GetTestProjectFile([CallerFilePath] string sourceFile = "")
        => System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(sourceFile)!,
            "HotChocolate.Fusion.Aspire.Tests.csproj");
}
