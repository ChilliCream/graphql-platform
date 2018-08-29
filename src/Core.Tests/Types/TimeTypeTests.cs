using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class TimeTypeTests
    {
        [Fact]
        public void Serialize_TimeSpan()
        {
            // arrange
            var dateTimeType = new TimeType();
            var time = new TimeSpan(8, 46, 14);
            var expectedValue = "08:46:14";

            // act
            var serializedValue = (string)dateTimeType.Serialize(time);

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        public void Serialize_DateTime()
        {
            // arrange
            var dateTimeType = new TimeType();
            var dateTime = new DateTime(
                2018, 6, 11, 8, 46, 14, DateTimeKind.Local);
            var expectedValue = "08:46:14";

            // act
            var serializedValue = (string)dateTimeType.Serialize(dateTime);

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        public void Serialize_DateTimeOffset()
        {
            // arrange
            var dateTimeType = new TimeType();
            var dateTime = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));
            var expectedValue = "08:46:14";

            // act
            var serializedValue = (string)dateTimeType.Serialize(dateTime);

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        public void Serialize_TimeSpan_WithOffset()
        {
            // arrange
            var dateTimeType = new TimeType(true);
            var time = new TimeSpan(8, 46, 14);
            var expectedValue = "08:46:14+00:00";

            // act
            var serializedValue = (string)dateTimeType.Serialize(time);

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        public void Serialize_DateTime_WithOffset()
        {
            // arrange
            var dateTimeType = new TimeType(true);
            var dateTime = new DateTime(
                2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);
            var expectedValue = "08:46:14+00:00";

            // act
            var serializedValue = (string)dateTimeType.Serialize(dateTime);

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        public void Serialize_DateTimeOffset_WithOffset()
        {
            // arrange
            var dateTimeType = new TimeType(true);
            var dateTime = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));
            var expectedValue = "08:46:14+04:00";

            // act
            var serializedValue = (string)dateTimeType.Serialize(dateTime);

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            var dateTimeType = new TimeType();

            // act
            object serializedValue = dateTimeType.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void ParseLiteral_StringValueNode()
        {
            // arrange
            var dateTimeType = new TimeType();
            var literal = new StringValueNode("08:46:14+04:00");
            var currentDateTime = DateTime.Now;
            var expectedTime = new TimeSpan(4, 46, 14);

            // act
            var time = (TimeSpan)dateTimeType
                .ParseLiteral(literal);

            // assert
            Assert.Equal(expectedTime, time);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            var dateTimeType = new TimeType();
            var literal = NullValueNode.Default;

            // act
            object value = dateTimeType.ParseLiteral(literal);

            // assert
            Assert.Null(value);
        }

        [Fact]
        public void ParseValue_TimeSpan()
        {
            // arrange
            var dateTimeType = new TimeType();
            var time = new TimeSpan(8, 46, 14);
            var expectedLiteralValue = "08:46:14";

            // act
            var stringLiteral =
                (StringValueNode)dateTimeType.ParseValue(time);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void ParseValue_DateTimeOffset()
        {
            // arrange
            var dateTimeType = new TimeType();
            var dateTime = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));
            var expectedLiteralValue = "08:46:14";

            // act
            var stringLiteral =
                (StringValueNode)dateTimeType.ParseValue(dateTime);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void ParseValue_DateTime()
        {
            // arrange
            var dateTimeType = new TimeType();
            var dateTime = new DateTime(
                2018, 6, 11, 8, 46, 14,
                DateTimeKind.Utc);
            var expectedLiteralValue = "08:46:14";

            // act
            StringValueNode stringLiteral =
                (StringValueNode)dateTimeType.ParseValue(dateTime);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void ParseValue_TimeSpan_WithOffset()
        {
            // arrange
            var dateTimeType = new TimeType(true);
            var time = new TimeSpan(8, 46, 14);
            var expectedLiteralValue = "08:46:14+00:00";

            // act
            var stringLiteral =
                (StringValueNode)dateTimeType.ParseValue(time);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }


        [Fact]
        public void ParseValue_DateTimeOffset_WithOffset()
        {
            // arrange
            var dateTimeType = new TimeType(true);
            var dateTime = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));
            var expectedLiteralValue = "08:46:14+04:00";

            // act
            var stringLiteral =
                (StringValueNode)dateTimeType.ParseValue(dateTime);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void ParseValue_DateTime_WithOffset()
        {
            // arrange
            var dateTimeType = new TimeType(true);
            var dateTime = new DateTime(
                2018, 6, 11, 8, 46, 14,
                DateTimeKind.Utc);
            var expectedLiteralValue = "08:46:14+00:00";

            // act
            StringValueNode stringLiteral =
                (StringValueNode)dateTimeType.ParseValue(dateTime);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void ParseValue_Null()
        {
            // arrange
            var dateTimeType = new TimeType();

            // act
            IValueNode literal = dateTimeType.ParseValue(null);

            // assert
            Assert.IsType<NullValueNode>(literal);
        }

        [Fact]
        public void EnsureDateTimeTypeKindIsCorret()
        {
            // arrange
            var type = new TimeType();

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }
    }
}
