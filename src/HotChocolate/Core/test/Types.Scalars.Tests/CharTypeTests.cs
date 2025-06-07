using System;
using HotChocolate.Language;
using Snapshooter.Xunit;

namespace HotChocolate.Types;

public class CharTypeTests : ScalarTypeTestBase
{
    [Fact]
    public void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<CharType>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
    [InlineData(typeof(IntValueNode), 1, false)]
    [InlineData(typeof(BooleanValueNode), true, false)]
    [InlineData(typeof(StringValueNode), "", false)]
    [InlineData(typeof(StringValueNode), "a", true)]
    [InlineData(typeof(StringValueNode), "7", true)]
    [InlineData(typeof(StringValueNode), "\u263B", true)]
    [InlineData(typeof(NullValueNode), null, true)]
    public void IsInstanceOfType_GivenValueNode_MatchExpected(
        Type type,
        object value,
        bool expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<CharType>(valueNode, expected);
    }

    [Theory]
    [InlineData(TestEnum.Foo, false)]
    [InlineData(1, false)]
    [InlineData(true, false)]
    [InlineData("", false)]
    [InlineData("a", false)]
    [InlineData('a', true)]
    [InlineData('7', true)]
    [InlineData('\u263B', true)]
    [InlineData(null, true)]
    public void IsInstanceOfType_GivenObject_MatchExpected(object value, bool expected)
    {
        // arrange
        // act
        // assert
        ExpectIsInstanceOfTypeToMatch<CharType>(value, expected);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), "a", 'a')]
    [InlineData(typeof(StringValueNode), "7", '7')]
    [InlineData(typeof(StringValueNode), "\u263B", '\u263b')]
    [InlineData(typeof(NullValueNode), null, null)]
    public void ParseLiteral_GivenValueNode_MatchExpected(
        Type type,
        object value,
        object expected)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToMatch<CharType>(valueNode, expected);
    }

    [Theory]
    [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
    [InlineData(typeof(FloatValueNode), 2.7d)]
    [InlineData(typeof(IntValueNode), char.MinValue - 1)]
    [InlineData(typeof(IntValueNode), char.MaxValue + 1)]
    [InlineData(typeof(BooleanValueNode), true)]
    [InlineData(typeof(StringValueNode), "")]
    [InlineData(typeof(StringValueNode), "cool beans")]
    public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
    {
        // arrange
        var valueNode = CreateValueNode(type, value);

        // act
        // assert
        ExpectParseLiteralToThrowSerializationException<CharType>(valueNode);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), 'a')]
    [InlineData(typeof(StringValueNode), '7')]
    [InlineData(typeof(StringValueNode), '\u263b')]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseValue_GivenObject_MatchExpectedType(Type type, object value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToMatchType<CharType>(value, type);
    }

    [Theory]
    [InlineData(2.7d)]
    [InlineData(true)]
    [InlineData("")]
    [InlineData("cool beans")]
    public void ParseValue_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseValueToThrowSerializationException<CharType>(value);
    }

    [Theory]
    [InlineData(65, 'A')]
    [InlineData('a', 'a')]
    [InlineData('7', '7')]
    [InlineData('\u0007', '\u0007')]
    [InlineData(null, null)]
    public void Deserialize_GivenValue_MatchExpected(
        object resultValue,
        object runtimeValue)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToMatch<CharType>(resultValue, runtimeValue);
    }

    [Theory]
    [InlineData(2.7d)]
    [InlineData(true)]
    [InlineData("")]
    [InlineData("cool beans")]
    public void Deserialize_GivenValue_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectDeserializeToThrowSerializationException<CharType>(value);
    }

    [Theory]
    [InlineData(65, 'A')]
    [InlineData('a', 'a')]
    [InlineData('7', '7')]
    [InlineData('\u0007', '\u0007')]
    [InlineData(null, null)]
    public void Serialize_GivenObject_MatchExpectedType(
        object runtimeValue,
        object resultValue)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToMatch<CharType>(runtimeValue, resultValue);
    }

    [Theory]
    [InlineData(2.7d)]
    [InlineData(char.MinValue - 1)]
    [InlineData(char.MaxValue + 1)]
    [InlineData(true)]
    [InlineData("")]
    [InlineData("cool beans")]
    public void Serialize_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectSerializeToThrowSerializationException<CharType>(value);
    }

    [Theory]
    [InlineData(typeof(StringValueNode), 'a')]
    [InlineData(typeof(StringValueNode), '7')]
    [InlineData(typeof(StringValueNode), '\u263b')]
    [InlineData(typeof(NullValueNode), null)]
    public void ParseResult_GivenObject_MatchExpectedType(Type type, object value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToMatchType<CharType>(value, type);
    }

    [Theory]
    [InlineData(2.7d)]
    [InlineData(char.MinValue - 1)]
    [InlineData(char.MaxValue + 1)]
    [InlineData(true)]
    [InlineData("")]
    [InlineData("cool beans")]
    public void ParseResult_GivenObject_ThrowSerializationException(object value)
    {
        // arrange
        // act
        // assert
        ExpectParseResultToThrowSerializationException<CharType>(value);
    }
}
