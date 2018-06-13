using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class FloatTests
    {
        [Fact]
        public void Serialize_Float()
        {
            // arrange
            FloatType type = new FloatType();
            float input = 1.0f;

            // act
            object serializedValue = type.Serialize(input);

            // assert
            Assert.IsType<float>(serializedValue);
            Assert.Equal(1.0f, serializedValue);
        }

        [Fact]
        public void Serialize_Double()
        {
            // arrange
            FloatType type = new FloatType();
            double input = 1.0d;

            // act
            object serializedValue = type.Serialize(input);

            // assert
            Assert.IsType<double>(serializedValue);
            Assert.Equal(1.0d, serializedValue);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            FloatType type = new FloatType();

            // act
            object serializedValue = type.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void ParseLiteral_FloatValueNode()
        {
            // arrange
            FloatType type = new FloatType();
            FloatValueNode input = new FloatValueNode("1.000000e+000");

            // act
            object output = type.ParseLiteral(input);

            // assert
            Assert.IsType<double>(output);
            Assert.Equal(1.0d, output);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            FloatType type = new FloatType();
            NullValueNode input = new NullValueNode();

            // act
            object output = type.ParseLiteral(input);

            // assert
            Assert.Null(output);
        }

        [Fact]
        public void ParseValue_Float()
        {
            // arrange
            FloatType type = new FloatType();
            float input = 1.0f;
            string expectedLiteralValue = "1.000000e+000";

            // act
            FloatValueNode literal =
                (FloatValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(expectedLiteralValue, literal.Value);
        }

        [Fact]
        public void ParseValue_Double()
        {
            // arrange
            FloatType type = new FloatType();
            double input = 1.0d;
            string expectedLiteralValue = "1.000000e+000";

            // act
            FloatValueNode literal =
                (FloatValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(expectedLiteralValue, literal.Value);
        }

        [Fact]
        public void ParseValue_Null()
        {
            // arrange
            FloatType type = new FloatType();
            object input = null;

            // act
            object output = type.ParseValue(input);

            // assert
            Assert.IsType<NullValueNode>(output);
        }
    }
}
