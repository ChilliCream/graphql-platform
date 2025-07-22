using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class InterfaceFieldNoImplementationRuleTests
{
    private static readonly object s_rule = new InterfaceFieldNoImplementationRule();
    private static readonly ImmutableArray<object> s_rules = [s_rule];
    private readonly CompositionLog _log = new();

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var merger = new SourceSchemaMerger(
            schemas,
            new SourceSchemaMergerOptions { RemoveUnreferencedTypes = false });
        var mergeResult = merger.Merge();
        var validator = new PostMergeValidator(mergeResult.Value, s_rules, schemas, _log);

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
        var merger = new SourceSchemaMerger(
            schemas,
            new SourceSchemaMergerOptions { RemoveUnreferencedTypes = false });
        var mergeResult = merger.Merge();
        var validator = new PostMergeValidator(mergeResult.Value, s_rules, schemas, _log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, _log.Select(e => e.Message).ToArray());
        Assert.True(_log.All(e => e.Code == "INTERFACE_FIELD_NO_IMPLEMENTATION"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, the "User" interface has three fields: "id", "name", and "email".
            // Both the "RegisteredUser" and "GuestUser" types implement all three fields,
            // satisfying the interface contract.
            {
                [
                    """
                    # Schema A
                    interface User {
                        id: ID!
                        name: String!
                        email: String
                    }

                    type RegisteredUser implements User {
                        id: ID!
                        name: String!
                        email: String
                        lastLogin: DateTime
                    }
                    """,
                    """
                    # Schema B
                    interface User {
                        id: ID!
                        name: String!
                        email: String
                    }

                    type GuestUser implements User {
                        id: ID!
                        name: String!
                        email: String
                        temporaryCartId: String
                    }
                    """
                ]
            },
            // Here, the "email" field on the "User" interface is marked as @inaccessible, so it
            // does not need to be implemented by the "GuestUser" type.
            {
                [
                    """
                    # Schema A
                    interface User {
                        id: ID!
                        name: String!
                        email: String @inaccessible
                    }

                    type RegisteredUser implements User {
                        id: ID!
                        name: String!
                        email: String
                        lastLogin: DateTime
                    }
                    """,
                    """
                    # Schema B
                    interface User {
                        id: ID!
                        name: String!
                    }

                    type GuestUser implements User {
                        id: ID!
                        name: String!
                        temporaryCartId: String
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
            // In this example, the "User" interface is defined with three fields, but the
            // "GuestUser" type omits one of them ("email"), causing an
            // INTERFACE_FIELD_NO_IMPLEMENTATION error.
            {
                [
                    """
                    # Schema A
                    interface User {
                        id: ID!
                        name: String!
                        email: String
                    }

                    type RegisteredUser implements User {
                        id: ID!
                        name: String!
                        email: String
                        lastLogin: DateTime
                    }
                    """,
                    """
                    # Schema B
                    interface User {
                        id: ID!
                        name: String!
                    }

                    type GuestUser implements User {
                        id: ID!
                        name: String!
                        temporaryCartId: String
                    }
                    """
                ],
                [
                    "The merged object type 'GuestUser' must implement the field 'email' on "
                    + "interface 'User'."
                ]
            }
        };
    }
}
