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
        var intType = BuiltIns.Int.Create();
        var merger = new SourceSchemaMerger(
        [
            new MutableSchemaDefinition
            {
                Types =
                {
                    new MutableObjectTypeDefinition(Query)
                        { Fields = { new MutableOutputFieldDefinition("field", intType) } },
                    new MutableObjectTypeDefinition(Mutation)
                        { Fields = { new MutableOutputFieldDefinition("field", intType) } },
                    new MutableObjectTypeDefinition(Subscription)
                        { Fields = { new MutableOutputFieldDefinition("field", intType) } }
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
                    {
                        Fields =
                        {
                            new MutableOutputFieldDefinition("field", BuiltIns.Int.Create())
                        }
                    },
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

        var merger = new SourceSchemaMerger(
            schemas.ToImmutableSortedSet(
                new SchemaByNameComparer<MutableSchemaDefinition>()));

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        SchemaFormatter.FormatAsString(result.Value).MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void Merge_WithRequireInputObject_RetainsInputObjectType()
    {
        // arrange
        var schemaA =
            SchemaParser.Parse(
                """
                type Query {
                    product: Product
                }

                type Product {
                    weight: Int!
                }
                """);
        schemaA.Name = "A";
        var schemaB =
            SchemaParser.Parse(
                """
                type Product {
                    deliveryEstimate(
                        zip: String!
                        dimension: ProductDimensionInput! @require(field: "{ weight }")
                    ): Int!
                }

                input ProductDimensionInput @inaccessible {
                    weight: Int!
                }
                """);
        schemaB.Name = "B";
        IEnumerable<MutableSchemaDefinition> schemas = [schemaA, schemaB];
        var merger = new SourceSchemaMerger(
            schemas.ToImmutableSortedSet(
                new SchemaByNameComparer<MutableSchemaDefinition>()));

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Types.ContainsName("ProductDimensionInput"));
    }
}
