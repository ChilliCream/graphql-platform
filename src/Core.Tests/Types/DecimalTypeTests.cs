using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class DecimalTypeTests
    {
        [Fact]
        public void Serialize_Decimal()
        {
            // arrange
            DecimalType type = new DecimalType();
            decimal input = 1.0m;

            // act
            object serializedValue = type.Serialize(input);

            // assert
            Assert.IsType<decimal>(serializedValue);
            Assert.Equal(1.0m, serializedValue);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            DecimalType type = new DecimalType();

            // act
            object serializedValue = type.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void ParseLiteral_FloatValueNode()
        {
            // arrange
            DecimalType type = new DecimalType();
            FloatValueNode input = new FloatValueNode("1.000000e+000");

            // act
            object output = type.ParseLiteral(input);

            // assert
            Assert.IsType<decimal>(output);
            Assert.Equal(1.0m, output);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            DecimalType type = new DecimalType();
            NullValueNode input = new NullValueNode();

            // act
            object output = type.ParseLiteral(input);

            // assert
            Assert.Null(output);
        }

        [Fact]
        public void ParseValue_Decimal_Max()
        {
            // arrange
            DecimalType type = new DecimalType();
            decimal input = decimal.MaxValue;
            string expectedLiteralValue = "7.922816e+028";

            // act
            FloatValueNode literal =
                (FloatValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(expectedLiteralValue, literal.Value, StringComparer.InvariantCulture);
        }

        [Fact]
        public void ParseValue_Decimal_Min()
        {
            // arrange
            DecimalType type = new DecimalType();
            decimal input = decimal.MinValue;
            string expectedLiteralValue = "-7.922816e+028";

            // act
            FloatValueNode literal =
                (FloatValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(expectedLiteralValue, literal.Value, StringComparer.InvariantCulture);
        }

        [Fact]
        public void ParseValue_Null()
        {
            // arrange
            DecimalType type = new DecimalType();
            object input = null;

            // act
            object output = type.ParseValue(input);

            // assert
            Assert.IsType<NullValueNode>(output);
        }
    }
}
