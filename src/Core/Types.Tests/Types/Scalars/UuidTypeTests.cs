using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class UuidTypeTests
    {
        [Fact]
        public void Serialize_Guid()
        {
            // arrange
            var uuidType = new UuidType();
            var guid = Guid.NewGuid();
            var expectedValue = guid.ToString("N");

            // act
            var serializedValue = (string)uuidType.Serialize(guid);

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            var uuidType = new UuidType();

            // act
            var serializedValue = uuidType.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void ParseLiteral_StringValueNode()
        {
            // arrange
            var uuidType = new UuidType();
            var expected = Guid.NewGuid();
            var literal = new StringValueNode(expected.ToString());

            // act
            var actual = (Guid)uuidType
                .ParseLiteral(literal);

            // assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            var uuidType = new UuidType();
            NullValueNode literal = NullValueNode.Default;

            // act
            var value = uuidType.ParseLiteral(literal);

            // assert
            Assert.Null(value);
        }

        [Fact]
        public void ParseValue_Guid()
        {
            // arrange
            var uuidType = new UuidType();
            var expected = Guid.NewGuid();
            var expectedLiteralValue = expected.ToString("N");

            // act
            var stringLiteral =
                (StringValueNode)uuidType.ParseValue(expected);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void ParseValue_Null()
        {
            // arrange
            var uuidType = new UuidType();
            Guid? guid = null;

            // act
            IValueNode stringLiteral =
                uuidType.ParseValue(guid);

            // assert
            Assert.True(stringLiteral is NullValueNode);
            Assert.Null(((NullValueNode)stringLiteral).Value);
        }

        [Fact]
        public void EnsureDateTypeKindIsCorret()
        {
            // arrange
            var type = new UuidType();

            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }
    }
}
