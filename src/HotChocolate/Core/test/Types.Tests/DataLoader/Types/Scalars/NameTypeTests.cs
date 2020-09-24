using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class NameTypeTests
    {
        [Fact]
        public void EnsureStringTypeKindIsCorret()
        {
            // arrange
            var type = new NameType();

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }

        [Fact]
        public void IsInstanceOfType_ValueNode()
        {
            // arrange
            var type = new NameType();
            var input = new StringValueNode("_123456");

            // act
            bool result = type.IsInstanceOfType(input);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_NullValueNode()
        {
            // arrange
            var type = new NameType();
            NullValueNode input = NullValueNode.Default;

            // act
            bool result = type.IsInstanceOfType(input);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_Wrong_ValueNode()
        {
            // arrange
            var type = new NameType();
            var input = new IntValueNode(123456);

            // act
            bool result = type.IsInstanceOfType(input);

            // assert
            Assert.False(result);
        }

        [InlineData("1234")]
        [InlineData("  ")]
        [Theory]
        public void IsInstanceOfType_Wrong_StringValue(string s)
        {
            // arrange
            var type = new NameType();
            var input = new StringValueNode(s);

            // act
            bool result = type.IsInstanceOfType(input);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsInstanceOfType_Null_Throws()
        {
            // arrange
            var type = new NameType();

            // act
            // assert
            Assert.Throws<ArgumentNullException>(
                () => type.IsInstanceOfType(null));
        }

        [Fact]
        public void Serialize_Type()
        {
            // arrange
            var type = new NameType();
            NameString input = "_123456";

            // act
            object serializedValue = type.Serialize(input);

            // assert
            Assert.IsType<string>(serializedValue);
            Assert.Equal("_123456", serializedValue);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            var type = new NameType();

            // act
            object serializedValue = type.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void Serialize_Wrong_Type_Throws()
        {
            // arrange
            var type = new NameType();
            object input = 123456;

            // act
            // assert
            Assert.Throws<SerializationException>(
                () => type.Serialize(input));
        }

        [Fact]
        public void ParseLiteral_ValueNode()
        {
            // arrange
            var type = new NameType();
            var input = new StringValueNode("__123456");

            // act
            object output = type.ParseLiteral(input);

            // assert
            Assert.IsType<NameString>(output);
            Assert.Equal(new NameString("__123456"), output);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            var type = new NameType();
            NullValueNode input = NullValueNode.Default;

            // act
            object output = type.ParseLiteral(input);

            // assert
            Assert.Null(output);
        }

        [Fact]
        public void ParseLiteral_Wrong_ValueNode_Throws()
        {
            // arrange
            var type = new NameType();
            var input = new IntValueNode(123456);

            // act
            // assert
            Assert.Throws<SerializationException>(
                () => type.ParseLiteral(input));
        }

        [Fact]
        public void ParseLiteral_Null_Throws()
        {
            // arrange
            var type = new NameType();

            // act
            // assert
            Assert.Throws<ArgumentNullException>(() => type.ParseLiteral(null));
        }

        [Fact]
        public void ParseValue_Wrong_Value_Throws()
        {
            // arrange
            var type = new NameType();
            object input = 123456;

            // act
            // assert
            Assert.Throws<SerializationException>(
                () => type.ParseValue(input));
        }

        [Fact]
        public void ParseValue_Null()
        {
            // arrange
            var type = new NameType();
            object input = null;

            // act
            object output = type.ParseValue(input);

            // assert
            Assert.IsType<NullValueNode>(output);
        }
    }
}
