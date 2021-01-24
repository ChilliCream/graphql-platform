using System;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Scalars
{
    public class NonEmptyStringTypeTests : ScalarTypeTestBase
    {
        [Fact]
        public void Schema_IsMatch()
        {
            // arrange
            ISchema schema = BuildSchema<NonEmptyStringType>();

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
        [InlineData(typeof(StringValueNode), "foo", true)]
        [InlineData(typeof(NullValueNode), null, true)]
        public void Test_IsInstanceOfTypeValueNode(Type type, object value, bool expected)
        {
            // arrange
            IValueNode valueNode = CreateValueNode(type, value);

            // act
            // assert
            IsInstanceOfType<NonEmptyStringType>(valueNode, expected);
        }

        [Theory]
        [InlineData(TestEnum.Foo, false)]
        [InlineData(1d, false)]
        [InlineData(1, false)]
        [InlineData(true, false)]
        [InlineData("", false)]
        [InlineData(null, true)]
        [InlineData("foo", true)]
        public void Test_IsInstanceOfTypeObject(object value, bool expected)
        {
            // arrange
            // act
            // assert
            IsInstanceOfType<NonEmptyStringType>(value, expected);
        }

        [Theory]
        [InlineData(typeof(StringValueNode), "foo", "foo")]
        [InlineData(typeof(NullValueNode), null, null)]
        public void Test_ParseLiteral(Type type, object value, object expected)
        {
            // arrange
            IValueNode valueNode = CreateValueNode(type, value);

            // act
            // assert
            ParseLiteral<NonEmptyStringType>(valueNode, expected);
        }

        [Theory]
        [InlineData(typeof(EnumValueNode), TestEnum.Foo)]
        [InlineData(typeof(FloatValueNode), 1d)]
        [InlineData(typeof(IntValueNode), 1)]
        [InlineData(typeof(BooleanValueNode), true)]
        [InlineData(typeof(StringValueNode), "")]
        public void Test_ParseLiteralInvalid(Type type, object value)
        {
            // arrange
            IValueNode valueNode = CreateValueNode(type, value);

            // act
            // assert
            ParseLiteralInvalid<NonEmptyStringType>(valueNode);
        }

        [Theory]
        [InlineData(typeof(StringValueNode), "foo")]
        [InlineData(typeof(NullValueNode), null)]
        public void Test_ParseValue(Type type, object value)
        {
            // arrange
            // act
            // assert
            ParseValue<NonEmptyStringType>(value, type);
        }

        [Theory]
        [InlineData(TestEnum.Foo)]
        [InlineData(1d)]
        [InlineData(1)]
        [InlineData(true)]
        [InlineData("")]
        public void Test_ParseValueInvalid(object value)
        {
            // arrange
            // act
            // assert
            ParseValueInvalid<NonEmptyStringType>(value);
        }
    }
}
