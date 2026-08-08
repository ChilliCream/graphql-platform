using System.Runtime.CompilerServices;
using System.Text.Json;
using Aspire.Hosting;

namespace HotChocolate.Fusion.Aspire;

public sealed class GraphQLResourceBuilderExtensionsTests
{
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
