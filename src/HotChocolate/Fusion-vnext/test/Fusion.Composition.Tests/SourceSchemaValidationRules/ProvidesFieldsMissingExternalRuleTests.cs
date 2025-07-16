using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ProvidesFieldsMissingExternalRuleTests
{
    private static readonly object s_rule = new ProvidesFieldsMissingExternalRule();
    private static readonly ImmutableArray<object> s_rules = [s_rule];
    private readonly CompositionLog _log = new();

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var validator = new SourceSchemaValidator(schemas, s_rules, _log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(_log.IsEmpty);
    }

    [Theory]
    [MemberData(nameof(InvalidExamplesData))]
    public void Examples_Invalid(string[] sdl, string[] errorMessages)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var validator = new SourceSchemaValidator(schemas, s_rules, _log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, _log.Select(e => e.Message).ToArray());
        Assert.True(_log.All(e => e.Code == "PROVIDES_FIELDS_MISSING_EXTERNAL"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // Here, the "Order" type from this schema is providing fields on "User" through
            // @provides. The "name" field of "User" is not defined in this schema; it is declared
            // with @external indicating that the "name" field comes from elsewhere. Thus,
            // referencing "name" under @provides(fields: "name") is valid.
            {
                [
                    """
                    type Order {
                        id: ID!
                        customer: User @provides(fields: "name")
                    }

                    type User @key(fields: "id") {
                        id: ID!
                        name: String @external
                    }
                    """
                ]
            }
        };
    }

    public static TheoryData<string[], string[]> InvalidExamplesData()
    {
        return new TheoryData<string[], string[]>
        {
            // In this example, "User.address" is not marked as @external in the same schema that
            // applies @provides. This means the schema already provides the "address" field in all
            // possible paths, so using @provides(fields: "address") is invalid.
            {
                [
                    """
                    type User {
                        id: ID!
                        address: String
                    }

                    type Order {
                        id: ID!
                        buyer: User @provides(fields: "address")
                    }
                    """
                ],
                [
                    "The @provides directive on field 'Order.buyer' in schema 'A' references "
                    + "field 'User.address', which must be marked as external."
                ]
            },
            // Nested field.
            {
                [
                    """
                    type User {
                        id: ID!
                        info: UserInfo
                    }

                    type UserInfo {
                        address: String
                    }

                    type Order {
                        id: ID!
                        buyer: User @provides(fields: "info { address }")
                    }
                    """
                ],
                [
                    "The @provides directive on field 'Order.buyer' in schema 'A' references "
                    + "field 'User.info', which must be marked as external.",

                    "The @provides directive on field 'Order.buyer' in schema 'A' references "
                    + "field 'UserInfo.address', which must be marked as external."
                ]
            }
        };
    }
}
