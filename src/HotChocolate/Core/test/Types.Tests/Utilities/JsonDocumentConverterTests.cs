using System;
using System.Collections;
using System.Text.Json;

using Xunit;

namespace HotChocolate.Utilities
{
    public class JsonDocumentConverterTests
    {
        [Fact]
        public void Convert_NullObject_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => JsonDocumentConverter.Convert(null));
        }

        [Fact]
        public void Convert_ObjectNotKnowType_ReturnsNull()
        {
            object obj = "";

            object result = JsonDocumentConverter.Convert(obj);

            Assert.Null(result);
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
        public void Convert_SimpleJson_ReturnsType(string json, Type type)
        {
            var obj = JsonSerializer.Deserialize<object>(json);

            object result = JsonDocumentConverter.Convert(obj);

            Assert.NotNull(result);
            Assert.IsType(type, result);
        }

        [Fact]
        public void Convert_ComplexJson_ReturnsObject()
        {
            var obj = JsonSerializer.Deserialize<object>("{ \"foo\": { \"bar\": { \"id\": 1, \"name\": \"name\" } } }");

            object result = JsonDocumentConverter.Convert(obj);

            Assert.NotNull(result);
            Assert.IsAssignableFrom<IDictionary>(result);
        }
    }
}
