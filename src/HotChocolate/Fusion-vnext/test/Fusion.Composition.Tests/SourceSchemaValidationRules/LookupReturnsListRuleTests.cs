namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class LookupReturnsListRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new LookupReturnsListRule();

    // In this example, "userById" returns a "User" object, satisfying the requirement.
    [Fact]
    public void Validate_LookupDoesNotReturnList_Succeeds()
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

    // Here, "usersByIds" returns a list of "User" objects, which violates the requirement that a
    // @lookup field must return a single object.
    [Fact]
    public void Validate_LookupReturnsList_Fails()
    {
        AssertInvalid(
            [
                """
                type Query {
                    usersByIds(ids: [ID!]!): [User!] @lookup
                }

                type User {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The lookup field 'Query.usersByIds' in schema 'A' must not return a list.",
                    "code": "LOOKUP_RETURNS_LIST",
                    "severity": "Error",
                    "coordinate": "Query.usersByIds",
                    "member": "usersByIds",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // Non-null list.
    [Fact]
    public void Validate_LookupReturnsNonNullList_Fails()
    {
        AssertInvalid(
            [
                """
                type Query {
                    usersByIds(ids: [ID!]!): [User!]! @lookup
                }

                type User {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The lookup field 'Query.usersByIds' in schema 'A' must not return a list.",
                    "code": "LOOKUP_RETURNS_LIST",
                    "severity": "Error",
                    "coordinate": "Query.usersByIds",
                    "member": "usersByIds",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
