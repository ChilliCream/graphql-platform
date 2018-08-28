using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class GuidTypeTests
    {
        [Fact]
        public void Serialize_Guid()
        {
            // arrange
            GuidType guidType = new GuidType();
            Guid guid = Guid.NewGuid();
            string expectedValue = guid.ToString();

            // act
            string serializedValue = (string)guidType.Serialize(guid);

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            GuidType guidType = new GuidType();

            // act
            object serializedValue = guidType.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void ParseLiteral_StringValueNode()
        {
            // arrange
            GuidType guidType = new GuidType();
            Guid expected = Guid.NewGuid();
            StringValueNode literal = new StringValueNode(expected.ToString());

            // act
            Guid actual = (Guid)guidType
                .ParseLiteral(literal);

            // assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            GuidType guidType = new GuidType();
            NullValueNode literal = NullValueNode.Default;

            // act
            object value = guidType.ParseLiteral(literal);

            // assert
            Assert.Null(value);
        }

        [Fact]
        public void ParseValue_Guid()
        {
            // arrange
            GuidType guidType = new GuidType();
            Guid expected = Guid.NewGuid();
            string expectedLiteralValue = expected.ToString();

            // act
            StringValueNode stringLiteral =
                (StringValueNode)guidType.ParseValue(expected);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void ParseValue_Null()
        {
            // arrange
            GuidType guidType = new GuidType();
            Guid? guid = null;

            // act
            IValueNode stringLiteral =
                guidType.ParseValue(guid);

            // assert
            Assert.True(stringLiteral is NullValueNode);
            Assert.Null(((NullValueNode)stringLiteral).Value);
        }

        [Fact]
        public void EnsureDateTypeKindIsCorret()
        {
            // arrange
            GuidType type = new GuidType();

            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }
    }
}
