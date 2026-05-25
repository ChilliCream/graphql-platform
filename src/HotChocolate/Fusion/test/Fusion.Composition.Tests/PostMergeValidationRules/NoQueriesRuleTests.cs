namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class NoQueriesRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new NoQueriesRule();

    // In this example, at least one schema provides accessible query fields, satisfying the rule.
    [Fact]
    public void Validate_HasQueries_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Query {
                product(id: ID!): Product
            }

            type Product {
                id: ID!
            }
            """,
            """
            # Schema B
            type Query {
                review(id: ID!): Review
            }

            type Review {
                id: ID!
                content: String
                rating: Int
            }
            """
        ]);
    }

    // Even if some query fields are marked as @inaccessible, as long as there is at least one
    // accessible query field in the composed schema, the rule is satisfied.
    [Fact]
    public void Validate_HasAccessibleQueries_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Query {
                internalData: InternalData @inaccessible
            }

            type InternalData {
                secret: String
            }
            """,
            """
            # Schema B
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

    // If all query fields in all schemas are marked as @inaccessible, the composed schema will lack
    // accessible query fields, violating the rule.
    [Fact]
    public void Validate_NoQueries_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Query {
                    internalData: InternalData @inaccessible
                }

                type InternalData {
                    secret: String
                }
                """,
                """
                # Schema B
                type Query {
                    adminStats: AdminStats @inaccessible
                }

                type AdminStats {
                    userCount: Int
                }
                """
            ],
            [
                """
                {
                    "message": "The merged query type has no accessible fields.",
                    "code": "NO_QUERIES",
                    "severity": "Error",
                    "coordinate": "Query",
                    "member": "Query",
                    "schema": "default",
                    "extensions": {}
                }
                """
            ]);
    }
}
