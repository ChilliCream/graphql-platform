using CookieCrumble;
using HotChocolate.Skimmed.Serialization;
using Microsoft.OpenApi.Readers;

namespace HotChocolate.OpenApi.Tests;

public sealed class OpenApiMutableSchemaBuilderTests
{
    [Fact]
    public async void CreateMutableSchema_WithoutMutationConventions_ReturnsExpectedResult()
    {
        // Arrange
        var input = await File.ReadAllTextAsync("__resources__/petstore-expanded.yaml");
        var openApiDocument = new OpenApiStringReader().Read(input, out _);

        // Act
        var mutableSchema = OpenApiMutableSchemaBuilder.Create(
            openApiDocument,
            httpClientName: "PetStoreExpanded").Build();

        // Assert
        var sdl = SchemaFormatter.FormatAsString(mutableSchema);
        Snapshot.Match(sdl, extension: ".graphql");
    }

    [Fact]
    public async void CreateMutableSchema_WithMutationConventions_ReturnsExpectedResult()
    {
        // Arrange
        var input = await File.ReadAllTextAsync("__resources__/petstore-expanded.yaml");
        var openApiDocument = new OpenApiStringReader().Read(input, out _);

        // Act
        var mutableSchema = OpenApiMutableSchemaBuilder.Create(
            openApiDocument,
            httpClientName: "PetStoreExpanded").AddMutationConventions().Build();

        // Assert
        var sdl = SchemaFormatter.FormatAsString(mutableSchema);
        Snapshot.Match(sdl, extension: ".graphql");
    }

    [Fact]
    public async void CreateMutableSchema_WithDefaultValues_ReturnsExpectedResult()
    {
        // Arrange
        var input = await File.ReadAllTextAsync("__resources__/synthetic-with-default-values.yaml");
        var openApiDocument = new OpenApiStringReader().Read(input, out _);

        // Act
        var mutableSchema = OpenApiMutableSchemaBuilder.Create(
            openApiDocument,
            httpClientName: "SyntheticWithDefaultValues").Build();

        // Assert
        var sdl = SchemaFormatter.FormatAsString(mutableSchema);
        Snapshot.Match(sdl, extension: ".graphql");
    }

    [Fact]
    public async void CreateMutableSchema_WithDeprecations_ReturnsExpectedResult()
    {
        // Arrange
        var input = await File.ReadAllTextAsync("__resources__/synthetic-with-deprecations.yaml");
        var openApiDocument = new OpenApiStringReader().Read(input, out _);

        // Act
        var mutableSchema = OpenApiMutableSchemaBuilder.Create(
            openApiDocument,
            httpClientName: "SyntheticWithDeprecations").Build();

        // Assert
        var sdl = SchemaFormatter.FormatAsString(mutableSchema);
        Snapshot.Match(sdl, extension: ".graphql");
    }

    [Fact]
    public async void CreateMutableSchema_WithLinks_ReturnsExpectedResult()
    {
        // Arrange
        var input = await File.ReadAllTextAsync("__resources__/synthetic-with-links.yaml");
        var openApiDocument = new OpenApiStringReader().Read(input, out _);

        // Act
        var mutableSchema = OpenApiMutableSchemaBuilder.Create(
            openApiDocument,
            httpClientName: "SyntheticWithLinks").Build();

        // Assert
        var sdl = SchemaFormatter.FormatAsString(mutableSchema);
        Snapshot.Match(sdl, extension: ".graphql");
    }

    [Fact]
    public async void CreateMutableSchema_WithTags_ReturnsExpectedResult()
    {
        // Arrange
        var input = await File.ReadAllTextAsync("__resources__/synthetic-with-tags.yaml");
        var openApiDocument = new OpenApiStringReader().Read(input, out _);

        // Act
        var mutableSchema = OpenApiMutableSchemaBuilder
            .Create(openApiDocument, httpClientName: "SyntheticWithTags")
            .Build();

        // Assert
        var sdl = SchemaFormatter.FormatAsString(mutableSchema);
        Snapshot.Match(sdl, extension: ".graphql");
    }
}
