namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class RootMutationUsedRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new RootMutationUsedRule();

    // Valid example.
    [Fact]
    public void Validate_RootMutationUsed_Succeeds()
    {
        AssertValid(
        [
            """
            schema {
                mutation: Mutation
            }

            type Mutation {
                createProduct(name: String): Product
            }

            type Product {
                id: ID!
                name: String
            }
            """
        ]);
    }

    // The following example violates the rule because "RootMutation" is used as the root mutation
    // type, but a type named "Mutation" is also defined.
    [Fact]
    public void Validate_RootMutationUsedDifferentName_Fails()
    {
        AssertInvalid(
            [
                """
                schema {
                    mutation: RootMutation
                }

                type RootMutation {
                    createProduct(name: String): Product
                }

                type Mutation {
                    deprecatedField: String
                }
                """
            ],
            [
                "The root mutation type in schema 'A' must be named 'Mutation'."
            ]);
    }

    // A type named "Mutation" is not the root mutation type.
    [Fact]
    public void Validate_RootMutationUsedNotRootType_Fails()
    {
        AssertInvalid(
            [
                "scalar Mutation"
            ],
            [
                "The root mutation type in schema 'A' must be named 'Mutation'."
            ]);
    }
}
