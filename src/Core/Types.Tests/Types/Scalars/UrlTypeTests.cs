using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class UrlTypeTests
    {
        [Fact]
        public void EnsureUrlTypeKindIsCorrect()
        {
            // arrange
            var type = new UrlType();

            // act
            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }

        [Fact]
        public void ParseLiteral_StringValueNode()
        {
            // arrange
            var urlType = new UrlType();
            var expected = new Uri("http://domain.test/url");
            var literal = new StringValueNode(expected.AbsoluteUri);

            // act
            var actual = (Uri)urlType
                .ParseLiteral(literal);

            // assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            var urlType = new UrlType();
            NullValueNode literal = NullValueNode.Default;

            // act
            object value = urlType.ParseLiteral(literal);

            // assert
            Assert.Null(value);
        }

        [Fact]
        public void ParseLiteral_Invalid_Url_Throws()
        {
            // arrange
            var type = new UrlType();
            var input = new StringValueNode("$*^domain.test");

            // act
            // assert
            Assert.Throws<ScalarSerializationException>(
                () => type.ParseLiteral(input));
        }

        [Fact]
        public void ParseValue_Url()
        {
            // arrange
            var urlType = new UrlType();
            var uri = new Uri("http://domain.test/url");
            string expectedLiteralValue = uri.AbsoluteUri;

            // act
            var stringLiteral =
                (StringValueNode)urlType.ParseValue(uri);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void ParseValue_Encoded()
        {
            // arrange
            var urlType = new UrlType();
            var uri = new Uri("http://domain.test/ä+😄?q=a/α");
            string expectedLiteralValue = uri.AbsoluteUri;

            // act
            var stringLiteral =
                (StringValueNode)urlType.ParseValue(uri);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            var dateType = new UrlType();

            // act
            object serializedValue = dateType.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void Serialize_Url()
        {
            // arrange
            var urlType = new UrlType();
            var uri = new Uri("http://domain.test/url");

            // act
            object serializedValue = urlType.Serialize(uri);

            // assert
            Assert.Equal(uri.AbsoluteUri, Assert.IsType<string>(serializedValue));
        }

        [Fact]
        public void IsInstanceOfType_GivenUriAsStringValueNode_ReturnsTrue()
        {
            // Arrange
            var urlType = new UrlType();
            var uri = new Uri("http://domain.test/url");

            // Act
            var isUrlType = urlType.IsInstanceOfType(new StringValueNode(uri.AbsoluteUri));

            // Assert
            Assert.True(isUrlType);
        }

        [Fact]
        public void IsInstanceOfType_GivenNullValueNode_ReturnsTrue()
        {
            // arrange
            var urlType = new UrlType();

            // act
            var isUrlType = urlType.IsInstanceOfType(new NullValueNode(null));

            // assert
            Assert.True(isUrlType);
        }

        [Fact]
        public void IsInstanceOfType_GivenInvalidUriAsStringLiteral_False()
        {
            // arrange
            var urlType = new UrlType();

            // act
            var isUrlType = urlType.IsInstanceOfType(
                new StringValueNode("$*^domain.test"));

            // assert
            Assert.False(isUrlType);
        }

        [Fact]
        public void IsInstanceOfType_GivenNull_ThrowsArgumentException()
        {
            // arrange
            var urlType = new UrlType();

            // act
            Action action = () => urlType.IsInstanceOfType(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void IsInstanceOfType_GivenNonUrlValueNode_ReturnsFalse()
        {
            // arrange
            var urlType = new UrlType();
            var intValue = new IntValueNode(1);

            // act
            var isUrlType = urlType.IsInstanceOfType(intValue);

            // assert
            Assert.False(isUrlType);
        }

    }
}
