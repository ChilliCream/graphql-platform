using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion.Validators;

public sealed class ConstantArgumentValidatorTests
{
    private static IComplexTypeDefinition GetQueryType()
    {
        var schema = SchemaParser.Parse(
            """
            type Query {
                entity(id: ID!): String
                product(unit: Unit = METRIC, code: Int): String
            }

            enum Unit { METRIC IMPERIAL }
            """u8.ToArray());

        return schema.QueryType!;
    }

    private static IOutputFieldDefinition GetEntityField()
        => GetQueryType().Fields["entity"];

    private static IOutputFieldDefinition GetProductField()
        => GetQueryType().Fields["product"];

    [Fact]
    public void Validate_KnownArgumentWithValidValue_NoErrors()
    {
        // arrange
        var field = GetProductField();
        var arguments = new ArgumentNode[] { new("unit", new EnumValueNode("IMPERIAL")) };
        var errors = new List<string>();

        // act
        ConstantArgumentValidator.Validate(arguments, field, "Query.product", errors);

        // assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_UnknownArgument_ReportsError()
    {
        // arrange
        var field = GetProductField();
        var arguments = new ArgumentNode[] { new("nope", new IntValueNode(1)) };
        var errors = new List<string>();

        // act
        ConstantArgumentValidator.Validate(arguments, field, "Query.product", errors);

        // assert
        var error = Assert.Single(errors);
        Assert.Equal("The argument 'nope' does not exist on field 'Query.product'.", error);
    }

    [Fact]
    public void Validate_IncompatibleValue_ReportsError()
    {
        // arrange
        var field = GetProductField();
        var arguments = new ArgumentNode[] { new("code", new StringValueNode("abc")) };
        var errors = new List<string>();

        // act
        ConstantArgumentValidator.Validate(arguments, field, "Query.product", errors);

        // assert
        var error = Assert.Single(errors);
        Assert.Equal(
            "The value provided for argument 'code' on field 'Query.product' is not compatible with the type 'Int'.",
            error);
    }

    [Fact]
    public void Validate_InvalidEnumValue_ReportsError()
    {
        // arrange
        var field = GetProductField();
        var arguments = new ArgumentNode[] { new("unit", new EnumValueNode("PARSEC")) };
        var errors = new List<string>();

        // act
        ConstantArgumentValidator.Validate(arguments, field, "Query.product", errors);

        // assert
        var error = Assert.Single(errors);
        Assert.Equal(
            "The value provided for argument 'unit' on field 'Query.product' is not compatible with the type 'Unit'.",
            error);
    }

    [Fact]
    public void Validate_MissingRequiredArgument_ReportsError()
    {
        // arrange
        var field = GetEntityField();
        var arguments = System.Array.Empty<ArgumentNode>();
        var errors = new List<string>();

        // act
        ConstantArgumentValidator.Validate(arguments, field, "Query.entity", errors);

        // assert
        var error = Assert.Single(errors);
        Assert.Equal(
            "The required argument 'id' on field 'Query.entity' was not provided.",
            error);
    }
}
