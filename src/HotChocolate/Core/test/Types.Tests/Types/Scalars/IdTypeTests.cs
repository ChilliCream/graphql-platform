using HotChocolate.Language;

namespace HotChocolate.Types;

public class IdTypeTests
{
    [Fact]
    public void Create_With_Default_Name()
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
    public void EnsureStringTypeKindIsCorret()
    {
        // arrange
        var type = new IdType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, type.Kind);
    }

    [Fact]
    public void IsInstanceOfType_StringValueNode()
    {
        // arrange
        var type = new IdType();
        var input = new StringValueNode("123456");

        // act
        var result = type.IsInstanceOfType(input);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_IntValueNode()
    {
        // arrange
        var type = new IdType();
        var input = new IntValueNode(123456);

        // act
        var result = type.IsInstanceOfType(input);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_NullValueNode()
    {
        // arrange
        var type = new IdType();
        var input = NullValueNode.Default;

        // act
        var result = type.IsInstanceOfType(input);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_Wrong_ValueNode()
    {
        // arrange
        var type = new IdType();
        var input = new FloatValueNode(123456.0);

        // act
        var result = type.IsInstanceOfType(input);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void IsInstanceOfType_Null_Throws()
    {
        // arrange
        var type = new IdType();

        // act
        // assert
        Assert.Throws<ArgumentNullException>(
            () => type.IsInstanceOfType(null));
    }

    [Fact]
    public void Serialize_String()
    {
        // arrange
        var type = new IdType();
        var input = "123456";

        // act
        var serializedValue = type.Serialize(input);

        // assert
        Assert.IsType<string>(serializedValue);
        Assert.Equal("123456", serializedValue);
    }

    [Fact]
    public void Serialize_Null()
    {
        // arrange
        var type = new IdType();

        // act
        var serializedValue = type.Serialize(null);

        // assert
        Assert.Null(serializedValue);
    }

    [Fact]
    public void Deserialize_String()
    {
        // arrange
        var type = new IdType();
        var serialized = "123456";

        // act
        var success = type.TryDeserialize(serialized, out var value);

        // assert
        Assert.True(success);
        Assert.Equal("123456", Assert.IsType<string>(value));
    }

    [Fact]
    public void Deserialize_Int()
    {
        // arrange
        var type = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("QueryRoot")
                .Field("abc")
                .Type<IdType>()
                .Resolve("abc"))
            .Create()
            .GetType<IdType>("ID");
        var serialized = 123456;

        // act
        var success = type.TryDeserialize(serialized, out var value);

        // assert
        Assert.True(success);
        Assert.Equal("123456", Assert.IsType<string>(value));
    }

    [Fact]
    public void Deserialize_Null()
    {
        // arrange
        var type = new IdType();
        object serialized = null;

        // act
        var success = type.TryDeserialize(serialized, out var value);

        // assert
        Assert.Null(value);
    }

    [Fact]
    public void Deserialize_Float()
    {
        // arrange
        var type = new IdType();
        var serialized = 1.1f;

        // act
        var success = type.TryDeserialize(serialized, out var value);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void Serialize_Wrong_Type_Throws()
    {
        // arrange
        var type = new IdType();
        object input = Guid.NewGuid();

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => type.Serialize(input));
    }

    [Fact]
    public void ParseLiteral_StringValueNode()
    {
        // arrange
        var type = new IdType();
        var input = new StringValueNode("123456");

        // act
        var output = type.ParseLiteral(input);

        // assert
        Assert.IsType<string>(output);
        Assert.Equal("123456", output);
    }

    [Fact]
    public void ParseLiteral_IntValueNode()
    {
        // arrange
        var type = new IdType();
        var input = new IntValueNode(123456);

        // act
        var output = type.ParseLiteral(input);

        // assert
        Assert.IsType<string>(output);
        Assert.Equal("123456", output);
    }

    [Fact]
    public void ParseLiteral_NullValueNode()
    {
        // arrange
        var type = new IdType();
        var input = NullValueNode.Default;

        // act
        var output = type.ParseLiteral(input);

        // assert
        Assert.Null(output);
    }

    [Fact]
    public void ParseLiteral_Wrong_ValueNode_Throws()
    {
        // arrange
        var type = new IdType();
        var input = new FloatValueNode(123456.0);

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => type.ParseLiteral(input));
    }

    [Fact]
    public void ParseLiteral_Null_Throws()
    {
        // arrange
        var type = new IdType();

        // act
        // assert
        Assert.Throws<ArgumentNullException>(() =>
            type.ParseLiteral(null));
    }

    [Fact]
    public void ParseValue_Wrong_Value_Throws()
    {
        // arrange
        var type = new IdType();
        object input = 123.456;

        // act
        // assert
        Assert.Throws<SerializationException>(
            () => type.ParseValue(input));
    }

    [Fact]
    public void ParseValue_Null()
    {
        // arrange
        var type = new IdType();
        object input = null;

        // act
        object output = type.ParseValue(input);

        // assert
        Assert.IsType<NullValueNode>(output);
    }

    [Fact]
    public void ParseValue_String()
    {
        // arrange
        var type = new IdType();
        object input = "hello";

        // act
        object output = type.ParseValue(input);

        // assert
        Assert.IsType<StringValueNode>(output);
    }
}
