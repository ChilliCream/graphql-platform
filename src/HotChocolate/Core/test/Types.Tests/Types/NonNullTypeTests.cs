using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class NonNullTypeTests
    {
        [Fact]
        public void EnsureInnerTypeIsCorrectlySet()
        {
            // arrange
            var innerType = new StringType();

            // act
            var type = new NonNullType(innerType);

            // assert
            Assert.Equal(innerType, type.Type);
        }


        [Fact]
        public void EnsureNativeTypeIsCorrectlyDetected()
        {
            // act
            var type = new NonNullType(new StringType());

            // assert
            Assert.Equal(typeof(string), type.ClrType);
        }

        [Fact]
        public void InnerType_Cannot_Be_A_NonNullType()
        {
            // act
            Action action =
                () => new NonNullType(new NonNullType(new StringType()));

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            var type = (IInputType)new NonNullType(new StringType());

            // act
            Action action = () => type.ParseLiteral(NullValueNode.Default);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void ParseLiteral_StringValueNode()
        {
            // arrange
            var type = (IInputType)new NonNullType(new StringType());

            // act
            object value = type.ParseLiteral(new StringValueNode("abc"));

            // assert
            Assert.Equal("abc", value);
        }

        [Fact]
        public void ParseValue_NullValue()
        {
            // arrange
            var type = (IInputType)new NonNullType(new StringType());

            // act
            Action action = () => type.ParseValue(null);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void ParseValue_StringValue()
        {
            // arrange
            var type = (IInputType)new NonNullType(new StringType());

            // act
            IValueNode value = type.ParseValue("abc");

            // assert
            Assert.Equal(
                "abc",
                Assert.IsType<StringValueNode>(value).Value);
        }

        [Fact]
        public void IsInstanceOfType_Literal_NullValueLiteral()
        {
            // arrange
            var type = (IInputType)new NonNullType(new StringType());

            // act
            bool value = type.IsInstanceOfType(NullValueNode.Default);

            // assert
            Assert.False(value);
        }

        [Fact]
        public void IsInstanceOfType_Literal_StringValueLiteral()
        {
            // arrange
            var type = (IInputType)new NonNullType(new StringType());

            // act
            bool value = type.IsInstanceOfType(new StringValueNode("abc"));

            // assert
            Assert.True(value);
        }

        [Fact]
        public void IsInstanceOfType_Value_NullValue()
        {
            // arrange
            var type = (IInputType)new NonNullType(new StringType());

            // act
            bool value = type.IsInstanceOfType((object)null);

            // assert
            Assert.False(value);
        }

        [Fact]
        public void IsInstanceOfType_Value_StringValue()
        {
            // arrange
            var type = (IInputType)new NonNullType(new StringType());

            // act
            bool value = type.IsInstanceOfType("abc");

            // assert
            Assert.True(value);
        }

        [Fact]
        public void EnsureInstanceOfIsDelegatedToInnerType()
        {
            // arrange
            var innerType = new ListType(new StringType());

            var type = new NonNullType(innerType);
            bool shouldBeFalse = ((IInputType)type).IsInstanceOfType(
                new IntValueNode(123));
            bool shouldBeTrue = ((IInputType)type).IsInstanceOfType(
                new ListValueNode(new[] { new StringValueNode("foo") }));

            // assert
            Assert.False(shouldBeFalse);
            Assert.True(shouldBeTrue);
        }

        [Fact]
        public void Serialize_NullValue()
        {
            // arrange
            var type = (IInputType)new NonNullType(new StringType());

            // act
            Action action = () => type.Serialize(null);

            // assert
            Assert.Throws<ScalarSerializationException>(action);
        }

        [Fact]
        public void Serialize_StringValue()
        {
            // arrange
            var type = (IInputType)new NonNullType(new StringType());

            // act
            object value = type.Serialize("abc");

            // assert
            Assert.Equal("abc", value);
        }

        [Fact]
        public void Deserialize_NullValue()
        {
            // arrange
            var type = (IInputType)new NonNullType(new StringType());

            // act
            Action action = () => type.Deserialize(null);

            // assert
            Assert.Throws<ScalarSerializationException>(action);
        }

        [Fact]
        public void Deserialize_StringValue()
        {
            // arrange
            var type = (IInputType)new NonNullType(new StringType());

            // act
            object value = type.Deserialize("abc");

            // assert
            Assert.Equal("abc", value);
        }
    }
}
