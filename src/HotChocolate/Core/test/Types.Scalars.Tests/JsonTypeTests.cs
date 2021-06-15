using System;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Tests;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class JsonTypeTests : ScalarTypeTestBase
    {
        [Fact]
        protected void Schema_WithScalar_IsMatch()
        {
            // arrange
            ISchema schema = BuildSchema<JsonType>();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Json_EnsureJsonTypeKindIsCorrect()
        {
            // arrange
            // act
            JsonType type = new()!;

            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }

        [Fact]
        protected void Json_ExpectIsObjectInstanceToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<JsonType>();
            ObjectValueNode valueSyntax = new(new ObjectFieldNode("key","value"));

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Fact]
        protected void Json_ExpectParseResultToMatchNull()
        {
            // arrange
            ScalarType scalar = new JsonType();
            object valueSyntax = null!;

            // act
            IValueNode result = scalar.ParseResult(valueSyntax);

            // assert
            Assert.Equal(typeof(NullValueNode), result.GetType());
        }

        [Fact]
        protected void Json_ExpectParseResultToThrowOnInvalidValueNode()
        {
            // arrange
            ScalarType scalar = new JsonType();
            var valueSyntax = "invalid";

            // act
            Exception? result = Record.Exception(() => scalar.ParseResult(valueSyntax));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Theory]
        [InlineData("\"3000-01-01T00:00:00\"", typeof(StringValueNode))]
        [InlineData("\"Hotchocolate\"", typeof(StringValueNode))]
        [InlineData("-32768", typeof(IntValueNode))]
        [InlineData("32767", typeof(IntValueNode))]
        [InlineData("-2147483648", typeof(IntValueNode))]
        [InlineData("2147483647", typeof(IntValueNode))]
        [InlineData("-9223372036854775808", typeof(IntValueNode))]
        [InlineData("9223372036854775807", typeof(IntValueNode))]
        [InlineData("-79228162514264337593543950335", typeof(FloatValueNode))]
        [InlineData("79228162514264337593543950335", typeof(FloatValueNode))]
        [InlineData("-1.7976931348623157E+308", typeof(FloatValueNode))]
        [InlineData("1.7976931348623157E+308", typeof(FloatValueNode))]
        [InlineData("-3.40282347E+38", typeof(FloatValueNode))]
        [InlineData("3.40282347E+38", typeof(FloatValueNode))]
        [InlineData("\"b8055c6e-bed0-4255-b1c3-01414b0a30dd\"", typeof(StringValueNode))]
        [InlineData("false", typeof(BooleanValueNode))]
        [InlineData("true", typeof(BooleanValueNode))]
        protected void Json_ExpectParseResultToMatchType(string json, Type type)
        {
            // arrange
            ScalarType scalar = new JsonType();
            object valueSyntax = JsonSerializer.Deserialize<object>(json);

            // act
            IValueNode result = scalar.ParseResult(valueSyntax);

            // assert
            Assert.IsType(type, result);
        }

        [Theory]
        [InlineData("\"3000-01-01T00:00:00\"", typeof(DateTimeOffset))]
        [InlineData("\"Hotchocolate\"", typeof(string))]
        [InlineData("-32768", typeof(short))]
        [InlineData("32767", typeof(short))]
        [InlineData("-2147483648", typeof(int))]
        [InlineData("2147483647", typeof(int))]
        [InlineData("-9223372036854775808", typeof(long))]
        [InlineData("9223372036854775807", typeof(long))]
        [InlineData("-79228162514264337593543950335", typeof(decimal))]
        [InlineData("79228162514264337593543950335", typeof(decimal))]
        [InlineData("-1.7976931348623157E+308", typeof(double))]
        [InlineData("1.7976931348623157E+308", typeof(double))]
        [InlineData("-3.40282347E+38", typeof(float))]
        [InlineData("3.40282347E+38", typeof(float))]
        [InlineData("\"b8055c6e-bed0-4255-b1c3-01414b0a30dd\"", typeof(Guid))]
        [InlineData("false", typeof(bool))]
        [InlineData("true", typeof(bool))]
        protected void Json_ExpectParseLiteralToMatch(string literal, Type type)
        {
            // arrange
            ScalarType scalar = CreateType<JsonType>();
            StringValueNode valueSyntax = new(literal);

            // act
            object value = scalar.ParseLiteral(valueSyntax)!;

            // assert
            Assert.IsType(type, value);
        }

        [Fact]
        protected void Json_ExpectParseLiteralToThrowSerializationException()
        {
            // arrange
            ScalarType scalar = CreateType<JsonType>();
            StringValueNode valueSyntax = new("foo");

            // act
            Exception? result = Record.Exception(() => scalar.ParseLiteral(valueSyntax));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        public void Latitude_ParseLiteral_NullValueNode()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            NullValueNode literal = NullValueNode.Default;

            // act
            object value = scalar.ParseLiteral(literal)!;

            // assert
            Assert.Null(value);
        }
    }
}
