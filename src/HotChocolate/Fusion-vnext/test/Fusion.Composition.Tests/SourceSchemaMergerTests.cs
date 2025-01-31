using HotChocolate.Skimmed;
using HotChocolate.Skimmed.Serialization;
using static HotChocolate.Fusion.WellKnownTypeNames;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerTests
{
    [Fact]
    public void Merge_WithOperationTypes_SetsOperationTypes()
    {
        // arrange
        var merger = new SourceSchemaMerger(
        [
            new SchemaDefinition
            {
                Types =
                {
                    new ObjectTypeDefinition(Query),
                    new ObjectTypeDefinition(Mutation),
                    new ObjectTypeDefinition(Subscription)
                }
            }
        ]);

        // act
        var result = merger.MergeSchemas();

        // assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.QueryType);
        Assert.NotNull(result.Value.MutationType);
        Assert.NotNull(result.Value.SubscriptionType);
    }

    [Fact]
    public void Merge_FourNamedSchemas_AddsFusionDefinitions()
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
