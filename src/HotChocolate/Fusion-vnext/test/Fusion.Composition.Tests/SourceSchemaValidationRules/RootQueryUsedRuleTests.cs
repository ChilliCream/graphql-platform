namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class RootQueryUsedRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new RootQueryUsedRule();

    // Valid example.
    [Fact]
    public void Validate_RootQueryUsed_Succeeds()
    {
        AssertValid(
        [
            """
            schema {
                query: Query
            }

            type Query {
                product(id: ID!): Product
            }

            type Product {
                id: ID!
                name: String
            }
            """
        ]);
    }

    // The following example violates the rule because "RootQuery" is used as the root query type,
    // but a type named "Query" is also defined.
    [Fact]
    public void Validate_RootQueryUsedDifferentName_Fails()
    {
        AssertInvalid(
            [
                """
                schema {
                    query: RootQuery
                }

                type RootQuery {
                    product(id: ID!): Product
                }

                type Query {
                    deprecatedField: String
                }
                """
            ],
            [
                "The root query type in schema 'A' must be named 'Query'."
            ]);
    }

    // A type named "Query" is not the root query type.
    [Fact]
    public void Validate_RootQueryUsedNotRootType_Fails()
    {
        AssertInvalid(
            [
                "scalar Query"
            ],
            [
                "The root query type in schema 'A' must be named 'Query'."
            ]);
    }
}
