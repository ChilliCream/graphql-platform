using System.Text;
using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class ByteArrayTypeTests
    {
        [Fact]
        public void IsInstanceOfType_StringLiteral()
        {
            // arrange
            string base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes("value"));
            var type = new ByteArrayType();
            var literal = new StringValueNode(base64);

            // act
            var result = type.IsInstanceOfType(literal);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_NullLiteral_True()
        {
            // arrange
            var type = new ByteArrayType();

            // act
            var result = type.IsInstanceOfType(NullValueNode.Default);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_IntLiteral_False()
        {
            // arrange
            var type = new ByteArrayType();
            var literal = new IntValueNode(123);

            // act
            var result = type.IsInstanceOfType(literal);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsInstanceOfType_Null()
        {
            // arrange
            var type = new ByteArrayType();

            // act
            Action action = () => type.IsInstanceOfType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Serialize_Type()
        {
            // arrange
            var type = new ByteArrayType();
            string value = "value";
            string base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(value));


            // act
            var serializedValue = type.Serialize(base64);

            // assert
            Assert.IsType<byte[]>(serializedValue);
            Assert.Equal(value, serializedValue);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            var type = new ByteArrayType();

            // act
            var serializedValue = type.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void Serialize_Wrong_Type_Throws()
        {
            // arrange
            var type = new ByteArrayType();
            var input = 123;

            // act
            // assert
            Assert.Throws<ScalarSerializationException>(
                () => type.Serialize(input));
        }

        [Fact]
        public void ParseLiteral_IntLiteral()
        {
            // arrange
            var type = new ByteArrayType();
            var literal = new IntValueNode(1);

            // act
            var value = type.ParseLiteral(literal);

            // assert
            Assert.IsType<byte>(value);
            Assert.Equal(literal.ToByte(), value);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            var type = new ByteArrayType();

            // act
            var output = type.ParseLiteral(NullValueNode.Default);

            // assert
            Assert.Null(output);
        }

        [Fact]
        public void ParseLiteral_Wrong_ValueNode_Throws()
        {
            // arrange
            var type = new ByteArrayType();
            var input = new StringValueNode("abc");

            // act
            // assert
            Assert.Throws<ScalarSerializationException>(
                () => type.ParseLiteral(input));
        }

        [Fact]
        public void ParseLiteral_Null_Throws()
        {
            // arrange
            var type = new ByteArrayType();

            // act
            // assert
            Assert.Throws<ArgumentNullException>(
                () => type.ParseLiteral(null));
        }

        [Fact]
        public void ParseValue_MaxValue()
        {
            // arrange
            var type = new ByteArrayType(1, 100);
            byte input = 100;

            // act
            var literal = (IntValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(100, literal.ToByte());
        }

        [Fact]
        public void ParseValue_MaxValue_Violation()
        {
            // arrange
            var type = new ByteArrayType(1, 100);
            byte input = 101;

            // act
            Action action = () => type.ParseValue(input);

            // assert
            Assert.Throws<ScalarSerializationException>(action);
        }

        [Fact]
        public void ParseValue_MinValue()
        {
            // arrange
            var type = new ByteArrayType(1, 100);
            byte input = 1;

            // act
            var literal = (IntValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(1, literal.ToByte());
        }

        [Fact]
        public void ParseValue_MinValue_Violation()
        {
            // arrange
            var type = new ByteArrayType(1, 100);
            byte input = 0;

            // act
            Action action = () => type.ParseValue(input);

            // assert
            Assert.Throws<ScalarSerializationException>(action);
        }


        [Fact]
        public void ParseValue_Wrong_Value_Throws()
        {
            // arrange
            var type = new ByteArrayType();
            var value = "123";

            // act
            // assert
            Assert.Throws<ScalarSerializationException>(
                () => type.ParseValue(value));
        }

        [Fact]
        public void ParseValue_Null()
        {
            // arrange
            var type = new ByteArrayType();
            object input = null;

            // act
            object output = type.ParseValue(input);

            // assert
            Assert.IsType<NullValueNode>(output);
        }

        [Fact]
        public void ParseValue_Nullable()
        {
            // arrange
            var type = new ByteArrayType();
            byte? input = 123;

            // act
            IntValueNode output = (IntValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(123, output.ToDouble());
        }

        [Fact]
        public void Ensure_TypeKind_is_Scalar()
        {
            // arrange
            var type = new ByteArrayType();

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, kind);
        }
    }
}
