using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class IdTypeTests
    {
        [Fact]
        public void EnsureStringTypeKindIsCorret()
        {
            // arrange
            var type = new IdType();

            // act
            TypeKind kind = type.Kind;

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
            bool result = type.IsInstanceOfType(input);

            // assert
            Assert.True(result);
        }


        [Fact]
        public void IsInstanceOfType_IntValueNode()
        {
            // arrange
            var type = new IdType();
            var input = new IntValueNode("123456");

            // act
            bool result = type.IsInstanceOfType(input);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_NullValueNode()
        {
            // arrange
            var type = new IdType();
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
            var type = new IdType();
            var input = new FloatValueNode("123456");

            // act
            bool result = type.IsInstanceOfType(input);

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
            Assert.Throws<ArgumentNullException>(() => type.IsInstanceOfType(null));
        }

        [Fact]
        public void Serialize_String()
        {
            // arrange
            var type = new IdType();
            var input = "123456";

            // act
            object serializedValue = type.Serialize(input);

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
            object serializedValue = type.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void Serialize_Wrong_Type_Throws()
        {
            // arrange
            var type = new IdType();
            object input = Guid.NewGuid();

            // act
            // assert
            Assert.Throws<ArgumentException>(() => type.Serialize(input));
        }

        [Fact]
        public void ParseLiteral_StringValueNode()
        {
            // arrange
            var type = new IdType();
            var input = new StringValueNode("123456");

            // act
            object output = type.ParseLiteral(input);

            // assert
            Assert.IsType<string>(output);
            Assert.Equal("123456", output);
        }

        [Fact]
        public void ParseLiteral_IntValueNode()
        {
            // arrange
            var type = new IdType();
            var input = new IntValueNode("123456");

            // act
            object output = type.ParseLiteral(input);

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
            object output = type.ParseLiteral(input);

            // assert
            Assert.Null(output);
        }

        [Fact]
        public void ParseLiteral_Wrong_ValueNode_Throws()
        {
            // arrange
            var type = new IdType();
            FloatValueNode input = new FloatValueNode("123456");

            // act
            // assert
            Assert.Throws<ArgumentException>(() => type.ParseLiteral(input));
        }

        [Fact]
        public void ParseLiteral_Null_Throws()
        {
            // arrange
            var type = new IdType();

            // act
            // assert
            Assert.Throws<ArgumentNullException>(() => type.ParseLiteral(null));
        }

        [Fact]
        public void ParseValue_Wrong_Value_Throws()
        {
            // arrange
            var type = new IdType();
            object input = 123.456;

            // act
            // assert
            Assert.Throws<ArgumentException>(() => type.ParseValue(input));
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
}
