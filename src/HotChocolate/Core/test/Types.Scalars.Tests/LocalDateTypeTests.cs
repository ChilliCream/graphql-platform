using Xunit;
using System;
using System.Globalization;
using System.Threading;
using HotChocolate.Language;
using Snapshooter.Xunit;

namespace HotChocolate.Types.Scalars
{
    public class LocalDateTypeTests : ScalarTypeTestBase
    {
        [Fact]
        protected void Schema_WithScalar_IsMatch()
        {
            // arrange
            ISchema schema = BuildSchema<LocalDateType>();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void LocalDate_EnsureDateTimeTypeKindIsCorret()
        {
            // arrange
            var type = new LocalDateType();

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }

        [Fact]
        protected void LocalDate_ExpectIsStringValueToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LocalDateType>();
            var valueSyntax = new StringValueNode("2018-06-29T08:46:14+04:00");

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Fact]
        protected void LocalDate_ExpectIsDateTimeToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LocalDateType>();
            var valueSyntax = new DateTime(2018, 6, 29, 8, 46, 14);

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Fact]
        protected void LocalDate_ExpectParseLiteralToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LocalDateType>();
            var valueSyntax = new StringValueNode("2018-06-29T08:46:14");
            var expectedResult = new DateTime(2018, 6, 29, 8, 46, 14);

            // act
            object result = (DateTime)scalar.ParseLiteral(valueSyntax)!;

