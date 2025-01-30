using HotChocolate.Skimmed;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerTests : CompositionTestBase
{
    [Fact]
    public void Merge_WithOperationTypes_SetsOperationTypes()
    {
        // arrange
        var schemas = CreateSchemaDefinitions(
            [
                """
                type Query {
                    foo: String
                }

                type Mutation {
                    bar: String
                }

                type Subscription {
                    baz: String
                }
                """
            ]);

        var merger = new SourceSchemaMerger(schemas);

        // act
        var result = merger.MergeSchemas();

        // assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.QueryType);
        Assert.NotNull(result.Value.MutationType);
        Assert.NotNull(result.Value.SubscriptionType);
    }

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
