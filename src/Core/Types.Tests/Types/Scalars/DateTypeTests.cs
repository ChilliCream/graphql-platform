using System;
using System.Globalization;
using System.Threading;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class DateTypeTests
    {
        [Fact]
        public void Serialize_Date()
        {
            // arrange
            var dateType = new DateType();
            var dateTime = new DateTime(
                2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);
            string expectedValue = "2018-06-11";

            // act
            string serializedValue = (string)dateType.Serialize(dateTime);

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        public void Serialize_DateTimeOffset()
        {
            // arrange
            var dateType = new DateType();
            var dateTime = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));
            string expectedValue = "2018-06-11";

            // act
            string serializedValue = (string)dateType.Serialize(dateTime);

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            var dateType = new DateType();

            // act
            object serializedValue = dateType.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void Serialize_String_Exception()
        {
            // arrange
            var dateType = new DateType();

            // act
            Action a = () => dateType.Serialize("foo");

            // assert
            Assert.Throws<ScalarSerializationException>(a);
        }

        [Fact]
        public void Deserialize_IsoString_DateTime()
        {
            // arrange
            var dateType = new DateType();
            var date = new DateTime(2018, 6, 11);

            // act
            var result = (DateTime)dateType.Deserialize("2018-06-11");

            // assert
            Assert.Equal(date, result);
        }

        [Fact]
        public void Deserialize_InvalidString_To_DateTimeOffset()
        {
            // arrange
            var type = new DateType();

            // act
            bool success = type.TryDeserialize("abc", out object deserialized);

            // assert
            Assert.False(success);
        }

        [Fact]
        public void Deserialize_DateTimeOffset_To_DateTime()
        {
            // arrange
            var type = new DateType();
            var time = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc));

            // act
            bool success = type.TryDeserialize(time, out object deserialized);

            // assert
            Assert.True(success);
            Assert.Equal(time.UtcDateTime,
                Assert.IsType<DateTime>(deserialized));
        }

        [Fact]
        public void Deserialize_DateTime_To_DateTime()
        {
            // arrange
            var type = new DateType();
            var time = new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);

            // act
            bool success = type.TryDeserialize(time, out object deserialized);

            // assert
            Assert.True(success);
            Assert.Equal(time, deserialized);
        }

        [Fact]
        public void Deserialize_NullableDateTime_To_DateTime()
        {
            // arrange
            var type = new DateType();
            DateTime? time =
                new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);

            // act
            bool success = type.TryDeserialize(time, out object deserialized);

            // assert
            Assert.True(success);
            Assert.Equal(time, Assert.IsType<DateTime>(deserialized));
        }

        [Fact]
        public void Deserialize_NullableDateTime_To_DateTime_2()
        {
            // arrange
            var type = new DateType();
            DateTime? time = null;

            // act
            bool success = type.TryDeserialize(time, out object deserialized);

            // assert
            Assert.True(success);
            Assert.Null(deserialized);
        }

        [Fact]
        public void Deserialize_Null_To_Null()
        {
            // arrange
            var type = new DateType();

            // act
            bool success = type.TryDeserialize(null, out object deserialized);

            // assert
            Assert.True(success);
            Assert.Null(deserialized);
        }

        [Fact]
        public void ParseLiteral_StringValueNode()
        {
            // arrange
            var dateType = new DateType();
            var literal = new StringValueNode("2018-06-29");
            var expectedDateTime = new DateTime(2018, 6, 29);

            // act
            var dateTime = (DateTime)dateType
                .ParseLiteral(literal);

            // assert
            Assert.Equal(expectedDateTime, dateTime);
        }

        [InlineData("en-US")]
        [InlineData("en-AU")]
        [InlineData("en-GB")]
        [InlineData("de-CH")]
        [InlineData("de-de")]
        [Theory]
        public void ParseLiteral_StringValueNode_DifferentCulture(
           string cultureName)
        {
            // arrange
            Thread.CurrentThread.CurrentCulture =
                CultureInfo.GetCultureInfo(cultureName);

            var dateType = new DateType();
            var literal = new StringValueNode("2018-06-29");
            var expectedDateTime = new DateTime(2018, 6, 29);

            // act
            var dateTime = (DateTime)dateType
                .ParseLiteral(literal);

            // assert
            Assert.Equal(expectedDateTime, dateTime);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            var dateType = new DateType();
            NullValueNode literal = NullValueNode.Default;

            // act
            object value = dateType.ParseLiteral(literal);

            // assert
            Assert.Null(value);
        }

        [Fact]
        public void ParseValue_DateTimeOffset()
        {
            // arrange
            var dateType = new DateType();
            var dateTime = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));
            string expectedLiteralValue = "2018-06-11";

            // act
            var stringLiteral =
                (StringValueNode)dateType.ParseValue(dateTime);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void ParseValue_DateTime()
        {
            // arrange
            var dateType = new DateType();
            var dateTime =
                new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);
            string expectedLiteralValue = "2018-06-11";

            // act
            var stringLiteral =
                (StringValueNode)dateType.ParseValue(dateTime);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void ParseValue_Null()
        {
            // arrange
            var dateType = new DateType();
            var dateTime =
                new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);
            string expectedLiteralValue = "2018-06-11";

            // act
            var stringLiteral =
                (StringValueNode)dateType.ParseValue(dateTime);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void EnsureDateTypeKindIsCorret()
        {
            // arrange
            var type = new DateType();

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }
    }
}
