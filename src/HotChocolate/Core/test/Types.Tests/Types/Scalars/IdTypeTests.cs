using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

public class IdTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new IdType();

        // assert
        Assert.Equal(ScalarNames.ID, type.Name);
    }

    [Fact]
    public void Create_With_Name()
    {
        // arrange
        // act
        var type = new IdType("Foo");

        // assert
        Assert.Equal("Foo", type.Name);
    }

    [Fact]
    public void Create_With_Name_And_Description()
    {
        // arrange
        // act
        var type = new IdType("Foo", "Bar");

        // assert
        Assert.Equal("Foo", type.Name);
        Assert.Equal("Bar", type.Description);
    }

    [Fact]
    public void Ensure_TypeKind_Is_Scalar()
    {
        // arrange
        var type = new IdType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, kind);
    }

    [Fact]
    public void IsValueCompatible_StringValueNode_True()
    {
        // arrange
        var type = new IdType();
        var input = new StringValueNode("123456");

        // act
        var result = type.IsValueCompatible(input);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsValueCompatible_IntValueNode_True()
    {
        // arrange
        var type = new IdType();
        var input = new IntValueNode(123456);

        // act
        var result = type.IsValueCompatible(input);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsValueCompatible_FloatValueNode_False()
    {
        // arrange
        var type = new IdType();
        var input = new FloatValueNode(123456.0);

        // act
        var result = type.IsValueCompatible(input);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void IsValueCompatible_Null_ReturnsFalse()
    {
        // arrange
        var type = new IdType();

        // act
        var result = type.IsValueCompatible(null!);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void CoerceInputLiteral_StringValueNode()
    {
        // arrange
        var type = new IdType();
        var input = new StringValueNode("123456");

        // act
        var output = type.CoerceInputLiteral(input);

        // assert
        Assert.IsType<string>(output);
        Assert.Equal("123456", output);
    }

    [Fact]
    public void CoerceInputLiteral_IntValueNode()
    {
        // arrange
        var type = new IdType();
        var input = new IntValueNode(123456);

        // act
        var output = type.CoerceInputLiteral(input);

        // assert
        Assert.IsType<string>(output);
        Assert.Equal("123456", output);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Format()
    {
        // arrange
        var type = new IdType();
        var input = new FloatValueNode(123456.0);

        // act
        void Action() => type.CoerceInputLiteral(input);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputLiteral_Null_Throws()
    {
        // arrange
        var type = new IdType();

        // act
        void Action() => type.CoerceInputLiteral(null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputValue_String()
    {
        // arrange
        var type = new IdType();
        var inputValue = JsonDocument.Parse("\"123456\"").RootElement;

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal("123456", runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_Int()
    {
        // arrange
        var type = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("QueryRoot")
                .Field("abc")
                .Type<IdType>()
                .Resolve("abc"))
            .Create()
            .Types
            .GetType<IdType>("ID");
        var inputValue = JsonDocument.Parse("123456").RootElement;

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal("123456", runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_Null()
    {
        // arrange
        var type = new IdType();
        var inputValue = JsonDocument.Parse("null").RootElement;

        // act
        void Action() => type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputValue_Invalid_Format()
    {
        // arrange
        var type = new IdType();
        var inputValue = JsonDocument.Parse("1.1").RootElement;

        // act
        void Action() => type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceOutputValue()
    {
        // arrange
        var type = new IdType();
        const string runtimeValue = "123456";

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(runtimeValue, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("\"123456\"");
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Format()
    {
        // arrange
        var type = new IdType();
        var input = Guid.NewGuid();

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        void Action() => type.CoerceOutputValue(input, resultValue);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void ValueToLiteral()
    {
        // arrange
        var type = new IdType();
        const string runtimeValue = "hello";

        // act
        var literal = type.ValueToLiteral(runtimeValue);

        // assert
        Assert.Equal("hello", Assert.IsType<StringValueNode>(literal).Value);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new IdType();
        var literal = new StringValueNode("123456");

        // act
        var runtimeValue = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal("123456", Assert.IsType<string>(runtimeValue));
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new IdType();

        // act
        void Action() => type.CoerceInputLiteral(new FloatValueNode(123.456));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }
}
