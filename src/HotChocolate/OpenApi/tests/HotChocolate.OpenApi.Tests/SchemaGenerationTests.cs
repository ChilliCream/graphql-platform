using HotChocolate.Execution;
using HotChocolate.Skimmed.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Readers;
using Snapshooter.Xunit;
using Xunit;
using Xunit.Abstractions;

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
        await using var stream = File.Open(System.IO.Path.Combine("__resources__", "PetStore.yaml"), FileMode.Open);
        var wrapper = new OpenApiWrapper();
        var document = new OpenApiStreamReader().Read(stream, out var diag);

        // Act
        var schema = wrapper.Wrap(document);

        // Assert
        var sdl = SchemaFormatter.FormatAsString(schema);
        _testOutputHelper.WriteLine(sdl);
        sdl.MatchSnapshot();
    }

    [Fact]
    public async Task Simple_PetStore_V3_Generates_Correct_Schema()
    {
        // Arrange
        await using var stream = File.Open(System.IO.Path.Combine("__resources__", "PetStore.yaml"), FileMode.Open);

        // Act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddOpenApi(stream)
            .BuildSchemaAsync();

        // Assert
        var sdl = schema.Print();
        _testOutputHelper.WriteLine(sdl);
        sdl.MatchSnapshot();
    }
}
