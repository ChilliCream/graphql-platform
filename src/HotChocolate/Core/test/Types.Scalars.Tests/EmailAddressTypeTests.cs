using System;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Scalars
{
    public class EmailAddressTypeTests : ScalarTypeTestBase
    {
        [Fact]
        public void Schema_WithScalar_IsMatch()
        {
            // arrange
            ISchema schema = BuildSchema<EmailAddressType>();

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
        [InlineData(typeof(StringValueNode), "test@chillicream.com", true)]
        [InlineData(typeof(StringValueNode), "CapitalizeTest@chillicream.com", true)]
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
            ExpectIsInstanceOfTypeToMatch<EmailAddressType>(valueNode, expected);
        }

        [Theory]
        [InlineData(TestEnum.Foo, false)]
        [InlineData(1d, false)]
        [InlineData(1, false)]
        [InlineData(true, false)]
        [InlineData("", false)]
        [InlineData(null, true)]
        [InlineData("test@chillicream.com", true)]
        [InlineData("CapitalizeTest@chillicream.com", true)]
        public void IsInstanceOfType_GivenObject_MatchExpected(object value, bool expected)
        {
            // arrange
            // act
            // assert
            ExpectIsInstanceOfTypeToMatch<EmailAddressType>(value, expected);
        }

        [Theory]
        [InlineData(typeof(StringValueNode), "test@chillicream.com", "test@chillicream.com")]
        [InlineData(typeof(StringValueNode), "CapitalizeTest@chillicream.com", "CapitalizeTest@chillicream.com")]
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
            ExpectParseLiteralToMatch<EmailAddressType>(valueNode, expected);
        }

        [Theory]
        [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
        [InlineData(typeof(FloatValueNode), 1d)]
        [InlineData(typeof(IntValueNode), 1)]
        [InlineData(typeof(BooleanValueNode), true)]
        [InlineData(typeof(StringValueNode), "")]
        [InlineData(typeof(StringValueNode), "invalid.email.com")]
        [InlineData(typeof(StringValueNode), "email@-example.com")]
        [InlineData(typeof(StringValueNode), "email@example..com")]
        public void ParseLiteral_GivenValueNode_ThrowSerializationException(Type type, object value)
        {
            // arrange
            IValueNode valueNode = CreateValueNode(type, value);

            // act
            // assert
            ExpectParseLiteralToThrowSerializationException<EmailAddressType>(valueNode);
        }

        [Theory]
        [InlineData("test@chillicream.com", "test@chillicream.com")]
        [InlineData("CapitalizeTest@chillicream.com", "CapitalizeTest@chillicream.com")]
        [InlineData(null, null)]
        public void Deserialize_GivenValue_MatchExpected(
            object resultValue,
            object runtimeValue)
        {
            // arrange
            // act
            // assert
            ExpectDeserializeToMatch<EmailAddressType>(resultValue, runtimeValue);
        }

        [Theory]
        [InlineData(TestEnum.Foo)]
        [InlineData(1d)]
        [InlineData(1)]
        [InlineData(true)]
        [InlineData("invalid.email.com")]
        [InlineData("email@-example.com")]
        [InlineData("email@example..com")]
        public void Deserialize_GivenValue_ThrowSerializationException(object value)
        {
            // arrange
            // act
            // assert
            ExpectDeserializeToThrowSerializationException<EmailAddressType>(value);
        }

        [Theory]
        [InlineData("test@chillicream.com", "test@chillicream.com")]
        [InlineData("CapitalizeTest@chillicream.com", "CapitalizeTest@chillicream.com")]
        [InlineData(null, null)]
        public void Serialize_GivenObject_MatchExpectedType(
            object runtimeValue,
            object resultValue)
        {
            // arrange
            // act
            // assert
            ExpectSerializeToMatch<EmailAddressType>(runtimeValue, resultValue);
        }

        [Theory]
        [InlineData(TestEnum.Foo)]
        [InlineData(1d)]
        [InlineData(1)]
        [InlineData(true)]
        [InlineData("invalid.email.com")]
        [InlineData("email@-example.com")]
        [InlineData("email@example..com")]
        public void Serialize_GivenObject_ThrowSerializationException(object value)
        {
            // arrange
            // act
            // assert
            ExpectSerializeToThrowSerializationException<EmailAddressType>(value);
        }
    }
}
