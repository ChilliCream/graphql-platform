using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class LongTests
    {
        [Fact]
        public void Serialize_Long()
        {
            // arrange
            LongType type = new LongType();
            long input = 1L;

            // act
            object serializedValue = type.Serialize(input);

            // assert
            Assert.IsType<long>(serializedValue);
            Assert.Equal(1L, serializedValue);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            LongType type = new LongType();

            // act
            object serializedValue = type.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void ParseLiteral_IntValueNode()
        {
            // arrange
            LongType type = new LongType();
            IntValueNode input = new IntValueNode("1");

            // act
            object output = type.ParseLiteral(input);

            // assert
            Assert.IsType<long>(output);
            Assert.Equal(1L, output);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            LongType type = new LongType();
            NullValueNode input = new NullValueNode();

            // act
            object output = type.ParseLiteral(input);

            // assert
            Assert.Null(output);
        }

        [Fact]
        public void ParseValue_Long_Max()
        {
            // arrange
            LongType type = new LongType();
            long input = long.MaxValue;
            string expectedLiteralValue = "9223372036854775807";

            // act
            IntValueNode literal =
                (IntValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(expectedLiteralValue, literal.Value);
        }

        [Fact]
        public void ParseValue_Long_Min()
        {
            // arrange
            LongType type = new LongType();
            long input = long.MinValue;
            string expectedLiteralValue = "-9223372036854775808";

            // act
            IntValueNode literal =
                (IntValueNode)type.ParseValue(input);

            // assert
            Assert.Equal(expectedLiteralValue, literal.Value);
        }

        [Fact]
        public void ParseValue_Null()
        {
            // arrange
            LongType type = new LongType();
            object input = null;

            // act
            object output = type.ParseValue(input);

            // assert
            Assert.IsType<NullValueNode>(output);
        }


        [Fact]
        public void EnsureFloatTypeKindIsCorret()
        {
            // arrange
            LongType type = new LongType();

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, kind);
        }
    }
}
