using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.OpenApi.Tests;

public class SchemaGenerationTests
{
    [Fact]
    public async Task Simple_PetStore_Generates_Correct_Schema()
    {
        // Arrange
        await using var stream = File.Open(System.IO.Path.Combine("__resources__", "PetStore.yaml"), FileMode.Open);

        // Act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddOpenApi(stream)
            .BuildSchemaAsync();

        // Assert
        schema.Print().MatchSnapshot();

    }
}
