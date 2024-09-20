using HotChocolate.Language;

namespace HotChocolate.Types;

public class BooleanTypeTests
{
    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var literal = new BooleanValueNode(null, true);

        // act
        var booleanType = new BooleanType();
        var result = booleanType.ParseLiteral(literal);

        // assert
        Assert.IsType<bool>(result);
        Assert.True((bool)result);
    }

    [Fact]
    public void IsInstanceOfType()
    {
        // arrange
        var boolLiteral = new BooleanValueNode(null, true);
        var stringLiteral = new StringValueNode(null, "12345", false);
        var nullLiteral = NullValueNode.Default;

        // act
        var booleanType = new BooleanType();
        var isIntLiteralInstanceOf = booleanType.IsInstanceOfType(boolLiteral);
        var isStringLiteralInstanceOf = booleanType.IsInstanceOfType(stringLiteral);
        var isNullLiteralInstanceOf = booleanType.IsInstanceOfType(nullLiteral);

        // assert
        Assert.True(isIntLiteralInstanceOf);
        Assert.False(isStringLiteralInstanceOf);
        Assert.True(isNullLiteralInstanceOf);
    }

    [Fact]
    public void EnsureBooleanTypeKindIsCorret()
    {
        // arrange
        var type = new BooleanType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, type.Kind);
    }

    [Fact]
    public void Serialize_Null_Null()
    {
        // arrange
        var booleanType = new BooleanType();

        // act
        var result = booleanType.Serialize(null);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void Serialize_True_True()
    {
        // arrange
        var booleanType = new BooleanType();

        // act
        var result = booleanType.Serialize(true);

        // assert
        Assert.IsType<bool>(result);
        Assert.True((bool)result);
    }

    [Fact]
    public void Serialize_String_Exception()
    {
        // arrange
        var booleanType = new BooleanType();

        // act
        Action a = () => booleanType.Serialize("foo");

        // assert
        Assert.Throws<SerializationException>(a);
    }

    [Fact]
    public void Deserialize_Null_Null()
    {
        // arrange
        var booleanType = new BooleanType();

        // act
        var result = booleanType.Serialize(null);

        // assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_True_True()
    {
        // arrange
        var booleanType = new BooleanType();

        // act
        var result = booleanType.Serialize(true);

        // assert
        Assert.IsType<bool>(result);
        Assert.True((bool)result);
    }

    [Fact]
    public void Deserialize_String_Exception()
    {
        // arrange
        var booleanType = new BooleanType();

        // act
        Action a = () => booleanType.Serialize("foo");

        // assert
        Assert.Throws<SerializationException>(a);
    }
}
