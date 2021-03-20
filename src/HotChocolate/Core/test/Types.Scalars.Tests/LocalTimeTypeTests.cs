using System;
using System.Collections.Generic;
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
        protected void Schema_WithScalar_IsMatch()
        {
            // arrange
            ISchema schema = BuildSchema<LocalTimeType>();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void LocalTime_EnsureLocalTimeTypeKindIsCorrect()
        {
            // arrange
            var type = new LocalTimeType();

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }

        [Fact]
        protected void LocalTime_ExpectIsStringValueToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LocalTimeType>();
            var valueSyntax = new StringValueNode(
                "2018-06-29T08:46:14+04:00");

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Fact]
        protected void LocalTime_ExpectIsDateTimeToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LocalTimeType>();
            var valueSyntax = new DateTime(2018, 6, 29, 8, 46, 14);

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Fact]
        protected void LocalTime_ExpectParseLiteralToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LocalTimeType>();
            var valueSyntax = new StringValueNode(
                "2018-06-29T08:46:14");
            var expectedResult = new DateTime(
                2018, 6, 29, 8, 46, 14);

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
        public void LocalTime_ParseLiteralStringValueDifferentCulture(
            string cultureName)
        {
            // arrange
            Thread.CurrentThread.CurrentCulture =
                CultureInfo.GetCultureInfo(cultureName);

            ScalarType scalar = new LocalTimeType();
            var valueSyntax = new StringValueNode(
                "2018-06-29T08:46:14+04:00");
            var expectedDateTime = new DateTimeOffset(
                new DateTime(2018, 6, 29, 8, 46, 14),
                new TimeSpan(4, 0, 0));

            // act
            var dateTime = (DateTime)scalar
                .ParseLiteral(valueSyntax)!;

            // assert
            Assert.Equal(expectedDateTime, dateTime);
        }

        [Fact]
        protected void LocalTime_ExpectParseLiteralToThrowSerializationException()
        {
            // arrange
            ScalarType scalar = CreateType<LocalTimeType>();
            var valueSyntax = new StringValueNode("foo");

            // act
            Exception? result = Record.Exception(() => scalar.ParseLiteral(valueSyntax));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void LocalTime_ExpectParseValueToMatchDateTime()
        {
            // arrange
            ScalarType scalar = CreateType<LocalTimeType>();
            var valueSyntax = new DateTime(2018, 6, 29, 8, 46, 14);

            // act
            IValueNode result = scalar.ParseValue(valueSyntax);

            // assert
            Assert.Equal(typeof(StringValueNode), result.GetType());
        }

        [Fact]
        protected void LocalTime_ExpectParseValueToThrowSerializationException()
        {
            // arrange
            ScalarType scalar = CreateType<LocalTimeType>();
            var runtimeValue = new StringValueNode("foo");

            // act
            Exception? result = Record.Exception(() => scalar.ParseValue(runtimeValue));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void LocalTime_ExpectSerializeUtcToMatch()
        {
            // arrange
            ScalarType scalar = new LocalTimeType();
            DateTimeOffset dateTime = new DateTime(
                2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);

            string expectedValue = "08:46:14";

            // act
            string serializedValue = (string)scalar.Serialize(dateTime)!;

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        protected void LocalTime_ExpectSerializeDateTimeOffsetToMatch()
        {
            // arrange
            ScalarType scalar = new LocalTimeType();
            var dateTime = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));
            string expectedValue = "08:46:14";

            // act
            string serializedValue = (string)scalar.Serialize(dateTime)!;

            // assert
            Assert.Equal(expectedValue, serializedValue);
        }

        [Fact]
        protected void LocalTime_ExpectDeserializeNullToMatch()
        {
            // arrange
            ScalarType scalar = new LocalTimeType();

            // act
            var success = scalar.TryDeserialize(null, out object deserialized);

            // assert
            Assert.True(success);
            Assert.Null(deserialized);
        }

        [Fact]
        public void LocalTime_ExpectDeserializeNullableDateTimeToDateTime()
        {
            // arrange
            ScalarType scalar = new LocalTimeType();
            DateTime? time = null;

            // act
            var success = scalar.TryDeserialize(time, out object? deserialized);

            // assert
            Assert.True(success);
            Assert.Null(deserialized);
        }

        [Fact]
        protected void LocalTime_ExpectDeserializeStringToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LocalTimeType>();
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
        protected void LocalTime_ExpectDeserializeDateTimeOffsetToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LocalTimeType>();
            object? resultValue = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));
            object? runtimeValue = new DateTimeOffset(
                new DateTime(2018, 6, 11, 8, 46, 14),
                new TimeSpan(4, 0, 0));

            // act
            object? result = scalar.Deserialize(resultValue);

            // assert
            Assert.Equal(resultValue, runtimeValue);
        }

        [Fact]
        protected void LocalTime_ExpectDeserializeDateTimeToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LocalTimeType>();
            object? resultValue =  new DateTime(
                2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);
            object? runtimeValue = new DateTime(
                2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);


            // act
            object? result = scalar.Deserialize(resultValue);

            // assert
            Assert.Equal(resultValue, runtimeValue);
        }

        [Fact]
        public void LocalTime_ExpectDeserializeInvalidStringToDateTime()
        {
            // arrange
            ScalarType scalar = new LocalTimeType();

            // act
            var success = scalar.TryDeserialize("abc", out object? deserialized);

            // assert
            Assert.False(success);
        }

        [Fact]
        public void LocalTime_ExpectDeserializeNullToNull()
        {
            // arrange
            ScalarType scalar = new LocalTimeType();

            // act
            var success = scalar.TryDeserialize(null, out object? deserialized);

            // assert
            Assert.True(success);
            Assert.Null(deserialized);
        }

        [Fact]
        protected void LocalTime_ExpectSerializeToThrowSerializationException()
        {
            // arrange
            ScalarType scalar = CreateType<LocalTimeType>();

            // act
            Exception? result = Record.Exception(() => scalar.Serialize("foo"));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void LocalTime_ExpectDeserializeToThrowSerializationException()
        {
            // arrange
            ScalarType scalar = CreateType<LocalTimeType>();
            object? runtimeValue = new IntValueNode(1);

            // act
            Exception? result = Record.Exception(() => scalar.Deserialize(runtimeValue));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void LocalTime_ExpectParseResultToMatchNull()
        {
            // arrange
            ScalarType scalar = new LocalTimeType();

            // act
            IValueNode result = scalar.ParseResult(null);

            // assert
            Assert.Equal(typeof(NullValueNode), result.GetType());
        }

        [Fact]
        protected void LocalTime_ExpectParseResultToMatchStringValue()
        {
            // arrange
            ScalarType scalar = new LocalTimeType();
            const string valueSyntax = "2018-06-29T08:46:14+04:00";

            // act
            IValueNode result = scalar.ParseResult(valueSyntax);

            // assert
            Assert.Equal(typeof(StringValueNode), result.GetType());
        }

        [Fact]
        protected void LocalTime_ExpectParseResultToThrowSerializationException()
        {
            // arrange
            ScalarType scalar = new LocalTimeType();
            IValueNode runtimeValue = new IntValueNode(1);

            // act
            Exception? result = Record.Exception(() => scalar.ParseResult(runtimeValue));

            // assert
            Assert.IsType<SerializationException>(result);
        }
    }
}
