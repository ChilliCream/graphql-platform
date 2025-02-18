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
        var intType = BuiltIns.Int.Create();
        var merger = new SourceSchemaMerger(
        [
            new SchemaDefinition
            {
                Types =
                {
                    new ObjectTypeDefinition(Query)
                        { Fields = { new OutputFieldDefinition("field", intType) } },
                    new ObjectTypeDefinition(Mutation)
                        { Fields = { new OutputFieldDefinition("field", intType) } },
                    new ObjectTypeDefinition(Subscription)
                        { Fields = { new OutputFieldDefinition("field", intType) } }
                }
            }
        ]);

        // act
        var (isSuccess, _, mergedSchema, _) = merger.Merge();

        // assert
        Assert.True(isSuccess);
        Assert.NotNull(mergedSchema.QueryType);
        Assert.NotNull(mergedSchema.MutationType);
        Assert.NotNull(mergedSchema.SubscriptionType);
    }

    [Fact]
    public void Merge_WithEmptyMutationAndSubscriptionType_RemovesEmptyOperationTypes()
    {
        // arrange
        var merger = new SourceSchemaMerger(
        [
            new SchemaDefinition
            {
                Types =
                {
                    new ObjectTypeDefinition(Query)
                        { Fields = { new OutputFieldDefinition("field", BuiltIns.Int.Create()) } },
                    new ObjectTypeDefinition(Mutation),
                    new ObjectTypeDefinition(Subscription)
                }
            }
        ]);

        // act
        var (isSuccess, _, mergedSchema, _) = merger.Merge();

        // assert
        Assert.True(isSuccess);
        Assert.NotNull(mergedSchema.QueryType);
        Assert.False(mergedSchema.Types.ContainsName(Mutation));
        Assert.Null(mergedSchema.MutationType);
        Assert.False(mergedSchema.Types.ContainsName(Subscription));
        Assert.Null(mergedSchema.SubscriptionType);
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
