using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class DateTests
    {
        [Fact]
        public void Serialize_Date()
        {
            // arrange
            DateType dateType = new DateType();
            DateTime dateTime = new DateTime(
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
            DateType dateType = new DateType();
            DateTimeOffset dateTime = new DateTimeOffset(
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
            DateType dateType = new DateType();

            // act
            object serializedValue = dateType.Serialize(null);

            // assert
            Assert.Null(serializedValue);
        }

        [Fact]
        public void Serialize_String_Exception()
        {
            // arrange
            DateType dateType = new DateType();

            // act
            Action a = () => dateType.Serialize("foo");

            // assert
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void Deserialize_IsoString_DateTime()
        {
            // arrange
            DateType dateType = new DateType();
            DateTime date = new DateTime(2018, 6, 11);

            // act
            DateTime result = (DateTime)dateType.Deserialize("2018-06-11");

            // assert
            Assert.Equal(date, result);
        }

        [Fact]
        public void ParseLiteral_StringValueNode()
        {
            // arrange
            DateType dateType = new DateType();
            StringValueNode literal = new StringValueNode("2018-06-11");
            DateTime expectedDateTime = new DateTime(2018, 6, 11);

            // act
            DateTime dateTime = (DateTime)dateType
                .ParseLiteral(literal);

            // assert
            Assert.Equal(expectedDateTime, dateTime);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            DateType dateType = new DateType();
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
            DateType dateType = new DateType();
            DateTimeOffset dateTime = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));
            string expectedLiteralValue = "2018-06-11";

            // act
            StringValueNode stringLiteral =
                (StringValueNode)dateType.ParseValue(dateTime);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void ParseValue_DateTime()
        {
            // arrange
            DateType dateType = new DateType();
            DateTime dateTime =
                new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);
            string expectedLiteralValue = "2018-06-11";

            // act
            StringValueNode stringLiteral =
                (StringValueNode)dateType.ParseValue(dateTime);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void ParseValue_Null()
        {
            // arrange
            DateType dateType = new DateType();
            DateTime dateTime =
                new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);
            string expectedLiteralValue = "2018-06-11";

            // act
            StringValueNode stringLiteral =
                (StringValueNode)dateType.ParseValue(dateTime);

            // assert
            Assert.Equal(expectedLiteralValue, stringLiteral.Value);
        }

        [Fact]
        public void EnsureDateTypeKindIsCorret()
        {
            // arrange
            DateType type = new DateType();

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }
    }
}
