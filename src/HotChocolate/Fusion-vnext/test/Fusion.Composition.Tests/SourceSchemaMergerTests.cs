using System.Collections.Immutable;
using HotChocolate.Fusion.Comparers;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;
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
            new MutableSchemaDefinition
            {
                Types =
                {
                    new MutableObjectTypeDefinition(Query)
                        { Fields = { new MutableOutputFieldDefinition("field") } },
                    new MutableObjectTypeDefinition(Mutation)
                        { Fields = { new MutableOutputFieldDefinition("field") } },
                    new MutableObjectTypeDefinition(Subscription)
                        { Fields = { new MutableOutputFieldDefinition("field") } }
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
            new MutableSchemaDefinition
            {
                Types =
                {
                    new MutableObjectTypeDefinition(Query)
                        { Fields = { new MutableOutputFieldDefinition("field") } },
                    new MutableObjectTypeDefinition(Mutation),
                    new MutableObjectTypeDefinition(Subscription)
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
        IEnumerable<MutableSchemaDefinition> schemas =
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
