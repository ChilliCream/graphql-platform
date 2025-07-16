using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class ImplementedByInaccessibleRuleTests
{
    private static readonly object s_rule = new ImplementedByInaccessibleRule();
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
        Assert.True(_log.All(e => e.Code == "IMPLEMENTED_BY_INACCESSIBLE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In the following example, "User.id" is accessible and implements "Node.id" which is
            // also accessible, no error occurs.
            {
                [
                    """
                    interface Node {
                        id: ID!
                    }

                    type User implements Node {
                        id: ID!
                        name: String
                    }
                    """
                ]
            },
            // Since "Auditable" and its field "lastAudit" are @inaccessible, the "Order.lastAudit"
            // field is allowed to be @inaccessible because it does not implement any visible
            // interface field in the composed schema.
            {
                [
                    """
                    interface Auditable @inaccessible {
                        lastAudit: DateTime!
                    }

                    type Order implements Auditable {
                        lastAudit: DateTime! @inaccessible
                        orderNumber: String
                    }
                    """
                ]
            },
            // Accessible interface field "User.id" implementing accessible field "Node.id" in
            // another interface.
            {
                [
                    """
                    interface Node {
                        id: ID!
                    }

                    interface User implements Node {
                        id: ID!
                        name: String
                    }
                    """
                ]
            },
            // Inaccessible interface field "Order.lastAudit" implementing inaccessible field
            // "Auditable.lastAudit" in another interface.
            {
                [
                    """
                    interface Auditable @inaccessible {
                        lastAudit: DateTime!
                    }

                    interface Order implements Auditable {
                        lastAudit: DateTime! @inaccessible
                        orderNumber: String
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
            // In this example, "Node.id" is visible in the public schema (no @inaccessible), but
            // "User.id" is marked @inaccessible. This violates the interface contract because
            // "User" claims to implement "Node", yet does not expose the "id" field to the public
            // schema.
            {
                [
                    """
                    interface Node {
                        id: ID!
                    }

                    type User implements Node {
                        id: ID! @inaccessible
                        name: String
                    }
                    """
                ],
                [
                    "The field 'User.id' implementing interface field 'Node.id' is inaccessible "
                    + "in the composed schema."
                ]
            },
            // Same as above, for an interface type.
            {
                [
                    """
                    interface Node {
                        id: ID!
                    }

                    interface User implements Node {
                        id: ID! @inaccessible
                        name: String
                    }
                    """
                ],
                [
                    "The field 'User.id' implementing interface field 'Node.id' is inaccessible "
                    + "in the composed schema."
                ]
            }
        };
    }
}
