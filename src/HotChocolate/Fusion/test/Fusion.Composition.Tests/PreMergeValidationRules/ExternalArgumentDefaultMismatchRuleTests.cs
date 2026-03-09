namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class ExternalArgumentDefaultMismatchRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ExternalArgumentDefaultMismatchRule();

    // Here, the "name" field on "Product" is defined in one source schema and marked as @external
    // in another. The argument "language" has the same default value in both source schemas,
    // satisfying the rule.
    [Fact]
    public void Validate_ExternalArgumentDefaultMatch_Succeeds()
    {
        AssertValid(
        [
            """
            type Product {
                name(language: String = "en"): String
            }
            """,
            """
            type Product {
                name(language: String = "en"): String @external
            }
            """
        ]);
    }

    // Here, the "name" field on "Product" is defined with multiple arguments. Both arguments have
    // the same default value in the source schemas, satisfying the rule.
    [Fact]
    public void Validate_ExternalArgumentDefaultMatchMultipleArguments_Succeeds()
    {
        AssertValid(
        [
            """
            type Product {
                name(language: String = "en", localization: String = "sr"): String
            }
            """,
            """
            type Product {
                name(localization: String = "sr", language: String = "en"): String @external
            }
            """,
            """
            type Product {
                name(language: String = "en", localization: String = "sr"): String @external
            }
            """
        ]);
    }

    // Here, the "name" field on "Product" is defined in one source schema and marked as @external
    // in another. The argument "language" has different default values in the two source schemas,
    // violating the rule.
    [Fact]
    public void Validate_ExternalArgumentDefaultMismatch_Fails()
    {
        AssertInvalid(
            [
                """
                type Product {
                    name(language: String = "en"): String
                }
                """,
                """
                type Product {
                    name(language: String = "de"): String @external
                }
                """
            ],
            [
                """
                {
                    "message": "The default value '\"de\"' of external argument 'Product.name(language:)' in schema 'B' differs from the default value of '\"en\"' in schema 'A'.",
                    "code": "EXTERNAL_ARGUMENT_DEFAULT_MISMATCH",
                    "severity": "Error",
                    "coordinate": "Product.name(language:)",
                    "member": "language",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }

    // In the following example, the "name" field on "Product" is defined in one source schema and
    // marked as @external in another. The argument "language" has a default value in the source
    // schema where the field is defined, but it does not have a default value in the source schema
    // where the field is marked as @external, violating the rule.
    [Fact]
    public void Validate_ExternalArgumentDefaultMismatchNoDefaultValueOnExternal_Fails()
    {
        AssertInvalid(
            [
                """
                type Product {
                    name(language: String = "en"): String
                }
                """,
                """
                type Product {
                    name(language: String): String @external
                }
                """
            ],
            [
                """
                {
                    "message": "The default value '(null)' of external argument 'Product.name(language:)' in schema 'B' differs from the default value of '\"en\"' in schema 'A'.",
                    "code": "EXTERNAL_ARGUMENT_DEFAULT_MISMATCH",
                    "severity": "Error",
                    "coordinate": "Product.name(language:)",
                    "member": "language",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }

    // Here, the "name" field on "Product" is defined without a default value for the "language"
    // argument in the non-external source schema, violating the rule.
    [Fact]
    public void Validate_ExternalArgumentDefaultMismatchNoDefaultValueOnDefinition_Fails()
    {
        AssertInvalid(
            [
                """
                type Product {
                    name(language: String): String
                }
                """,
                """
                type Product {
                    name(language: String = "en"): String @external
                }
                """
            ],
            [
                """
                {
                    "message": "The default value '\"en\"' of external argument 'Product.name(language:)' in schema 'B' differs from the default value of '(null)' in schema 'A'.",
                    "code": "EXTERNAL_ARGUMENT_DEFAULT_MISMATCH",
                    "severity": "Error",
                    "coordinate": "Product.name(language:)",
                    "member": "language",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }

    // Here, the "name" field on "Product" is defined with multiple arguments. One argument has a
    // matching default value, whilst the other does not, violating the rule.
    [Fact]
    public void Validate_ExternalArgumentDefaultMismatchMultipleArguments_Fails()
    {
        AssertInvalid(
            [
                """
                type Product {
                    name(language: String = "en", localization: String = "sr"): String
                }
                """,
                """
                type Product {
                    name(language: String = "en", localization: String = "sa"): String @external
                }
                """
            ],
            [
                """
                {
                    "message": "The default value '\"sa\"' of external argument 'Product.name(localization:)' in schema 'B' differs from the default value of '\"sr\"' in schema 'A'.",
                    "code": "EXTERNAL_ARGUMENT_DEFAULT_MISMATCH",
                    "severity": "Error",
                    "coordinate": "Product.name(localization:)",
                    "member": "localization",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }

    // Here, the "name" field on "Product" is defined with multiple arguments. One argument has a
    // matching default value, whilst the other omits the value completely, violating the rule.
    [Fact]
    public void Validate_ExternalArgumentDefaultMismatchNoDefaultValueOn2ndExternalArgument_Fails()
    {
        AssertInvalid(
            [
                """
                type Product {
                    name(language: String = "en", localization: String = "sr"): String
                }
                """,
                """
                type Product {
                    name(language: String = "en", localization: String): String @external
                }
                """
            ],
            [
                """
                {
                    "message": "The default value '(null)' of external argument 'Product.name(localization:)' in schema 'B' differs from the default value of '\"sr\"' in schema 'A'.",
                    "code": "EXTERNAL_ARGUMENT_DEFAULT_MISMATCH",
                    "severity": "Error",
                    "coordinate": "Product.name(localization:)",
                    "member": "localization",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }
}
