namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class QueryRootTypeInaccessibleRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new QueryRootTypeInaccessibleRule();

    // In this example, no @inaccessible annotation is applied to the query root, so the rule is
    // satisfied.
    [Fact]
    public void Validate_QueryRootTypeAccessible_Succeeds()
    {
        AssertValid(
        [
            """
            schema {
                query: Query
            }

            type Query {
                allBooks: [Book]
            }

            type Book {
                id: ID!
                title: String
            }
            """
        ]);
    }

    // Since the schema marks the query root type as @inaccessible, the rule is violated.
    // QUERY_ROOT_TYPE_INACCESSIBLE is raised because a schema’s root query type cannot be hidden
    // from consumers.
    [Fact]
    public void Validate_QueryRootTypeInaccessible_Fails()
    {
        AssertInvalid(
            [
                """
                schema {
                    query: Query
                }

                type Query @inaccessible {
                    allBooks: [Book]
                }

                type Book {
                    id: ID!
                    title: String
                }
                """
            ],
            [
                "The root query type in schema 'A' must be accessible."
            ]);
    }
}
