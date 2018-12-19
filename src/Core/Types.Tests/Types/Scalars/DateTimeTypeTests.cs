using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class DateTimeTypeTests
    {
        [Fact]
        public void Serialize_Utc_DateTime()
        {
            // arrange
            DateTimeType dateTimeType = new DateTimeType();
            DateTime dateTime = new DateTime(
                2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);
            string expectedValue = "2018-06-11T08:46:14.000Z";

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
            string expectedValue = "2018-06-11T08:46:14.000+04:00";

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
        public void Serialize_String_Exception()
        {
            // arrange
            DateTimeType dateTimeType = new DateTimeType();

            // act
            Action a = () => dateTimeType.Serialize("foo");

            // assert
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void ParseLiteral_StringValueNode()
        {
            // arrange
            DateTimeType dateTimeType = new DateTimeType();
            StringValueNode literal = new StringValueNode(
                "2018-06-11T08:46:14+04:00");
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
        public void Deserialize_IsoString_DateTimeOffset()
        {
            // arrange
            DateTimeType dateTimeType = new DateTimeType();
            DateTimeOffset dateTime = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));

            // act
            DateTimeOffset deserializedValue = (DateTimeOffset)dateTimeType
                .Deserialize("2018-06-11T08:46:14+04:00");

            // assert
            Assert.Equal(dateTime, deserializedValue);
        }

        [Fact]
        public void Deserialize_ZuluString_DateTimeOffset()
        {
            // arrange
            DateTimeType dateTimeType = new DateTimeType();
            DateTimeOffset dateTime = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(0, 0, 0));

            // act
            DateTimeOffset deserializedValue = (DateTimeOffset)dateTimeType
                .Deserialize("2018-06-11T08:46:14.000Z");

            // assert
            Assert.Equal(dateTime, deserializedValue);
        }

        [Fact]
        public void Deserialize_IsoString_DateTime()
        {
            // arrange
            DateTimeType dateTimeType = new DateTimeType();
            DateTime dateTime = new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Unspecified);

            // act
            DateTime deserializedValue = ((DateTimeOffset)dateTimeType
                .Deserialize("2018-06-11T08:46:14+04:00")).DateTime;

            // assert
            Assert.Equal(dateTime, deserializedValue);
            Assert.Equal(DateTimeKind.Unspecified, deserializedValue.Kind);
        }

        [Fact]
        public void Deserialize_ZuluString_DateTime()
        {
            // arrange
            DateTimeType dateTimeType = new DateTimeType();
            DateTimeOffset dateTime = new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);

            // act
            DateTime deserializedValue = ((DateTimeOffset)dateTimeType
                .Deserialize("2018-06-11T08:46:14.000Z")).DateTime;

            // assert
            Assert.Equal(dateTime, deserializedValue);
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
            string expectedLiteralValue = "2018-06-11T08:46:14.000+04:00";

            // act
            StringValueNode stringLiteral =
                (StringValueNode)dateTimeType.ParseValue(dateTime);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void ParseValue_Unspecified_DateTime()
        {
            // arrange
            DateTimeType dateTimeType = new DateTimeType();
            DateTime dateTime =
                new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Unspecified);
            DateTimeOffset offset = dateTime;
            DateTime offsetDateTime = offset.DateTime;

            // act
            StringValueNode stringLiteral =
                (StringValueNode)dateTimeType.ParseValue(dateTime);
            StringValueNode stringLiteralOffset =
                (StringValueNode)dateTimeType.ParseValue(offsetDateTime);

            // assert
            Assert.Equal(stringLiteralOffset, stringLiteral);
        }

        [Fact]
        public void ParseValue_Local_DateTime()
        {
            // arrange
            DateTimeType dateTimeType = new DateTimeType();
            DateTime dateTime =
                new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Local);
            DateTimeOffset offset = dateTime;
            DateTime offsetDateTime = offset.DateTime;

            // act
            StringValueNode stringLiteral =
                (StringValueNode)dateTimeType.ParseValue(dateTime);
            StringValueNode stringLiteralOffset =
                (StringValueNode)dateTimeType.ParseValue(offsetDateTime);

            // assert
            Assert.Equal(stringLiteral, stringLiteralOffset);
        }

        [Fact]
        public void ParseValue_Utc_DateTime()
        {
            // arrange
            DateTimeType dateTimeType = new DateTimeType();
            DateTime dateTime =
                new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);
            string expectedLiteralValue = "2018-06-11T08:46:14.000Z";

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
