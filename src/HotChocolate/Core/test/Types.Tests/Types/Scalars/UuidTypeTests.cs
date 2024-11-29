using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types;

public class UuidTypeTests
{
    [Fact]
    public void IsInstanceOfType_StringLiteral()
    {
        // arrange
        var uuidType = new UuidType();
        var guid = Guid.NewGuid();

        // act
        var isOfType = uuidType.IsInstanceOfType(guid);

        // assert
        Assert.True(isOfType);
    }

    [Fact]
    public void IsInstanceOfType_NullLiteral()
    {
        // arrange
        var uuidType = new UuidType();
        var literal = new NullValueNode(null);

        // act
        var isOfType = uuidType.IsInstanceOfType(literal);

        // assert
        Assert.True(isOfType);
    }

    [Fact]
    public void IsInstanceOfType_IntLiteral()
    {
        // arrange
        var uuidType = new UuidType();
        var literal = new IntValueNode(123);

        // act
        var isOfType = uuidType.IsInstanceOfType(literal);

        // assert
        Assert.False(isOfType);
    }

    [Fact]
    public void IsInstanceOfType_Null()
    {
        // arrange
        var uuidType = new UuidType();

        // act
        void Action() => uuidType.IsInstanceOfType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Serialize_Guid()
    {
        // arrange
        var uuidType = new UuidType();
        var guid = Guid.NewGuid();

        // act
        var serializedValue = uuidType.Serialize(guid);

        // assert
        Assert.Equal(guid.ToString("D"), Assert.IsType<string>(serializedValue));
    }

    [Fact]
    public void Serialize_Null()
    {
        // arrange
        var uuidType = new UuidType();

        // act
        var serializedValue = uuidType.Serialize(null);

        // assert
        Assert.Null(serializedValue);
    }

    [Fact]
    public void Serialize_Int()
    {
        // arrange
        var uuidType = new UuidType();
        var value = 123;

        // act
        void Action() => uuidType.Serialize(value);

        // assert
        Assert.Throws<SerializationException>(Action);
    }

    [Fact]
    public void Deserialize_Null()
    {
        // arrange
        var uuidType = new UuidType();

        // act
        var success = uuidType.TryDeserialize(null, out var o);

        // assert
        Assert.True(success);
        Assert.Null(o);
    }

    [Fact]
    public void Deserialize_String()
    {
        // arrange
        var uuidType = new UuidType();
        var guid = Guid.NewGuid();

        // act
        var success = uuidType.TryDeserialize(guid.ToString("N"), out var o);

        // assert
        Assert.True(success);
        Assert.Equal(guid, o);
    }

    [Fact]
    public void Deserialize_Guid()
    {
        // arrange
        var uuidType = new UuidType();
        var guid = Guid.NewGuid();

        // act
        var success = uuidType.TryDeserialize(guid, out var o);

        // assert
        Assert.True(success);
        Assert.Equal(guid, o);
    }

    [Fact]
    public void Deserialize_Int()
    {
        // arrange
        var uuidType = new UuidType();
        var value = 123;

        // act
        var success = uuidType.TryDeserialize(value, out _);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void ParseLiteral_StringValueNode()
    {
        // arrange
        var uuidType = new UuidType();
        var expected = Guid.NewGuid();
        var literalA = new StringValueNode(expected.ToString("N"));
        var literalB = new StringValueNode(expected.ToString("P"));

        // act
        var runtimeValueA = (Guid)uuidType.ParseLiteral(literalA)!;
        var runtimeValueB = (Guid)uuidType.ParseLiteral(literalB)!;

        // assert
        Assert.Equal(expected, runtimeValueA);
        Assert.Equal(expected, runtimeValueB);
    }

    [Fact]
    public void ParseLiteral_StringValueNode_Enforce_Format()
    {
        // arrange
        var uuidType = new UuidType(defaultFormat: 'P', enforceFormat: true);
        var expected = Guid.NewGuid();
        var literal = new StringValueNode(expected.ToString("N"));

        // act
        void Action() => uuidType.ParseLiteral(literal);

        // assert
        Assert.Throws<SerializationException>(Action);
    }

    [Fact]
    public void ParseLiteral_IntValueNode()
    {
        // arrange
        var uuidType = new UuidType();
        var literal = new IntValueNode(123);

        // act
        void Action() => uuidType.ParseLiteral(literal);

        // assert
        Assert.Throws<SerializationException>(Action);
    }

    [Fact]
    public void ParseLiteral_NullValueNode()
    {
        // arrange
        var uuidType = new UuidType();
        var literal = NullValueNode.Default;

        // act
        var value = uuidType.ParseLiteral(literal);

        // assert
        Assert.Null(value);
    }

    [Fact]
    public void ParseLiteral_Null()
    {
        // arrange
        var uuidType = new UuidType();

        // act
        void Action() => uuidType.ParseLiteral(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void ParseValue_Guid()
    {
        // arrange
        var uuidType = new UuidType();
        var expected = Guid.NewGuid();
        var expectedLiteralValue = expected.ToString("D");

        // act
        var stringLiteral = (StringValueNode)uuidType.ParseValue(expected);

        // assert
        Assert.Equal(expectedLiteralValue, stringLiteral.Value);
    }

    [Fact]
    public void ParseValue_Null()
    {
        // arrange
        var uuidType = new UuidType();
        Guid? guid = null;

        // act
        var stringLiteral = uuidType.ParseValue(guid);

        // assert
        Assert.True(stringLiteral is NullValueNode);
        Assert.Null(((NullValueNode)stringLiteral).Value);
    }

    [Fact]
    public void ParseValue_Int()
    {
        // arrange
        var uuidType = new UuidType();
        var value = 123;

        // act
        void Action() => uuidType.ParseValue(value);

        // assert
        Assert.Throws<SerializationException>(Action);
    }

    [Fact]
    public void EnsureDateTypeKindIsCorrect()
    {
        // arrange
        var type = new UuidType();

        // assert
        Assert.Equal(TypeKind.Scalar, type.Kind);
    }

    [InlineData('N')]
    [InlineData('D')]
    [InlineData('B')]
    [InlineData('P')]
    [Theory]
    public void Serialize_With_Format(char format)
    {
        // arrange
        var uuidType = new UuidType(defaultFormat: format);
        var guid = Guid.Empty;

        // act
        var s = (string)uuidType.Serialize(guid)!;

        // assert
        Assert.Equal(guid.ToString(format.ToString()), s);
    }

    [InlineData('N')]
    [InlineData('D')]
    [InlineData('B')]
    [InlineData('P')]
    [Theory]
    public void Deserialize_With_Format(char format)
    {
        // arrange
        var uuidType = new UuidType(defaultFormat: format);
        var guid = Guid.Empty;
        var serialized = guid.ToString(format.ToString());

        // act
        var deserialized = (Guid)uuidType.Deserialize(serialized)!;

        // assert
        Assert.Equal(guid, deserialized);
    }

    [InlineData('N')]
    [InlineData('D')]
    [InlineData('B')]
    [InlineData('P')]
    [Theory]
    public void ParseValue_With_Format(char format)
    {
        // arrange
        var uuidType = new UuidType(defaultFormat: format);
        var guid = Guid.Empty;

        // act
        var s = (StringValueNode)uuidType.ParseValue(guid);

        // assert
        Assert.Equal(guid.ToString(format.ToString()), s.Value);
    }

    [InlineData('N')]
    [InlineData('D')]
    [InlineData('B')]
    [InlineData('P')]
    [Theory]
    public void ParseLiteral_With_Format(char format)
    {
        // arrange
        var uuidType = new UuidType(defaultFormat: format);
        var guid = Guid.Empty;
        var literal = new StringValueNode(guid.ToString(format.ToString()));

        // act
        var deserialized = (Guid)uuidType.ParseLiteral(literal)!;

        // assert
        Assert.Equal(guid, deserialized);
    }

    [Fact]
    public void Specify_Invalid_Format()
    {
        // arrange
        // act
        void Action() => new UuidType(defaultFormat: 'z');

        // assert
        Assert.Throws<ArgumentException>(Action).Message.MatchSnapshot();
    }

    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public void Parse_Guid_String_With_Appended_String(bool enforceFormat)
    {
        // arrange
        var input = new StringValueNode("fbdef721-93c5-4267-8f92-ca27b60aa51f-foobar");
        var uuidType = new UuidType(defaultFormat: 'D', enforceFormat: enforceFormat);

        // act
        void Fail() => uuidType.ParseLiteral(input);

        // assert
        Assert.Throws<SerializationException>(Fail);
    }

    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public void Parse_Guid_Valid_Input(bool enforceFormat)
    {
        // arrange
        var input = new StringValueNode("fbdef721-93c5-4267-8f92-ca27b60aa51f");
        var uuidType = new UuidType(defaultFormat: 'D', enforceFormat: enforceFormat);

        // act
        var guid = (Guid)uuidType.ParseLiteral(input)!;

        // assert
        Assert.Equal(input.Value, guid.ToString("D"));
    }

    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public void Deserialize_Guid_String_With_Appended_String(bool enforceFormat)
    {
        // arrange
        var input = "fbdef721-93c5-4267-8f92-ca27b60aa51f-foobar";
        var uuidType = new UuidType(defaultFormat: 'D', enforceFormat: enforceFormat);

        // act
        void Fail() => uuidType.Deserialize(input);

        // assert
        Assert.Throws<SerializationException>(Fail);
    }

    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public void Deserialize_Guid_Valid_Format(bool enforceFormat)
    {
        // arrange
        var input = "fbdef721-93c5-4267-8f92-ca27b60aa51f";
        var uuidType = new UuidType(defaultFormat: 'D', enforceFormat: enforceFormat);

        // act
        var guid = (Guid)uuidType.Deserialize(input)!;

        // assert
        Assert.Equal(input, guid.ToString("D"));
    }

    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public void IsInstanceOf_Guid_String_With_Appended_String(bool enforceFormat)
    {
        // arrange
        var input = new StringValueNode("fbdef721-93c5-4267-8f92-ca27b60aa51f-foobar");
        var uuidType = new UuidType(defaultFormat: 'D', enforceFormat: enforceFormat);

        // act
        var result = uuidType.IsInstanceOfType(input);

        // assert
        Assert.False(result);
    }

    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public void IsInstanceOf_Guid_Valid_Format(bool enforceFormat)
    {
        // arrange
        var input = new StringValueNode("fbdef721-93c5-4267-8f92-ca27b60aa51f");
        var uuidType = new UuidType(defaultFormat: 'D', enforceFormat: enforceFormat);

        // act
        var result = uuidType.IsInstanceOfType(input);

        // assert
        Assert.True(result);
    }
}
