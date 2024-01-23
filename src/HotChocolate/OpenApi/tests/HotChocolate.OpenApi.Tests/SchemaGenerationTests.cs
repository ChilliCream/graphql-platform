using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Skimmed.Serialization;
using Microsoft.OpenApi.Readers;
using Xunit;
using Xunit.Abstractions;
using static System.IO.Path;

namespace HotChocolate.OpenApi.Tests;

public class SchemaGenerationTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public SchemaGenerationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Simple_PetStore_V3_Generates_Correct_SkimmedSchema()
    {
        // Arrange
        await using var stream = File.Open(Combine("__resources__", "PetStore.yaml"), FileMode.Open);
        var wrapper = new OpenApiWrapper();
        var document = new OpenApiStreamReader().Read(stream, out var diag);

        // Act
        var schema = wrapper.Wrap("PetStore", document);

        // Assert
        var sdl = SchemaFormatter.FormatAsString(schema);
        _testOutputHelper.WriteLine(sdl);
        Snapshot.Match(sdl, extension: ".graphql");
    }

    [Fact]
    public async Task Simple_PetStore_V3_Generates_Correct_Schema()
    {
        // Arrange
        var apiDocument  = await File.ReadAllTextAsync(Combine("__resources__", "PetStore.yaml"));

        // Act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddOpenApi("PetStore", apiDocument)
            .BuildSchemaAsync();

        // Assert
        _testOutputHelper.WriteLine(schema.ToString());
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Uber_Generates_Correct_SkimmedSchema()
    {
        // Arrange
        await using var stream = File.Open(Combine("__resources__", "Uber.json"), FileMode.Open);
        var wrapper = new OpenApiWrapper();
        var document = new OpenApiStreamReader().Read(stream, out var diag);

        // Act
        var schema = wrapper.Wrap("PetStore", document);

        // Assert
        var sdl = SchemaFormatter.FormatAsString(schema);
        _testOutputHelper.WriteLine(sdl);
        Assert.NotNull(sdl);
        Snapshot.Match(sdl, extension: ".graphql");
    }
}
