using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class DateTimeTypeTests
    {
        [Fact]
        public void Serialize_DateTime()
        {
            // arrange
            DateTimeType dateTimeType = new DateTimeType();
            DateTime dateTime = new DateTime(
                2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);
            string expectedValue = "2018-06-11T08:46:14+00:00";

            // act
            string serializedValue = (string)dateTimeType.Serialize(dateTime);

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        public void Serialize_DateTimeOffset()
        {
            // arrange
            DateTimeType dateTimeType = new DateTimeType();
            DateTimeOffset dateTime = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));
            string expectedValue = "2018-06-11T08:46:14+04:00";

            // act
            string serializedValue = (string)dateTimeType.Serialize(dateTime);

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            DateTimeType dateTimeType = new DateTimeType();

            // act
            object serializedValue = dateTimeType.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void ParseLiteral_StringValueNode()
        {
            // arrange
            DateTimeType dateTimeType = new DateTimeType();
            StringValueNode literal = new StringValueNode("2018-06-11T08:46:14+04:00");
            DateTimeOffset expectedDateTime = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));

            // act
            DateTimeOffset dateTime = (DateTimeOffset)dateTimeType
                .ParseLiteral(literal);

            // assert
            Assert.Equal(expectedDateTime, dateTime);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            DateTimeType dateTimeType = new DateTimeType();
            NullValueNode literal = NullValueNode.Default;

            // act
            object value = dateTimeType.ParseLiteral(literal);

            // assert
            Assert.Null(value);
        }

        [Fact]
        public void ParseValue_DateTimeOffset()
        {
            // arrange
            DateTimeType dateTimeType = new DateTimeType();
            DateTimeOffset dateTime = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));
            string expectedLiteralValue = "2018-06-11T08:46:14+04:00";

            // act
            StringValueNode stringLiteral =
                (StringValueNode)dateTimeType.ParseValue(dateTime);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void ParseValue_DateTime()
        {
            // arrange
            DateTimeType dateTimeType = new DateTimeType();
            DateTime dateTime =
                new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);
            string expectedLiteralValue = "2018-06-11T08:46:14+00:00";

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
            DateTimeType dateTimeType = new DateTimeType();

            // act
            IValueNode literal = dateTimeType.ParseValue(null);

            // assert
            Assert.IsType<NullValueNode>(literal);
        }

        [Fact]
        public void EnsureDateTimeTypeKindIsCorret()
        {
            // arrange
            DateTimeType type = new DateTimeType();

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }
    }
}