            // assert
            Assert.Equal(expectedResult, result);
        }

        [InlineData("en-US")]
        [InlineData("en-AU")]
        [InlineData("en-GB")]
        [InlineData("de-CH")]
        [InlineData("de-de")]
        [Theory]
        public void LocalDate_ParseLiteralStringValueDifferentCulture(
            string cultureName)
        {
            // arrange
            Thread.CurrentThread.CurrentCulture =
                CultureInfo.GetCultureInfo(cultureName);

            ScalarType scalar = new LocalDateType();
            var valueSyntax = new StringValueNode("2018-06-29T08:46:14+04:00");
            var expectedDateTime = new DateTimeOffset(new DateTime(2018, 6, 29, 8, 46, 14),
                new TimeSpan(4, 0, 0));

            // act
            var dateTime = (DateTime)scalar.ParseLiteral(valueSyntax)!;

            // assert
            Assert.Equal(expectedDateTime, dateTime);
        }

        [Fact]
        protected void LocalDate_ExpectParseLiteralToThrowSerializationException()
        {
            // arrange
            ScalarType scalar = CreateType<LocalDateType>();
            var valueSyntax = new StringValueNode("foo");

            // act
            Exception? result = Record.Exception(() => scalar.ParseLiteral(valueSyntax));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void LocalDate_ExpectParseValueToMatchDateTime()
        {
            // arrange
            ScalarType scalar = CreateType<LocalDateType>();
            var valueSyntax = new DateTime(2018, 6, 29, 8, 46, 14);

            // act
            IValueNode result = scalar.ParseValue(valueSyntax);

            // assert
            Assert.Equal(typeof(StringValueNode), result.GetType());
        }

        [Fact]
        protected void LocalDate_ExpectParseValueToThrowSerializationException()
        {
            // arrange
            ScalarType scalar = CreateType<LocalDateType>();
            var runtimeValue = new StringValueNode("foo");

            // act
            Exception? result = Record.Exception(() => scalar.ParseValue(runtimeValue));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void LocalDate_ExpectSerializeUtcToMatch()
        {
            // arrange
            ScalarType scalar = new LocalDateType();
            DateTimeOffset dateTime = new DateTime(
                2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);

            string expectedValue = "2018-06-11";

            // act
            string serializedValue = (string)scalar.Serialize(dateTime)!;

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        protected void LocalDate_ExpectSerializeDateTimeOffsetToMatch()
        {
            // arrange
            ScalarType scalar = new LocalDateType();
            var dateTime = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));
            string expectedValue = "2018-06-11";

            // act
            string serializedValue = (string)scalar.Serialize(dateTime)!;

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        protected void LocalDate_ExpectDeserializeNullToMatch()
        {
            // arrange
            ScalarType scalar = new LocalDateType();

            // act
            var success = scalar.TryDeserialize(null, out object? deserialized);

            // assert
            Assert.True(success);
            Assert.Null(deserialized);
        }

        [Fact]
        public void LocalDate_ExpectDeserializeNullableDateTimeToDateTime()
        {
            // arrange
            ScalarType scalar = new LocalDateType();
            DateTime? time = null;

            // act
            var success = scalar.TryDeserialize(time, out object? deserialized);

            // assert
            Assert.True(success);
            Assert.Null(deserialized);
        }

        [Fact]
        protected void LocalDate_ExpectDeserializeStringToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LocalDateType>();
            var runtimeValue = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));

            // act
            var deserializedValue = (DateTime)scalar
                .Deserialize("2018-06-11T08:46:14+04:00")!;

            // assert
            Assert.Equal(runtimeValue, deserializedValue);
        }

        [Fact]
        protected void LocalDate_ExpectDeserializeDateTimeOffsetToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LocalTimeType>();
            object input = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));
            object expected = new DateTime(2018, 6, 11, 8, 46, 14);

            // act
            object? result = scalar.Deserialize(input);

            // assert
            Assert.Equal(result, expected);
        }

        [Fact]
        protected void LocalDate_ExpectDeserializeDateTimeToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LocalDateType>();
            object? resultValue =  new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);


            // act
            object? result = scalar.Deserialize(resultValue);

            // assert
            Assert.Equal(resultValue, result);
        }

        [Fact]
        public void LocalDate_ExpectDeserializeInvalidStringToDateTime()
        {
            // arrange
            ScalarType scalar = new LocalDateType();

            // act
            var success = scalar.TryDeserialize("abc", out object? _);

            // assert
            Assert.False(success);
        }

        [Fact]
        public void LocalDate_ExpectDeserializeNullToNull()
        {
            // arrange
            ScalarType scalar = new LocalDateType();

            // act
            var success = scalar.TryDeserialize(null, out object? deserialized);

            // assert
            Assert.True(success);
            Assert.Null(deserialized);
        }

        [Fact]
        protected void LocalDate_ExpectSerializeToThrowSerializationException()
        {
            // arrange
            ScalarType scalar = CreateType<LocalDateType>();

            // act
            Exception? result = Record.Exception(() => scalar.Serialize("foo"));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void LocalDate_ExpectDeserializeToThrowSerializationException()
        {
            // arrange
            ScalarType scalar = CreateType<LocalDateType>();
            object? runtimeValue = new IntValueNode(1);

            // act
            Exception? result = Record.Exception(() => scalar.Deserialize(runtimeValue));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void LocalDate_ExpectParseResultToMatchNull()
        {
            // arrange
            ScalarType scalar = new LocalDateType();

            // act
            IValueNode result = scalar.ParseResult(null);

            // assert
            Assert.Equal(typeof(NullValueNode), result.GetType());
        }

        [Fact]
        protected void LocalDate_ExpectParseResultToMatchStringValue()
        {
            // arrange
            ScalarType scalar = new LocalDateType();
            const string valueSyntax = "2018-06-29T08:46:14+04:00";

            // act
            IValueNode result = scalar.ParseResult(valueSyntax);

            // assert
            Assert.Equal(typeof(StringValueNode), result.GetType());
        }

        [Fact]
        protected void LocalDate_ExpectParseResultToThrowSerializationException()
        {
            // arrange
            ScalarType scalar = new LocalDateType();
            IValueNode runtimeValue = new IntValueNode(1);

            // act
            Exception? result = Record.Exception(() => scalar.ParseResult(runtimeValue));

            // assert
            Assert.IsType<SerializationException>(result);
        }
    }
}
