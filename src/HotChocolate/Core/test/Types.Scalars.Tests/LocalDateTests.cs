using System;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Scalars
{
    public class LocalDateTypeTests : ScalarTypeTestBase
    {
        [Fact]
        public void Schema_WithScalar_IsMatch()
        {
            // arrange
            ISchema schema = BuildSchema<LocalDateType>();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Theory]
        [InlineData(typeof(EnumValueNode), TestEnum.Foo, false)]
        [InlineData(typeof(FloatValueNode), 1d, false)]
        [InlineData(typeof(IntValueNode), 1, false)]
        [InlineData(typeof(BooleanValueNode), true, false)]
        [InlineData(typeof(StringValueNode), "", false)]
        [InlineData(typeof(StringValueNode), "1", false)]
        [InlineData(typeof(StringValueNode), "11", false)]
        [InlineData(typeof(StringValueNode), "2021", false)]
        [InlineData(typeof(StringValueNode), "2021-04", false)]
        [InlineData(typeof(StringValueNode), "2021-03-011", false)]
        [InlineData(typeof(StringValueNode), "2021-03-32", false)]
        [InlineData(typeof(StringValueNode), "2021-033-32", false)]
        [InlineData(typeof(StringValueNode), "2021-13-02", false)]
        [InlineData(typeof(StringValueNode), "20211-03-01", false)]
        [InlineData(typeof(StringValueNode), "2021-01-00", false)]
        [InlineData(typeof(StringValueNode), "2021-00-01", false)]
        [InlineData(typeof(StringValueNode), "2021-00-00", false)]
        [InlineData(typeof(StringValueNode), "2021-2-01", false)]
        [InlineData(typeof(StringValueNode), "2021-02-1", false)]
        [InlineData(typeof(StringValueNode), "202-01-01", false)]
        [InlineData(typeof(StringValueNode), "0000-01-01", true)]
        [InlineData(typeof(StringValueNode), "2021-03-01", true)]
        [InlineData(typeof(StringValueNode), "2021-12-31", true)]
        [InlineData(typeof(StringValueNode), "2021-02-31", false)]
        [InlineData(typeof(StringValueNode), "2021-02-28", true)]
        [InlineData(typeof(StringValueNode), "9999-01-01", true)]
        [InlineData(typeof(StringValueNode), "2021:01:01", false)]
        [InlineData(typeof(StringValueNode), "2021/01/01", false)]
        [InlineData(typeof(StringValueNode), "2021.01.01", false)]
        [InlineData(typeof(NullValueNode), null, true)]
        public void IsInstanceOfType_GivenValueNode_MatchExpected(
            Type type,
            object value,
            bool expected)
        {
            // arrange
            IValueNode valueNode = CreateValueNode(type, value);

            // act
            // assert
            ExpectIsInstanceOfTypeToMatch<LocalDateType>(valueNode, expected);
        }

        [Theory]
        [InlineData(TestEnum.Foo, false)]
        [InlineData(1d, false)]
        [InlineData(1, false)]
        [InlineData(true, false)]
        [InlineData("", false)]
        [InlineData("1", false)]
        [InlineData("11", false)]
        [InlineData("2021", false)]
        [InlineData("2021-04", false)]
        [InlineData("2021-03-011", false)]
        [InlineData("2021-03-32", false)]
        [InlineData("2021-033-02", false)]
        [InlineData("2021-13-32", false)]
        [InlineData("20211-03-01", false)]
        [InlineData("2021-01-00", false)]
        [InlineData("2021-00-01", false)]
        [InlineData("2021-00-00", false)]
        [InlineData("2021-2-01", false)]
        [InlineData("2021-02-1", false)]
        [InlineData("202-01-01", false)]
        [InlineData("0000-01-01", true)]
        [InlineData("2021-03-01", true)]
        [InlineData("2021-12-31", true)]
        [InlineData("2021-02-31", false)]
        [InlineData("2021-02-28", true)]
        [InlineData("9999-01-01", true)]
        [InlineData("2021:01:01", false)]
        [InlineData("2021/01/01", false)]
        [InlineData("2021.01.01", false)]
        [InlineData(null, true)]
        public void IsInstanceOfType_GivenObject_MatchExpected(object value, bool expected)
        {
            // arrange
            // act
            // assert
            ExpectIsInstanceOfTypeToMatch<LocalDateType>(value, expected);
        }

        [Theory]
        [InlineData(typeof(StringValueNode), "2021-02-28", "2021-02-28")]
        [InlineData(typeof(StringValueNode), "2021-12-31", "2021-12-31")]
        [InlineData(typeof(StringValueNode), "2021-01-01", "2021-01-01")]
        [InlineData(typeof(StringValueNode), "0000-01-01", "0000-01-01")]
        [InlineData(typeof(StringValueNode), "9999-01-01", "9999-01-01")]
        [InlineData(typeof(NullValueNode), null, null)]
        public void ParseLiteral_GivenValueNode_MatchExpected(
            Type type,
            object value,
            object expected)
        {
            // arrange
            IValueNode valueNode = CreateValueNode(type, value);

            // act
            // assert
            ExpectParseLiteralToMatch<LocalDateType>(valueNode, expected);
        }

        [Theory]
        [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
        [InlineData(typeof(FloatValueNode), 1d)]
        [InlineData(typeof(IntValueNode), 1)]
        [InlineData(typeof(IntValueNode), 12345)]
        [InlineData(typeof(StringValueNode), "")]
        [InlineData(typeof(StringValueNode), "1")]
        [InlineData(typeof(StringValueNode), "2021")]
        [InlineData(typeof(StringValueNode), "2021-02-31")]
        [InlineData(typeof(StringValueNode), "2021-13-02")]
        [InlineData(typeof(StringValueNode), "2021-13-33")]
        [InlineData(typeof(StringValueNode), "2021.01.01")]
        [InlineData(typeof(StringValueNode), "2021/01/01")]
        [InlineData(typeof(StringValueNode), "2021:01:01")]
        public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
        {
            // arrange
            IValueNode valueNode = CreateValueNode(type, value);

            // act
            // assert
            ExpectParseLiteralToThrowSerializationException<LocalDateType>(valueNode);
        }

        [Theory]
        [InlineData("2021-02-28", "2021-02-28")]
        [InlineData("2021-12-31", "2021-12-31")]
        [InlineData("2021-01-01", "2021-01-01")]
        [InlineData("0000-01-01", "0000-01-01")]
        [InlineData(null, null)]
        public void Deserialize_GivenValue_MatchExpected(
            object resultValue,
            object runtimeValue)
        {
            // arrange
            // act
            // assert
            ExpectDeserializeToMatch<LocalDateType>(resultValue, runtimeValue);
        }

        [Theory]
        [InlineData(TestEnum.Foo)]
        [InlineData(1d)]
        [InlineData(1)]
        [InlineData(12345)]
        [InlineData("")]
        [InlineData("1")]
        [InlineData("2021")]
        [InlineData("2021-02-31")]
        [InlineData("2021-13-02")]
        [InlineData("2021-13-33")]
        [InlineData("2021-01-111")]
        [InlineData("2021:00:00")]
        [InlineData("2021/00/00")]
        [InlineData("2021.00.00")]
        public void Deserialize_GivenValue_ThrowSerializationException(object value)
        {
            // arrange
            // act
            // assert
            ExpectDeserializeToThrowSerializationException<EmailAddressType>(value);
        }

        [Theory]
        [InlineData("2021-02-28", "2021-02-28")]
        [InlineData("2021-12-31", "2021-12-31")]
        [InlineData("2021-01-01", "2021-01-01")]
        [InlineData("0000-01-01", "0000-01-01")]
        [InlineData("9999-01-01", "9999-01-01")]
        [InlineData(null, null)]
        public void Serialize_GivenObject_MatchExpectedType(
            object runtimeValue,
            object resultValue)
        {
            // arrange
            // act
            // assert
            ExpectSerializeToMatch<LocalDateType>(runtimeValue, resultValue);
        }

        [Theory]
        [InlineData(TestEnum.Foo)]
        [InlineData(1d)]
        [InlineData(1)]
        [InlineData(12345)]
        [InlineData("")]
        [InlineData("1")]
        [InlineData("2021")]
        [InlineData("2021-02-31")]
        [InlineData("2021-13-02")]
        [InlineData("2021-13-33")]
        [InlineData("2021-01-111")]
        [InlineData("2021:00:00")]
        [InlineData("2021/00/00")]
        [InlineData("2021.00.00")]
        public void Serialize_GivenObject_ThrowSerializationException(object value)
        {
            // arrange
            // act
            // assert
            ExpectSerializeToThrowSerializationException<LocalDateType>(value);
        }

        [Theory]
        [InlineData(typeof(StringValueNode), "2021-02-28")]
        [InlineData(typeof(StringValueNode), "2021-12-02")]
        [InlineData(typeof(StringValueNode), "2021-12-31")]
        [InlineData(typeof(StringValueNode), "0000-01-01")]
        [InlineData(typeof(NullValueNode), null)]
        public void ParseValue_GivenObject_MatchExpectedType(Type type, object value)
        {
            // arrange
            // act
            // assert
            ExpectParseValueToMatchType<LocalDateType>(value, type);
        }

        [Theory]
        [InlineData(TestEnum.Foo)]
        [InlineData(1d)]
        [InlineData(1)]
        [InlineData(12345)]
        [InlineData("")]
        [InlineData("1")]
        [InlineData("2021")]
        [InlineData("2021-02-31")]
        [InlineData("2021-13-02")]
        [InlineData("2021-13-33")]
        [InlineData("2021-00-00")]
        [InlineData("2021:00:00")]
        [InlineData("2021/00/00")]
        [InlineData("2021.00.00")]
        public void ParseValue_GivenObject_ThrowSerializationException(object value)
        {
            // arrange
            // act
            // assert
            ExpectParseValueToThrowSerializationException<LocalDateType>(value);
        }

        [Theory]
        [InlineData(typeof(StringValueNode), "2021-02-28")]
        [InlineData(typeof(StringValueNode), "2021-12-31")]
        [InlineData(typeof(StringValueNode), "2021-01-01")]
        [InlineData(typeof(StringValueNode), "0000-01-01")]
        [InlineData(typeof(StringValueNode), "9999-01-01")]
        [InlineData(typeof(NullValueNode), null)]
        public void ParseResult_GivenObject_MatchExpectedType(Type type, object value)
        {
            // arrange
            // act
            // assert
            ExpectParseResultToMatchType<LocalDateType>(value, type);
        }

        [Theory]
        [InlineData(TestEnum.Foo)]
        [InlineData(1d)]
        [InlineData(1)]
        [InlineData(12345)]
        [InlineData("")]
        [InlineData("1")]
        [InlineData("2021")]
        [InlineData("2021-02-31")]
        [InlineData("2021-13-02")]
        [InlineData("2021-13-33")]
        [InlineData("2021-00-00")]
        [InlineData("2021:00:00")]
        [InlineData("2021/00/00")]
        [InlineData("2021.00.00")]
        public void ParseResult_GivenObject_ThrowSerializationException(object value)
        {
            // arrange
            // act
            // assert
            ExpectParseResultToThrowSerializationException<LocalDateType>(value);
        }
    }
}
