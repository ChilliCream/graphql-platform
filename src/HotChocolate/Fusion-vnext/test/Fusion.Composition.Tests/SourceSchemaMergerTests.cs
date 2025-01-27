using HotChocolate.Skimmed;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerTests
{
    [Fact]
    public void AddFusionDefinitions_FourNamedSchemas_MatchesSnapshot()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            [
                new SchemaDefinition { Name = "ExampleOne" },
                new SchemaDefinition { Name = "Example_Two" },
                new SchemaDefinition { Name = "Example__Three" },
                new SchemaDefinition { Name = "ExampleFourFive" }
            ]);

        // act
        var result = merger.MergeSchemas();

        // assert
        Assert.True(result.IsSuccess);
        SchemaFormatter.FormatAsString(result.Value).MatchSnapshot(extension: ".graphql");
    }
}
