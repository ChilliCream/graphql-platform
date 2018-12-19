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
            UrlType type = new UrlType();

            // act
            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }

        [Fact]
        public void ParseLiteral_StringValueNode()
        {
            // arrange
            UrlType urlType = new UrlType();
            Uri expected = new Uri("http://domain.test/url");
            StringValueNode literal = new StringValueNode(expected.AbsoluteUri);

            // act
            Uri actual = (Uri)urlType
                .ParseLiteral(literal);

            // assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            UrlType urlType = new UrlType();
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
            UrlType type = new UrlType();
            StringValueNode input = new StringValueNode("$*^domain.test");

            // act
            // assert
            Assert.Throws<ArgumentException>(() => type.ParseLiteral(input));
        }

        [Fact]
        public void ParseValue_Url()
        {
            // arrange
            UrlType urlType = new UrlType();
            Uri uri = new Uri("http://domain.test/url");
            string expectedLiteralValue = uri.AbsoluteUri;

            // act
            StringValueNode stringLiteral =
                (StringValueNode)urlType.ParseValue(uri);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void ParseValue_Encoded()
        {
            // arrange
            UrlType urlType = new UrlType();
            Uri uri = new Uri("http://domain.test/ä+😄?q=a/α");
            string expectedLiteralValue = uri.AbsoluteUri;

            // act
            StringValueNode stringLiteral =
                (StringValueNode)urlType.ParseValue(uri);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            UrlType dateType = new UrlType();

            // act
            object serializedValue = dateType.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void Serialize_Url()
        {
            // arrange
            UrlType urlType = new UrlType();
            Uri uri = new Uri("http://domain.test/url");
            string expectedValue = uri.AbsoluteUri;

            // act
            string serializedValue = (string)urlType.Serialize(uri);

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }
    }
}
