namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ListSizeDirectiveArgumentRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ListSizeDirectiveArgumentRule();

    [Fact]
    public void Validate_Should_Fail_When_CompatibleDefinitionHasNegativeAssumedSize()
    {
        AssertInvalid(
            [
                """
                type Query {
                    field: [Int] @listSize(assumedSize: -1)
                }

                directive @listSize(assumedSize: Int) on FIELD_DEFINITION
                """
            ],
            [
                """
                {
                    "message": "The argument 'assumedSize' of the @listSize directive on field 'Query.field' in schema 'A' must not be negative (-1).",
                    "code": "INVALID_GRAPHQL",
                    "severity": "Error",
                    "coordinate": "Query.field",
                    "member": "field",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_IncompatibleDefinitionHasNegativeAssumedSize()
    {
        AssertValid(
        [
            """
            type Query {
                field: [Int] @listSize(assumedSize: -1)
            }

            directive @listSize(
                assumedSize: Int
                custom: Boolean
            ) on FIELD_DEFINITION
            """
        ]);
    }

    // GraphQL list input coercion: a single value in a list position is valid and means a
    // one-element list.
    [Fact]
    public void Validate_Should_Succeed_When_SlicingArgumentsIsSingletonString()
    {
        AssertValid(
        [
            """
            type Query {
                field: [Int] @listSize(slicingArguments: "first")
            }

            directive @listSize(slicingArguments: [String!]) on FIELD_DEFINITION
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_SizedFieldsIsSingletonString()
    {
        AssertValid(
        [
            """
            type Query {
                field: [Int] @listSize(sizedFields: "edges")
            }

            directive @listSize(sizedFields: [String!]) on FIELD_DEFINITION
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_SlicingArgumentsIsBareInt()
    {
        AssertInvalid(
            [
                """
                type Query {
                    field: [Int] @listSize(slicingArguments: 1)
                }

                directive @listSize(slicingArguments: [String!]) on FIELD_DEFINITION
                """
            ],
            [
                """
                {
                    "message": "The argument 'slicingArguments' of the @listSize directive on field 'Query.field' in schema 'A' has an invalid value (1).",
                    "code": "INVALID_GRAPHQL",
                    "severity": "Error",
                    "coordinate": "Query.field",
                    "member": "field",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_SlicingArgumentsListHasIntItem()
    {
        AssertInvalid(
            [
                """
                type Query {
                    field: [Int] @listSize(slicingArguments: ["first", 1])
                }

                directive @listSize(slicingArguments: [String!]) on FIELD_DEFINITION
                """
            ],
            [
                """
                {
                    "message": "The argument 'slicingArguments' of the @listSize directive on field 'Query.field' in schema 'A' has an invalid value ([\"first\", 1]).",
                    "code": "INVALID_GRAPHQL",
                    "severity": "Error",
                    "coordinate": "Query.field",
                    "member": "field",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_SizedFieldsListHasIntItem()
    {
        AssertInvalid(
            [
                """
                type Query {
                    field: [Int] @listSize(sizedFields: ["edges", 1])
                }

                directive @listSize(sizedFields: [String!]) on FIELD_DEFINITION
                """
            ],
            [
                """
                {
                    "message": "The argument 'sizedFields' of the @listSize directive on field 'Query.field' in schema 'A' has an invalid value ([\"edges\", 1]).",
                    "code": "INVALID_GRAPHQL",
                    "severity": "Error",
                    "coordinate": "Query.field",
                    "member": "field",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_AssumedSizeIsNotInt()
    {
        AssertInvalid(
            [
                """
                type Query {
                    field: [Int] @listSize(assumedSize: "5")
                }

                directive @listSize(assumedSize: Int) on FIELD_DEFINITION
                """
            ],
            [
                """
                {
                    "message": "The argument 'assumedSize' of the @listSize directive on field 'Query.field' in schema 'A' has an invalid value (\"5\").",
                    "code": "INVALID_GRAPHQL",
                    "severity": "Error",
                    "coordinate": "Query.field",
                    "member": "field",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_RequireOneSlicingArgumentIsNotBoolean()
    {
        AssertInvalid(
            [
                """
                type Query {
                    field: [Int] @listSize(requireOneSlicingArgument: 1)
                }

                directive @listSize(requireOneSlicingArgument: Boolean) on FIELD_DEFINITION
                """
            ],
            [
                """
                {
                    "message": "The argument 'requireOneSlicingArgument' of the @listSize directive on field 'Query.field' in schema 'A' has an invalid value (1).",
                    "code": "INVALID_GRAPHQL",
                    "severity": "Error",
                    "coordinate": "Query.field",
                    "member": "field",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_SlicingArgumentDefaultValueIsNotInt()
    {
        AssertInvalid(
            [
                """
                type Query {
                    field: [Int] @listSize(slicingArgumentDefaultValue: "5")
                }

                directive @listSize(slicingArgumentDefaultValue: Int) on FIELD_DEFINITION
                """
            ],
            [
                """
                {
                    "message": "The argument 'slicingArgumentDefaultValue' of the @listSize directive on field 'Query.field' in schema 'A' has an invalid value (\"5\").",
                    "code": "INVALID_GRAPHQL",
                    "severity": "Error",
                    "coordinate": "Query.field",
                    "member": "field",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
