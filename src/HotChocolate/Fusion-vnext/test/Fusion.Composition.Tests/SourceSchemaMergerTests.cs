using System.Collections.Immutable;
using HotChocolate.Fusion.Comparers;
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
        var result = merger.Merge();

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
        IEnumerable<SchemaDefinition> schemas =
        [
            new() { Name = "ExampleOne" },
            new() { Name = "Example_Two" },
            new() { Name = "Example__Three" },
            new() { Name = "ExampleFourFive" }
        ];

        var merger =
            new SourceSchemaMerger(schemas.ToImmutableSortedSet(new SchemaByNameComparer()));

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        SchemaFormatter.FormatAsString(result.Value).MatchSnapshot(extension: ".graphql");
    }
}
