namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class LookupReturnsNonNullableTypeRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new LookupReturnsNonNullableTypeRule();

    // In this example, "userById" returns a nullable "User" type, aligning with the recommendation.
    [Fact]
    public void Validate_LookupReturnsNullableType_Succeeds()
    {
        AssertValid(
        [
            """
            type Query {
                userById(id: ID!): User @lookup
            }

            type User {
                id: ID!
                name: String
            }
            """
        ]);
    }

    // Here, "userById" returns a non-nullable "User!", which does not align with the recommendation
    // that a @lookup field should have a nullable return type.
    [Fact]
    public void Validate_LookupReturnsNonNullableType_Fails()
    {
        AssertInvalid(
            [
                """
                type Query {
                    userById(id: ID!): User! @lookup
                }

                type User {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                "The lookup field 'Query.userById' in schema 'A' should return a nullable type."
            ]);
    }
}
