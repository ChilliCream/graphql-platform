using System;
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
            NullValueNode nullLiteral = NullValueNode.Default;

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

        [Fact]
        public void Serialize_Null_Null()
        {
            // arrange
            BooleanType booleanType = new BooleanType();

            // act
            object result = booleanType.Serialize(null);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public void Serialize_True_True()
        {
            // arrange
            BooleanType booleanType = new BooleanType();

            // act
            object result = booleanType.Serialize(true);

            // assert
            Assert.IsType<bool>(result);
            Assert.True((bool)result);
        }

        [Fact]
        public void Serialize_String_Exception()
        {
            // arrange
            BooleanType booleanType = new BooleanType();

            // act
            Action a = () => booleanType.Serialize("foo");

            // assert
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void Deserialize_Null_Null()
        {
            // arrange
            BooleanType booleanType = new BooleanType();

            // act
            object result = booleanType.Serialize(null);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public void Deserialize_True_True()
        {
            // arrange
            BooleanType booleanType = new BooleanType();

            // act
            object result = booleanType.Serialize(true);

            // assert
            Assert.IsType<bool>(result);
            Assert.True((bool)result);
        }

        [Fact]
        public void Deserialize_String_Exception()
        {
            // arrange
            BooleanType booleanType = new BooleanType();

            // act
            Action a = () => booleanType.Serialize("foo");

            // assert
            Assert.Throws<ArgumentException>(a);
        }
    }
}
