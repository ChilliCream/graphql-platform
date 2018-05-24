using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class IntegerTypeTests
    {
        [Fact]
        public void ParseLiteral()
        {
            // arrange
            IntValueNode literal = new IntValueNode(null, "12345");

            // act
            IntegerType integerType = new IntegerType();
            object result = integerType.ParseLiteral(literal);

            // assert
            Assert.IsType<int>(result);
            Assert.Equal(12345, result);
        }

        [Fact]
        public void IsInstanceOfType()
        {
            // arrange
            IntValueNode intLiteral = new IntValueNode(null, "12345");
            StringValueNode stringLiteral = new StringValueNode(null, "12345", false);
            NullValueNode nullLiteral = new NullValueNode(null);

            // act
            IntegerType integerType = new IntegerType();
            bool isIntLiteralInstanceOf = integerType.IsInstanceOfType(intLiteral);
            bool isStringLiteralInstanceOf = integerType.IsInstanceOfType(stringLiteral);
            bool isNullLiteralInstanceOf = integerType.IsInstanceOfType(nullLiteral);

            // assert
            Assert.True(isIntLiteralInstanceOf);
            Assert.False(isStringLiteralInstanceOf);
            Assert.True(isNullLiteralInstanceOf);
        }
    }
}
