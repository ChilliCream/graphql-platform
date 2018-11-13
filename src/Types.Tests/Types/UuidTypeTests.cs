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
            UuidType uuidType = new UuidType();
            Guid guid = Guid.NewGuid();
            string expectedValue = guid.ToString("N");

            // act
            string serializedValue = (string)uuidType.Serialize(guid);

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            UuidType uuidType = new UuidType();

            // act
            object serializedValue = uuidType.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void ParseLiteral_StringValueNode()
        {
            // arrange
            UuidType uuidType = new UuidType();
            Guid expected = Guid.NewGuid();
            StringValueNode literal = new StringValueNode(expected.ToString());

            // act
            Guid actual = (Guid)uuidType
                .ParseLiteral(literal);

            // assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            UuidType uuidType = new UuidType();
            NullValueNode literal = NullValueNode.Default;

            // act
            object value = uuidType.ParseLiteral(literal);

            // assert
            Assert.Null(value);
        }

        [Fact]
        public void ParseValue_Guid()
        {
            // arrange
            UuidType uuidType = new UuidType();
            Guid expected = Guid.NewGuid();
            string expectedLiteralValue = expected.ToString("N");

            // act
            StringValueNode stringLiteral =
                (StringValueNode)uuidType.ParseValue(expected);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void ParseValue_Null()
        {
            // arrange
            UuidType uuidType = new UuidType();
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
            UuidType type = new UuidType();

            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }
    }
}
