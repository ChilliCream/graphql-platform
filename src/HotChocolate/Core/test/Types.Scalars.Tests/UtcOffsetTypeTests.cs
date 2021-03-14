using System;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Scalars
{
    public class UtcOffsetTypeTests : ScalarTypeTestBase
    {
        [Fact]
        public void Schema_WithScalar_IsMatch()
        {
            // arrange
            ISchema schema = BuildSchema<UtcOffsetType>();

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
        [InlineData(typeof(StringValueNode), "+00:00", true)]
        [InlineData(typeof(StringValueNode), "+01:00", true)]
        [InlineData(typeof(StringValueNode), "+02:00", true)]
        [InlineData(typeof(StringValueNode), "+03:00", true)]
        [InlineData(typeof(StringValueNode), "+04:00", true)]
        [InlineData(typeof(StringValueNode), "+05:00", true)]
        [InlineData(typeof(StringValueNode), "+06:00", true)]
        [InlineData(typeof(StringValueNode), "+07:00", true)]
        [InlineData(typeof(StringValueNode), "+08:00", true)]
        [InlineData(typeof(StringValueNode), "+09:00", true)]
        [InlineData(typeof(StringValueNode), "+10:00", true)]
        [InlineData(typeof(StringValueNode), "+11:00", true)]
        [InlineData(typeof(StringValueNode), "+12:30", true)]
        [InlineData(typeof(StringValueNode), "+13:30", true)]
        [InlineData(typeof(StringValueNode), "+14:30", true)]
        [InlineData(typeof(StringValueNode), "-00:00", true)]
        [InlineData(typeof(StringValueNode), "-01:00", true)]
        [InlineData(typeof(StringValueNode), "-02:00", true)]
        [InlineData(typeof(StringValueNode), "-03:00", true)]
        [InlineData(typeof(StringValueNode), "-04:00", true)]
        [InlineData(typeof(StringValueNode), "-05:00", true)]
        [InlineData(typeof(StringValueNode), "-06:00", true)]
        [InlineData(typeof(StringValueNode), "-07:00", true)]
        [InlineData(typeof(StringValueNode), "-08:00", true)]
        [InlineData(typeof(StringValueNode), "-09:00", true)]
        [InlineData(typeof(StringValueNode), "-10:00", true)]
        [InlineData(typeof(StringValueNode), "-11:00", true)]
        [InlineData(typeof(StringValueNode), "-12:30", true)]
        [InlineData(typeof(StringValueNode), "-13:30", true)]
        [InlineData(typeof(StringValueNode), "-14:30", true)]
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
            ExpectIsInstanceOfTypeToMatch<UtcOffsetType>(valueNode, expected);
        }

        [Theory]
        [InlineData(TestEnum.Foo, false)]
        [InlineData(1d, false)]
        [InlineData(1, false)]
        [InlineData(true, false)]
        [InlineData("", false)]
        [InlineData("+00:00", true)]
        [InlineData("+01:00", true)]
        [InlineData("+02:00", true)]
        [InlineData("+03:00", true)]
        [InlineData("+04:00", true)]
        [InlineData("+05:00", true)]
        [InlineData("+06:00", true)]
        [InlineData("+07:00", true)]
        [InlineData("+08:00", true)]
        [InlineData("+09:00", true)]
        [InlineData("+10:00", true)]
        [InlineData("+11:00", true)]
        [InlineData("+12:30", true)]
        [InlineData("+13:30", true)]
        [InlineData("+14:30", true)]
        [InlineData("-00:00", true)]
        [InlineData("-01:00", true)]
        [InlineData("-02:00", true)]
        [InlineData("-03:00", true)]
        [InlineData("-04:00", true)]
        [InlineData("-05:00", true)]
        [InlineData("-06:00", true)]
        [InlineData("-07:00", true)]
        [InlineData("-08:00", true)]
        [InlineData("-09:00", true)]
        [InlineData("-10:00", true)]
        [InlineData("-11:00", true)]
        [InlineData("-12:30", true)]
        [InlineData("-13:30", true)]
        [InlineData("-14:30", true)]
        [InlineData(null, true)]
        public void IsInstanceOfType_GivenObject_MatchExpected(object value, bool expected)
        {
            // arrange
            // act
            // assert
            ExpectIsInstanceOfTypeToMatch<UtcOffsetType>(value, expected);
        }

        [Theory]
        [InlineData(typeof(StringValueNode), "+00:00", "+00:00")]
        [InlineData(typeof(StringValueNode), "+01:00", "+01:00")]
        [InlineData(typeof(StringValueNode), "+02:00", "+02:00")]
        [InlineData(typeof(StringValueNode), "+03:00", "+03:00")]
        [InlineData(typeof(StringValueNode), "+04:00", "+04:00")]
        [InlineData(typeof(StringValueNode), "+05:00", "+05:00")]
        [InlineData(typeof(StringValueNode), "+06:00", "+06:00")]
        [InlineData(typeof(StringValueNode), "+07:00", "+07:00")]
        [InlineData(typeof(StringValueNode), "+08:00", "+08:00")]
        [InlineData(typeof(StringValueNode), "+09:00", "+09:00")]
        [InlineData(typeof(StringValueNode), "+10:00", "+10:00")]
        [InlineData(typeof(StringValueNode), "+11:00", "+11:00")]
        [InlineData(typeof(StringValueNode), "+12:30", "+12:30")]
        [InlineData(typeof(StringValueNode), "+13:30", "+13:30")]
        [InlineData(typeof(StringValueNode), "+14:30", "+14:30")]
        [InlineData(typeof(StringValueNode), "-00:00", "-00:00")]
        [InlineData(typeof(StringValueNode), "-01:00", "-01:00")]
        [InlineData(typeof(StringValueNode), "-02:00", "-02:00")]
        [InlineData(typeof(StringValueNode), "-03:00", "-03:00")]
        [InlineData(typeof(StringValueNode), "-04:00", "-04:00")]
        [InlineData(typeof(StringValueNode), "-05:00", "-05:00")]
        [InlineData(typeof(StringValueNode), "-06:00", "-06:00")]
        [InlineData(typeof(StringValueNode), "-07:00", "-07:00")]
        [InlineData(typeof(StringValueNode), "-08:00", "-08:00")]
        [InlineData(typeof(StringValueNode), "-09:00", "-09:00")]
        [InlineData(typeof(StringValueNode), "-10:00", "-10:00")]
        [InlineData(typeof(StringValueNode), "-11:00", "-11:00")]
        [InlineData(typeof(StringValueNode), "-12:30", "-12:30")]
        [InlineData(typeof(StringValueNode), "-13:30", "-13:30")]
        [InlineData(typeof(StringValueNode), "-14:30", "-14:30")]
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
            ExpectParseLiteralToMatch<UtcOffsetType>(valueNode, expected);
        }

        [Theory]
        [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
        [InlineData(typeof(FloatValueNode), 1d)]
        [InlineData(typeof(IntValueNode), 1)]
        [InlineData(typeof(IntValueNode), 12345)]
        [InlineData(typeof(StringValueNode), "")]
        [InlineData(typeof(StringValueNode), "1")]
        [InlineData(typeof(StringValueNode), "1200")]
        [InlineData(typeof(StringValueNode), "+1500")]
        [InlineData(typeof(StringValueNode), "-1500")]
        [InlineData(typeof(StringValueNode), "-+11:30")]
        public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
        {
            // arrange
            IValueNode valueNode = CreateValueNode(type, value);

            // act
            // assert
            ExpectParseLiteralToThrowSerializationException<UtcOffsetType>(valueNode);
        }

        [Theory]
        [InlineData("+00:00", "+00:00")]
        [InlineData("+01:00", "+01:00")]
        [InlineData("+02:00", "+02:00")]
        [InlineData("+03:00", "+03:00")]
        [InlineData("+04:00", "+04:00")]
        [InlineData("+05:00", "+05:00")]
        [InlineData("+06:00", "+06:00")]
        [InlineData("+07:00", "+07:00")]
        [InlineData("+08:00", "+08:00")]
        [InlineData("+09:00", "+09:00")]
        [InlineData("+10:00", "+10:00")]
        [InlineData("+11:00", "+11:00")]
        [InlineData("+12:30", "+12:30")]
        [InlineData("+13:30", "+13:30")]
        [InlineData("+14:30", "+14:30")]
        [InlineData("-00:00", "-00:00")]
        [InlineData("-01:00", "-01:00")]
        [InlineData("-02:00", "-02:00")]
        [InlineData("-03:00", "-03:00")]
        [InlineData("-04:00", "-04:00")]
        [InlineData("-05:00", "-05:00")]
        [InlineData("-06:00", "-06:00")]
        [InlineData("-07:00", "-07:00")]
        [InlineData("-08:00", "-08:00")]
        [InlineData("-09:00", "-09:00")]
        [InlineData("-10:00", "-10:00")]
        [InlineData("-11:00", "-11:00")]
        [InlineData("-12:30", "-12:30")]
        [InlineData("-13:30", "-13:30")]
        [InlineData("-14:30", "-14:30")]
        [InlineData(null, null)]
        public void Deserialize_GivenValue_MatchExpected(
            object resultValue,
            object runtimeValue)
        {
            // arrange
            // act
            // assert
            ExpectDeserializeToMatch<UtcOffsetType>(resultValue, runtimeValue);
        }

        [Theory]
        [InlineData(TestEnum.Foo)]
        [InlineData(1d)]
        [InlineData(1)]
        [InlineData(12345)]
        [InlineData("")]
        [InlineData("1")]
        [InlineData("1200")]
        [InlineData("+1500")]
        [InlineData("-1500")]
        [InlineData("-+11:30")]
        public void Deserialize_GivenValue_ThrowSerializationException(object value)
        {
            // arrange
            // act
            // assert
            ExpectDeserializeToThrowSerializationException<UtcOffsetType>(value);
        }

        [Theory]
        [InlineData("+00:00", "+00:00")]
        [InlineData("+01:00", "+01:00")]
        [InlineData("+02:00", "+02:00")]
        [InlineData("+03:00", "+03:00")]
        [InlineData("+04:00", "+04:00")]
        [InlineData("+05:00", "+05:00")]
        [InlineData("+06:00", "+06:00")]
        [InlineData("+07:00", "+07:00")]
        [InlineData("+08:00", "+08:00")]
        [InlineData("+09:00", "+09:00")]
        [InlineData("+10:00", "+10:00")]
        [InlineData("+11:00", "+11:00")]
        [InlineData("+12:30", "+12:30")]
        [InlineData("+13:30", "+13:30")]
        [InlineData("+14:30", "+14:30")]
        [InlineData("-00:00", "-00:00")]
        [InlineData("-01:00", "-01:00")]
        [InlineData("-02:00", "-02:00")]
        [InlineData("-03:00", "-03:00")]
        [InlineData("-04:00", "-04:00")]
        [InlineData("-05:00", "-05:00")]
        [InlineData("-06:00", "-06:00")]
        [InlineData("-07:00", "-07:00")]
        [InlineData("-08:00", "-08:00")]
        [InlineData("-09:00", "-09:00")]
        [InlineData("-10:00", "-10:00")]
        [InlineData("-11:00", "-11:00")]
        [InlineData("-12:30", "-12:30")]
        [InlineData("-13:30", "-13:30")]
        [InlineData("-14:30", "-14:30")]
        [InlineData(null, null)]
        public void Serialize_GivenObject_MatchExpectedType(
            object runtimeValue,
            object resultValue)
        {
            // arrange
            // act
            // assert
            ExpectSerializeToMatch<UtcOffsetType>(runtimeValue, resultValue);
        }

        [Theory]
        [InlineData(TestEnum.Foo)]
        [InlineData(1d)]
        [InlineData(1)]
        [InlineData(12345)]
        [InlineData("")]
        [InlineData("1")]
        [InlineData("1200")]
        [InlineData("+1500")]
        [InlineData("-1500")]
        [InlineData("-+11:30")]
        public void Serialize_GivenObject_ThrowSerializationException(object value)
        {
            // arrange
            // act
            // assert
            ExpectSerializeToThrowSerializationException<UtcOffsetType>(value);
        }

        [Theory]
        [InlineData(typeof(StringValueNode), "+00:00")]
        [InlineData(typeof(StringValueNode), "+01:00")]
        [InlineData(typeof(StringValueNode), "+02:00")]
        [InlineData(typeof(StringValueNode), "+03:00")]
        [InlineData(typeof(StringValueNode), "+04:00")]
        [InlineData(typeof(StringValueNode), "+05:00")]
        [InlineData(typeof(StringValueNode), "+06:00")]
        [InlineData(typeof(StringValueNode), "+07:00")]
        [InlineData(typeof(StringValueNode), "+08:00")]
        [InlineData(typeof(StringValueNode), "+09:00")]
        [InlineData(typeof(StringValueNode), "+10:00")]
        [InlineData(typeof(StringValueNode), "+11:00")]
        [InlineData(typeof(StringValueNode), "+12:30")]
        [InlineData(typeof(StringValueNode), "+13:30")]
        [InlineData(typeof(StringValueNode), "+14:30")]
        [InlineData(typeof(StringValueNode), "-00:00")]
        [InlineData(typeof(StringValueNode), "-01:00")]
        [InlineData(typeof(StringValueNode), "-02:00")]
        [InlineData(typeof(StringValueNode), "-03:00")]
        [InlineData(typeof(StringValueNode), "-04:00")]
        [InlineData(typeof(StringValueNode), "-05:00")]
        [InlineData(typeof(StringValueNode), "-06:00")]
        [InlineData(typeof(StringValueNode), "-07:00")]
        [InlineData(typeof(StringValueNode), "-08:00")]
        [InlineData(typeof(StringValueNode), "-09:00")]
        [InlineData(typeof(StringValueNode), "-10:00")]
        [InlineData(typeof(StringValueNode), "-11:00")]
        [InlineData(typeof(StringValueNode), "-12:30")]
        [InlineData(typeof(StringValueNode), "-13:30")]
        [InlineData(typeof(StringValueNode), "-14:30")]
        [InlineData(typeof(NullValueNode), null)]
        public void ParseValue_GivenObject_MatchExpectedType(Type type, object value)
        {
            // arrange
            // act
            // assert
            ExpectParseValueToMatchType<UtcOffsetType>(value, type);
        }

        [Theory]
        [InlineData(TestEnum.Foo)]
        [InlineData(1d)]
        [InlineData(1)]
        [InlineData(12345)]
        [InlineData("")]
        [InlineData("1")]
        [InlineData("1200")]
        [InlineData("+1500")]
        [InlineData("-1500")]
        [InlineData("-+11:30")]
        public void ParseValue_GivenObject_ThrowSerializationException(object value)
        {
            // arrange
            // act
            // assert
            ExpectParseValueToThrowSerializationException<UtcOffsetType>(value);
        }

        [Theory]
        [InlineData(typeof(StringValueNode), "+00:00")]
        [InlineData(typeof(StringValueNode), "+01:00")]
        [InlineData(typeof(StringValueNode), "+02:00")]
        [InlineData(typeof(StringValueNode), "+03:00")]
        [InlineData(typeof(StringValueNode), "+04:00")]
        [InlineData(typeof(StringValueNode), "+05:00")]
        [InlineData(typeof(StringValueNode), "+06:00")]
        [InlineData(typeof(StringValueNode), "+07:00")]
        [InlineData(typeof(StringValueNode), "+08:00")]
        [InlineData(typeof(StringValueNode), "+09:00")]
        [InlineData(typeof(StringValueNode), "+10:00")]
        [InlineData(typeof(StringValueNode), "+11:00")]
        [InlineData(typeof(StringValueNode), "+12:30")]
        [InlineData(typeof(StringValueNode), "+13:30")]
        [InlineData(typeof(StringValueNode), "+14:30")]
        [InlineData(typeof(StringValueNode), "-00:00")]
        [InlineData(typeof(StringValueNode), "-01:00")]
        [InlineData(typeof(StringValueNode), "-02:00")]
        [InlineData(typeof(StringValueNode), "-03:00")]
        [InlineData(typeof(StringValueNode), "-04:00")]
        [InlineData(typeof(StringValueNode), "-05:00")]
        [InlineData(typeof(StringValueNode), "-06:00")]
        [InlineData(typeof(StringValueNode), "-07:00")]
        [InlineData(typeof(StringValueNode), "-08:00")]
        [InlineData(typeof(StringValueNode), "-09:00")]
        [InlineData(typeof(StringValueNode), "-10:00")]
        [InlineData(typeof(StringValueNode), "-11:00")]
        [InlineData(typeof(StringValueNode), "-12:30")]
        [InlineData(typeof(StringValueNode), "-13:30")]
        [InlineData(typeof(StringValueNode), "-14:30")]
        [InlineData(typeof(NullValueNode), null)]
        public void ParseResult_GivenObject_MatchExpectedType(Type type, object value)
        {
            // arrange
            // act
            // assert
            ExpectParseResultToMatchType<UtcOffsetType>(value, type);
        }

        [Theory]
        [InlineData(TestEnum.Foo)]
        [InlineData(1d)]
        [InlineData(1)]
        [InlineData(12345)]
        [InlineData("")]
        [InlineData("1")]
        [InlineData("1200")]
        [InlineData("+1500")]
        [InlineData("-1500")]
        [InlineData("-+11:30")]
        public void ParseResult_GivenObject_ThrowSerializationException(object value)
        {
            // arrange
            // act
            // assert
            ExpectParseResultToThrowSerializationException<UtcOffsetType>(value);
        }
    }
}
