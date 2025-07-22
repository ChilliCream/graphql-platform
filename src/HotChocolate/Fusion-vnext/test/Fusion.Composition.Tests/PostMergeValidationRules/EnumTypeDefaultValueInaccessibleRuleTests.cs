using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class EnumTypeDefaultValueInaccessibleRuleTests
{
    private static readonly object s_rule = new EnumTypeDefaultValueInaccessibleRule();
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
        Assert.True(_log.All(e => e.Code == "ENUM_TYPE_DEFAULT_VALUE_INACCESSIBLE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example the "FOO" value in the "Enum1" enum is not marked with @inaccessible,
            // hence it does not violate the rule.
            {
                [
                    """
                    # Schema A
                    type Query {
                        field(type: Enum1 = FOO): [Baz!]!
                    }

                    enum Enum1 {
                        FOO
                        BAR
                    }
                    """
                ]
            },
            // Non-nullable default values.
            {
                [
                    """
                    # Schema A
                    type Query {
                        field1(type: Enum1! = FOO): [Baz!]!
                        field2(type: [Int!]! = [1]): [Baz!]!
                        field3(type: Input1! = { field1: 1 }): [Baz!]!
                    }

                    enum Enum1 {
                        FOO
                        BAR
                    }

                    input Input1 {
                        field1: Int!
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
            // The following example violates this rule because the default value for the argument
            // (arg) and the input field (field) references an enum value (FOO), that is marked as
            // @inaccessible.
            {
                [
                    """
                    # Schema A
                    type Query {
                        field(arg: Enum1 = FOO): [Baz!]!
                    }

                    input Input1 {
                        field: Enum1 = FOO
                    }

                    enum Enum1 {
                        FOO @inaccessible
                        BAR
                    }
                    """
                ],
                [
                    "The default value of 'Query.field(arg:)' references the inaccessible enum "
                    + "value 'Enum1.FOO'.",

                    "The default value of 'Input1.field' references the inaccessible enum value "
                    + "'Enum1.FOO'."
                ]
            },
            // The following example violates this rule because the default value for the argument
            // (arg) and the input field (field2) references an @inaccessible enum value (FOO)
            // within an object value.
            {
                [
                    """
                    # Schema A
                    type Query {
                        field(arg: Input1 = { field1: FOO }): [Baz!]!
                    }

                    input Input1 {
                        field1: Enum1
                        field2: Input2 = { field3: FOO }
                    }

                    input Input2 {
                        field3: Enum1
                    }

                    enum Enum1 {
                        FOO @inaccessible
                        BAR
                    }
                    """
                ],
                [
                    "The default value of 'Query.field(arg:)' references the inaccessible enum "
                    + "value 'Enum1.FOO'.",

                    "The default value of 'Input1.field2' references the inaccessible enum value "
                    + "'Enum1.FOO'."
                ]
            },
            // The following example violates this rule because the default value for the argument
            // (arg) and the input field (field) references an @inaccessible enum value (FOO) within
            // a list.
            {
                [
                    """
                    # Schema A
                    type Query {
                        field(arg: [Enum1] = [FOO]): [Baz!]!
                    }

                    input Input1 {
                        field: [Enum1] = [FOO]
                    }

                    enum Enum1 {
                        FOO @inaccessible
                        BAR
                    }
                    """
                ],
                [
                    "The default value of 'Query.field(arg:)' references the inaccessible enum "
                    + "value 'Enum1.FOO'.",

                    "The default value of 'Input1.field' references the inaccessible enum value "
                    + "'Enum1.FOO'."
                ]
            }
        };
    }
}
