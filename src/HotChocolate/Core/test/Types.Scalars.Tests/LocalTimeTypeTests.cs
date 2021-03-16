using System;
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
            string serializedValue = (string)dateTimeType.Serialize(dateTime);

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
            string serializedValue = (string)dateTimeType.Serialize(dateTime);

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
                "06/28/2018 23:46:14");
            var expectedDateTime = new DateTimeOffset(
                new DateTime(2018, 6, 29, 8, 46, 14),
                new TimeSpan(4, 0, 0));

            // act
            var dateTime = dateTimeType
                .ParseLiteral(literal);

            // assert
            Assert.Equal(expectedDateTime, dateTime);
        }
    }
}
