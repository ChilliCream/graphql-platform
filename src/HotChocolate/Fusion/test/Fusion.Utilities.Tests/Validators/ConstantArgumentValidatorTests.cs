using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion.Validators;

public sealed class ConstantArgumentValidatorTests
{
    [Fact]
    public void Validate_KnownArgumentWithValidValue_NoErrors()
    {
        // arrange
        var field = GetProductField();
        var arguments = new ArgumentNode[] { new("unit", new EnumValueNode("IMPERIAL")) };
        var errors = new List<string>();

        // act
        ConstantArgumentValidator.Validate(arguments, field, errors);

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
        ConstantArgumentValidator.Validate(arguments, field, errors);

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
        ConstantArgumentValidator.Validate(arguments, field, errors);

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
        ConstantArgumentValidator.Validate(arguments, field, errors);

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
        var arguments = Array.Empty<ArgumentNode>();
        var errors = new List<string>();

        // act
        ConstantArgumentValidator.Validate(arguments, field, errors);

        // assert
        var error = Assert.Single(errors);
        Assert.Equal(
            "The required argument 'id' on field 'Query.entity' was not provided.",
            error);
    }

    // An integer literal is a valid value for an ID argument (ID accepts a string or an int).
    [Fact]
    public void Validate_IdArgumentWithIntValue_NoErrors()
    {
        // arrange
        var field = GetEntityField();
        var arguments = new ArgumentNode[] { new("id", new IntValueNode(123)) };
        var errors = new List<string>();

        // act
        ConstantArgumentValidator.Validate(arguments, field, errors);

        // assert
        Assert.Empty(errors);
    }

    // The same argument specified more than once is invalid.
    [Fact]
    public void Validate_DuplicateArgument_ReportsError()
    {
        // arrange
        var field = GetProductField();
        var arguments = new ArgumentNode[]
        {
            new("code", new IntValueNode(1)),
            new("code", new IntValueNode(2))
        };
        var errors = new List<string>();

        // act
        ConstantArgumentValidator.Validate(arguments, field, errors);

        // assert
        var error = Assert.Single(errors);
        Assert.Equal(
            "The argument 'code' on field 'Query.product' must not be specified more than once.",
            error);
    }

    // The same input object field specified more than once is invalid.
    [Fact]
    public void Validate_DuplicateInputObjectField_ReportsError()
    {
        // arrange
        var field = GetSearchField();
        var arguments = new ArgumentNode[]
        {
            new("filter", new ObjectValueNode(
                new ObjectFieldNode("term", new IntValueNode(1)),
                new ObjectFieldNode("term", new IntValueNode(2))))
        };
        var errors = new List<string>();

        // act
        ConstantArgumentValidator.Validate(arguments, field, errors);

        // assert
        var error = Assert.Single(errors);
        Assert.Equal(
            "The input field 'term' on field 'Query.search' must not be specified more than once.",
            error);
    }

    private static MutableObjectTypeDefinition GetQueryType()
    {
        var schema = SchemaParser.Parse(
            """
            type Query {
                entity(id: ID!): String
                product(unit: Unit = METRIC, code: Int): String
                search(filter: Filter): String
            }

            input Filter {
                term: Int
            }

            enum Unit { METRIC IMPERIAL }
            """u8.ToArray());

        return schema.QueryType!;
    }

    private static MutableOutputFieldDefinition GetEntityField()
        => GetQueryType().Fields["entity"];

    private static MutableOutputFieldDefinition GetProductField()
        => GetQueryType().Fields["product"];

    private static MutableOutputFieldDefinition GetSearchField()
        => GetQueryType().Fields["search"];
}
