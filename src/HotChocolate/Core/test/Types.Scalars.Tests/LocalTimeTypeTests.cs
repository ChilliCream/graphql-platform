using System;
using System.Globalization;
using System.Threading;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Scalars
{
    public class LocalTimeTypeTests : ScalarTypeTestBase
    {
        [Fact]
        public void Schema_WithScalar_IsMatch()
        {
            // arrange
            ISchema schema = BuildSchema<LocalTimeType>();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_Utc_DateTimeOffset()
        {
            // arrange
            var dateTimeType = new LocalTimeType();
            DateTimeOffset dateTime = new DateTime(
                2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);

            string expectedValue = "08:46:14";

            // act
            string serializedValue = (string)dateTimeType.Serialize(dateTime)!;

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        public void Serialize_DateTimeOffset()
        {
            // arrange
            var dateTimeType = new LocalTimeType();
            var dateTime = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));
            string expectedValue = "08:46:14";

            // act
            string serializedValue = (string)dateTimeType.Serialize(dateTime)!;

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        public void Serialize_String_Exception()
        {
            // arrange
            var dateTimeType = new LocalTimeType();

            // act
            Action err = () => dateTimeType.Serialize("foo");

            // assert
            Assert.Throws<SerializationException>(err);
        }

        [Fact]
        public void ParseLiteral_StringValueNode()
        {
            // arrange
            var dateTimeType = new LocalTimeType();
            var literal = new StringValueNode(
                "2018-06-29T08:46:14+04:00");
            var expectedDateTime = new DateTimeOffset(
                new DateTime(2018, 6, 29, 8, 46, 14),
                new TimeSpan(4, 0, 0));

            // act
            var dateTime = (DateTime)dateTimeType
                .ParseLiteral(literal)!;

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

            var dateTimeType = new LocalTimeType();
            var literal = new StringValueNode(
                "2018-06-29T08:46:14+04:00");
            var expectedDateTime = new DateTimeOffset(
                new DateTime(2018, 6, 29, 8, 46, 14),
                new TimeSpan(4, 0, 0));

            // act
            var dateTime = (DateTime)dateTimeType
                .ParseLiteral(literal)!;

            // assert
            Assert.Equal(expectedDateTime, dateTime);
        }

        [Fact]
        public void Deserialize_IsoString_DateTimeOffset()
        {
            // arrange
            var dateTimeType = new LocalTimeType();
            var dateTime = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));

            // act
            var deserializedValue = (DateTime)dateTimeType
                .Deserialize("2018-06-11T08:46:14+04:00")!;

            // assert
            Assert.Equal(dateTime, deserializedValue);
        }

        [Fact]
        public void Deserialize_DateTimeOffset_To_DateTimeOffset()
        {
            // arrange
            var type = new LocalTimeType();
            var time = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc));

            // act
            bool success = type.TryDeserialize(time, out object deserialized);

            // assert
            Assert.True(success);
            Assert.Equal(time, deserialized);
        }

        [Fact]
        public void Deserialize_DateTime_To_DateTimeOffset()
        {
            // arrange
            var type = new LocalTimeType();
            var time = new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);

            // act
            bool success = type.TryDeserialize(time, out object deserialized);

            // assert
            Assert.True(success);
            Assert.Equal(time,
                Assert.IsType<DateTimeOffset>(deserialized).UtcDateTime);
        }

        [Fact]
        public void Deserialize_NullableDateTime_To_DateTimeOffset()
        {
            // arrange
            var type = new LocalTimeType();
            DateTime? time =
                new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);

            // act
            bool success = type.TryDeserialize(time, out object deserialized);

            // assert
            Assert.True(success);
            Assert.Equal(time,
                Assert.IsType<DateTimeOffset>(deserialized).UtcDateTime);
        }

        [Fact]
        public void Deserialize_NullableDateTime_To_DateTimeOffset_2()
        {
            // arrange
            var type = new LocalTimeType();
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
            var type = new LocalTimeType();

            // act
            bool success = type.TryDeserialize(null, out object deserialized);

            // assert
            Assert.True(success);
            Assert.Null(deserialized);
        }

        [Fact]
        public void ParseLiteral_NullValueNode()
        {
            // arrange
            var dateTimeType = new LocalTimeType();
            NullValueNode literal = NullValueNode.Default;

            // act
            object value = dateTimeType.ParseLiteral(literal);

            // assert
            Assert.Null(value);
        }

        [Fact]
        public void ParseValue_Null()
        {
            // arrange
            var dateTimeType = new LocalTimeType();

            // act
            IValueNode literal = dateTimeType.ParseValue(null);

            // assert
            Assert.IsType<NullValueNode>(literal);
        }
    }
}
