using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ProvidesInvalidFieldsTypeRuleTests
{
    private static readonly object s_rule = new ProvidesInvalidFieldsTypeRule();
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
        Assert.True(_log.All(e => e.Code == "PROVIDES_INVALID_FIELDS_TYPE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, the @provides directive on "details" uses the string
            // "features specifications" to specify that both fields are provided in the child type
            // "ProductDetails".
            {
                [
                    """
                    type Product {
                        id: ID!
                        details: ProductDetails @provides(fields: "features specifications")
                    }

                    type ProductDetails {
                        features: [String]
                        specifications: String
                    }

                    type Query {
                        products: [Product]
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
            // Here, the @provides directive includes a numeric value (123) instead of a string in
            // its "fields" argument. This invalid usage raises a PROVIDES_INVALID_FIELDS_TYPE
            // error.
            {
                [
                    """
                    type Product {
                        id: ID!
                        details: ProductDetails @provides(fields: 123)
                    }

                    type ProductDetails {
                        features: [String]
                        specifications: String
                    }
                    """
                ],
                [
                    "The @provides directive on field 'Product.details' in schema 'A' must "
                    + "specify a string value for the 'fields' argument."
                ]
            }
        };
    }
}
