using System.Collections.Immutable;
using HotChocolate.Fusion.Comparers;
using HotChocolate.Fusion.Logging;
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
        var queryType = new MutableObjectTypeDefinition(Query);
        queryType.Fields.Add(
            new MutableOutputFieldDefinition("field", intType)
            {
                DeclaringMember = queryType
            });
        var mutationType = new MutableObjectTypeDefinition(Mutation);
        mutationType.Fields.Add(
            new MutableOutputFieldDefinition("field", intType)
            {
                DeclaringMember = mutationType
            });
        var subscriptionType = new MutableObjectTypeDefinition(Subscription);
        subscriptionType.Fields.Add(
            new MutableOutputFieldDefinition("field", intType)
            {
                DeclaringMember = subscriptionType
            });
        var schema = new MutableSchemaDefinition
        {
            Types =
            {
                queryType,
                mutationType,
                subscriptionType
            }
        };
        new SourceSchemaEnricher(schema, [schema]).Enrich();
        var merger = new SourceSchemaMerger([schema]);

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
        var queryType = new MutableObjectTypeDefinition(Query);
        queryType.Fields.Add(
            new MutableOutputFieldDefinition("field", BuiltIns.Int.Create())
            {
                DeclaringMember = queryType
            });
        var schema = new MutableSchemaDefinition
        {
            Types =
            {
                queryType,
                new MutableObjectTypeDefinition(Mutation),
                new MutableObjectTypeDefinition(Subscription)
            }
        };
        new SourceSchemaEnricher(schema, [schema]).Enrich();
        var merger = new SourceSchemaMerger([schema]);

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
        var sourceSchemaTextA =
            new SourceSchemaText(
                "A",
                """
                type Query {
                    product: Product
                }

                type Product {
                    weight: Int!
                }
                """);
        var sourceSchemaTextB =
            new SourceSchemaText(
                "B",
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
        var compositionLog = new CompositionLog();
        var sourceSchemaParser1 = new SourceSchemaParser(sourceSchemaTextA, compositionLog);
        var sourceSchemaParser2 = new SourceSchemaParser(sourceSchemaTextB, compositionLog);
        var schema1 = sourceSchemaParser1.Parse().Value;
        var schema2 = sourceSchemaParser2.Parse().Value;
        var schemas =
            ImmutableSortedSet.Create(
                new SchemaByNameComparer<MutableSchemaDefinition>(), schema1, schema2);
        new SourceSchemaEnricher(schema1, schemas).Enrich();
        new SourceSchemaEnricher(schema2, schemas).Enrich();
        var merger = new SourceSchemaMerger(schemas);

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Types.ContainsName("ProductDimensionInput"));
    }
}
