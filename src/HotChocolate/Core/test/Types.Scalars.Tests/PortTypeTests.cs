using HotChocolate.Language;

namespace HotChocolate.Types;

public class PortTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<PortType>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
    [InlineData(typeof(FloatValueNode), 1d, false)]
    [InlineData(typeof(BooleanValueNode), true, false)]
    [InlineData(typeof(StringValueNode), "", false)]
    [InlineData(typeof(StringValueNode), "foo", false)]
    [InlineData(typeof(IntValueNode), -1, false)]
    [InlineData(typeof(IntValueNode), -65536, false)]
    [InlineData(typeof(IntValueNode), 0, true)]
    [InlineData(typeof(IntValueNode), 1337, true)]
    [InlineData(typeof(IntValueNode), 3000, true)]
    [InlineData(typeof(IntValueNode), 4000, true)]
    [InlineData(typeof(IntValueNode), 5000, true)]
    [InlineData(typeof(IntValueNode), 8080, true)]
    [InlineData(typeof(IntValueNode), 65535, true)]
    [InlineData(typeof(NullValueNode), null, true)]
    public void IsInstanceOfType_GivenValueNode_MatchExpected(
        Type type,
        object? value,
        bool expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<PortType>(valueNode, expected);
    }

    [Theory]
    [InlineData(TestEnum.Foo, false)]
    [InlineData(1d, false)]
    [InlineData(true, false)]
    [InlineData("", false)]
    [InlineData("foo", false)]
    [InlineData(-1, false)]
    [InlineData(-65536, false)]
    [InlineData(0, true)]
    [InlineData(1337, true)]
    [InlineData(3000, true)]
    [InlineData(4000, true)]
    [InlineData(5000, true)]
    [InlineData(8080, true)]
    [InlineData(65535, true)]
    [InlineData(null, true)]
    public void IsInstanceOfType_GivenObject_MatchExpected(object? value, bool expected)
    {
        // arrange
        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<PortType>(value, expected);
    }

    [Theory]
    [InlineData(typeof(IntValueNode), 0, 0)]
    [InlineData(typeof(IntValueNode), 1337, 1337)]
    [InlineData(typeof(IntValueNode), 3000, 3000)]
    [InlineData(typeof(IntValueNode), 4000, 4000)]
    [InlineData(typeof(IntValueNode), 5000, 5000)]
    [InlineData(typeof(IntValueNode), 8080, 8080)]
    [InlineData(typeof(IntValueNode), 65535, 65535)]
    public void ParseLiteral_GivenValueNode_MatchExpected(
        Type type,
        object value,
        object expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToMatch<PortType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
    [InlineData(typeof(FloatValueNode), 1d)]
    [InlineData(typeof(BooleanValueNode), true)]
    [InlineData(typeof(StringValueNode), "")]
    [InlineData(typeof(IntValueNode), int.MinValue)]
    [InlineData(typeof(IntValueNode), int.MaxValue)]
    [InlineData(typeof(IntValueNode), -1)]
    [InlineData(typeof(IntValueNode), 65536)]
    public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<PortType>(valueNode);
    }

    [Theory]
    [InlineData(typeof(IntValueNode), 0)]
    [InlineData(typeof(IntValueNode), 1337)]
    [InlineData(typeof(IntValueNode), 3000)]
    [InlineData(typeof(IntValueNode), 4000)]
    [InlineData(typeof(IntValueNode), 5000)]
    [InlineData(typeof(IntValueNode), 8080)]
    [InlineData(typeof(IntValueNode), 65535)]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<PortType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(true)]
    [InlineData("foo")]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void ParseLiteral_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToThrowSerializationException<PortType>(value);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1337, 1337)]
    [InlineData(3000, 3000)]
    [InlineData(4000, 4000)]
    [InlineData(5000, 5000)]
    [InlineData(8080, 8080)]
    [InlineData(65535, 65535)]
    public void Deserialize_GivenValue_MatchExpected(
        object resultValue,
        object runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToMatch<PortType>(resultValue, runtimeValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(true)]
    [InlineData("foo")]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void Deserialize_GivenValue_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToThrowSerializationException<PortType>(value);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1337, 1337)]
    [InlineData(3000, 3000)]
    [InlineData(4000, 4000)]
    [InlineData(5000, 5000)]
    [InlineData(8080, 8080)]
    [InlineData(65535, 65535)]
    public void Serialize_GivenObject_MatchExpectedType(
        object runtimeValue,
        object resultValue)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToMatch<PortType>(runtimeValue, resultValue);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(true)]
    [InlineData("foo")]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void Serialize_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToThrowSerializationException<PortType>(value);
    }

    [Theory]
    [InlineData(typeof(IntValueNode), 0)]
    [InlineData(typeof(IntValueNode), 1337)]
    [InlineData(typeof(IntValueNode), 3000)]
    [InlineData(typeof(IntValueNode), 4000)]
    [InlineData(typeof(IntValueNode), 5000)]
    [InlineData(typeof(IntValueNode), 8080)]
    [InlineData(typeof(IntValueNode), 65535)]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object? value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<PortType>(value, type);
    }

    [Theory]
    [InlineData(TestEnum.Foo)]
    [InlineData(1d)]
    [InlineData(true)]
    [InlineData("foo")]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void ParseResult_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToThrowSerializationException<PortType>(value);
    }
}
