using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class BooleanTypeTests
    {
        [Fact]
        public void ParseLiteral()
        {
            // arrange
            BooleanValueNode literal = new BooleanValueNode(null, true);

            // act
            BooleanType booleanType = new BooleanType();
            object result = booleanType.ParseLiteral(literal);

            // assert
            Assert.IsType<bool>(result);
            Assert.True((bool)result);
        }

        [Fact]
        public void IsInstanceOfType()
        {
            // arrange
            BooleanValueNode boolLiteral = new BooleanValueNode(null, true);
            StringValueNode stringLiteral = new StringValueNode(null, "12345", false);
            NullValueNode nullLiteral = new NullValueNode(null);

            // act
            BooleanType booleanType = new BooleanType();
            bool isIntLiteralInstanceOf = booleanType.IsInstanceOfType(boolLiteral);
            bool isStringLiteralInstanceOf = booleanType.IsInstanceOfType(stringLiteral);
            bool isNullLiteralInstanceOf = booleanType.IsInstanceOfType(nullLiteral);

            // assert
            Assert.True(isIntLiteralInstanceOf);
            Assert.False(isStringLiteralInstanceOf);
            Assert.True(isNullLiteralInstanceOf);
        }

        [Fact]
        public void EnsureBooleanTypeKindIsCorret()
        {
            // arrange
            BooleanType type = new BooleanType();

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }
    }
}
