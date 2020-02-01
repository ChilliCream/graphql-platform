using System;
using System.Text;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class DecimalTypeTests
    {
        [Fact]
        public void IsInstanceOfType_FloatLiteral_True()
        {
            // arrange
            var type = new DecimalType();

            // act
            var result = type.IsInstanceOfType(CreateExponentialLiteral());

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_NullLiteral_True()
        {
            // arrange
            var type = new DecimalType();

            // act
            var result = type.IsInstanceOfType(NullValueNode.Default);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_IntLiteral_True()
        {
            // arrange
            var type = new DecimalType();

            // act
            var result = type.IsInstanceOfType(new IntValueNode(123));

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IsInstanceOfType_StringLiteral_False()
        {
            // arrange
            var type = new DecimalType();

            // act
            var result = type.IsInstanceOfType(new StringValueNode("123"));

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IsInstanceOfType_Null_Throws()
        {
            // arrange
            var type = new DecimalType();

            // act
            // assert
            Assert.Throws<ArgumentNullException>(
                () => type.IsInstanceOfType(null));
        }

        [Fact]
        public void Serialize_Type()
        {
            // arrange
            var type = new DecimalType();
            decimal value = 123.456M;

            // act
            var serializedValue = type.Serialize(value);

            // assert
            Assert.IsType<decimal>(serializedValue);
            Assert.Equal(value, serializedValue);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            var type = new DecimalType();

            // act
            var serializedValue = type.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void Serialize_Wrong_Type_Throws()
        {
            // arrange
            var type = new DecimalType();
            var input = "abc";

            // act
            // assert
            Assert.Throws<ScalarSerializationException>(
                () => type.Serialize(input));
        }

        [Fact]
        public void Serialize_MaxValue_Violation()
        {
            // arrange
            var type = new DecimalType(0, 100);
            decimal value = 123.456M;

            // act
            // assert
            Assert.Throws<ScalarSerializationException>(
                () => type.Serialize(value));
        }

        [Fact]
        public void ParseLiteral_FixedPointLiteral()
        {
            // arrange
            var type = new DecimalType();
            FloatValueNode literal = CreateFixedPointLiteral();

            // act
            var value = type.ParseLiteral(literal);

            // assert
            Assert.IsType<decimal>(value);
            Assert.Equal(literal.ToDecimal(), value);
        }

        [Fact]
        public void ParseLiteral_ExponentialLiteral()
        {
            // arrange
            var type = new DecimalType();
            FloatValueNode literal = CreateExponentialLiteral();

            // act
            var value = type.ParseLiteral(literal);

            // assert
            Assert.IsType<decimal>(value);
            Assert.Equal(literal.ToDecimal(), value);
        }

        [Fact]
        public void ParseLiteral_IntLiteral()
        {
            // arrange
            var type = new DecimalType();
            var literal = new IntValueNode(123);

            // act
            var value = type.ParseLiteral(literal);

            // assert
            Assert.IsType<decimal>(value);
            Assert.Equal(literal.ToDecimal(), value);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            var type = new DecimalType();

            // act
            var output = type.ParseLiteral(NullValueNode.Default);

            // assert
            Assert.Null(output);
        }

        [Fact]
        public void ParseLiteral_Wrong_ValueNode_Throws()
        {
            // arrange
            var type = new DecimalType();
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
            var type = new DecimalType();

            // act
            // assert
            Assert.Throws<ArgumentNullException>(
                () => type.ParseLiteral(null));
        }

        [Fact]
        public void ParseValue_MaxValue()
        {
            // arrange
            var type = new DecimalType(1, 100);
            decimal input = 100M;

            // act
            var literal = (FloatValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(100M, literal.ToDecimal());
        }

        [Fact]
        public void ParseValue_MaxValue_Violation()
        {
            // arrange
            var type = new DecimalType(1, 100);
            decimal input = 101M;

            // act
            Action action = () => type.ParseValue(input);

            // assert
            Assert.Throws<ScalarSerializationException>(action);
        }

        [Fact]
        public void ParseValue_MinValue()
        {
            // arrange
            var type = new DecimalType(1, 100);
            decimal input = 1M;

            // act
            var literal = (FloatValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(1M, literal.ToDecimal());
        }

        [Fact]
        public void ParseValue_MinValue_Violation()
        {
            // arrange
            var type = new DecimalType(1, 100);
            decimal input = 0M;

            // act
            Action action = () => type.ParseValue(input);

            // assert
            Assert.Throws<ScalarSerializationException>(action);
        }


        [Fact]
        public void ParseValue_Wrong_Value_Throws()
        {
            // arrange
            var type = new DecimalType();
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
            var type = new DecimalType();
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
            var type = new DecimalType();
            decimal? input = 123M;

            // act
            FloatValueNode output = (FloatValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(123M, output.ToDecimal());
        }

        [Fact]
        public void Ensure_TypeKind_is_Scalar()
        {
            // arrange
            var type = new DecimalType();

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, kind);
        }

        private FloatValueNode CreateExponentialLiteral() =>
            new FloatValueNode(Encoding.UTF8.GetBytes("1.000000E+000"), FloatFormat.Exponential);

        private FloatValueNode CreateFixedPointLiteral() =>
            new FloatValueNode(Encoding.UTF8.GetBytes("1.23"), FloatFormat.FixedPoint);
    }
}
