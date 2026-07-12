using System.Runtime.CompilerServices;
using System.Text.Json;
using Aspire.Hosting;

namespace HotChocolate.Fusion.Aspire;

public sealed class GraphQLResourceBuilderExtensionsTests
{
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
        Assert.Equal(
            "/custom/schema",
            resource.Resource.GetGraphQLSchemaPath(defaultPath: "/graphql"));
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

    [Theory]
    [InlineData(
        null,
        null,
        "GraphQL",
        "/graphql/schema.graphql")]
    [InlineData(
        "1.0",
        "Version1",
        "ApolloFederation",
        "/graphql")]
    [InlineData(
        "2.0",
        "Version2",
        "ApolloFederation",
        "/graphql")]
    public void ReadEndpointConfiguration_Should_SelectProtocolAndImplicitPath_When_SettingsAreValid(
        string? version,
        string? expectedVersion,
        string expectedProtocol,
        string expectedDefaultPath)
    {
        using var settings = CreateSettings("Products", version);

        var configuration = SchemaComposition.ReadEndpointConfiguration(
            "products-resource",
            configuredSourceSchemaName: null,
            settings);

        Assert.Equal("Products", configuration.SourceSchemaName);
        Assert.Equal(expectedVersion, configuration.ApolloFederationVersion?.ToString());
        Assert.Equal(expectedProtocol, configuration.Protocol.ToString());
        Assert.Equal(expectedDefaultPath, configuration.DefaultPath);
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
