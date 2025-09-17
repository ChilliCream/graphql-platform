using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class FieldWithMissingRequiredArgumentRuleTests
{
    private static readonly object s_rule = new FieldWithMissingRequiredArgumentRule();
    private static readonly ImmutableArray<object> s_rules = [s_rule];
    private readonly CompositionLog _log = new();

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var validator = new PreMergeValidator(schemas, s_rules, _log);

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
        var validator = new PreMergeValidator(schemas, s_rules, _log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, _log.Select(e => e.Message).ToArray());
        Assert.True(_log.All(e => e.Code == "FIELD_WITH_MISSING_REQUIRED_ARGUMENT"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // All schemas agree on having a required argument "author" for the "books" field.
            {
                [
                    """
                    # Schema A
                    type Query {
                        books(author: String!): [Book] @shareable
                    }
                    """,
                    """
                    # Schema B
                    type Query {
                        books(author: String!): [Book] @shareable
                    }
                    """
                ]
            },
            // In the following example, the "author" argument on the "books" field in Schema A
            // specifies a dependency on the "author" field in Schema C. The "author" argument on
            // the books field in Schema B is optional. As a result, the composition succeeds;
            // however, the "author" argument will not be included in the composite schema.
            {
                [
                    """
                    # Schema A
                    type Collection {
                        books(author: String! @require(field: "author")): [Book] @shareable
                    }
                    """,
                    """
                    # Schema B
                    type Collection {
                        books(author: String): [Book] @shareable
                    }
                    """,
                    """
                    # Schema C
                    type Collection {
                        author: String!
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
            // In the following example, the "author" argument is required in one schema but not in
            // the other. This will result in a FIELD_WITH_MISSING_REQUIRED_ARGUMENT error.
            {
                [
                    """
                    # Schema A
                    type Query {
                        books(author: String!): [Book] @shareable
                    }
                    """,
                    """
                    # Schema B
                    type Query {
                        books: [Book] @shareable
                    }
                    """
                ],
                [
                    "The argument 'Query.books(author:)' must be defined as required in schema "
                    + "'B'. Arguments marked with @require are treated as non-required."
                ]
            },
            // In the following example, the "author" argument on the "books" field in Schema A
            // specifies a dependency on the "author" field in Schema C. The "author" argument on
            // the "books" field in Schema B is not optional. This will result in a
            // FIELD_WITH_MISSING_REQUIRED_ARGUMENT error.
            {
                [
                    """
                    # Schema A
                    type Collection {
                        books(author: String! @require(field: "author")): [Book] @shareable
                    }
                    """,
                    """
                    # Schema B
                    type Collection {
                        books(author: String!): [Book] @shareable
                    }
                    """,
                    """
                    # Schema C
                    type Collection {
                        author: String!
                    }
                    """
                ],
                [
                    "The argument 'Collection.books(author:)' must be defined as required in "
                    + "schema 'A'. Arguments marked with @require are treated as non-required."
                ]
            }
        };
    }
}
