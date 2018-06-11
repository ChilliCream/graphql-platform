using System;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Execution.ValueConverters
{
    public class DateTimeValueConverterTests
    {
        [Fact]
        public void CanConvert_DateTimeType_True()
        {
            // arrange
            DateTimeValueConverter converter = new DateTimeValueConverter();
            DateTimeType type = new DateTimeType();

            // act
            bool result = converter.CanConvert(type);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void CanConvert_StringType_True()
        {
            // arrange
            DateTimeValueConverter converter = new DateTimeValueConverter();
            StringType type = new StringType();

            // act
            bool result = converter.CanConvert(type);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void Convert_DateTimeOffset_LocalDateTime()
        {
            // arrange
            DateTimeValueConverter converter = new DateTimeValueConverter();
            DateTimeType type = new DateTimeType();
            DateTimeOffset input = new DateTimeOffset(
                new DateTime(2018, 04, 05, 13, 15, 0),
                new TimeSpan(4, 0, 0));

            DateTime expectedUtcOutput = new DateTime(
                2018, 04, 05, 09, 15, 0, DateTimeKind.Utc);

            // act
            bool result = converter.TryConvert(
                typeof(DateTimeOffset), typeof(DateTime),
                input, out object convertedValue);

            // assert
            Assert.True(result);
            Assert.IsType<DateTime>(convertedValue);
            Assert.Equal(expectedUtcOutput, ((DateTime)convertedValue).ToUniversalTime());
        }

        [Fact]
        public void Convert_DateTimeOffset_LocalNullableDateTime()
        {
            // arrange
            DateTimeValueConverter converter = new DateTimeValueConverter();
            DateTimeType type = new DateTimeType();
            DateTimeOffset input = new DateTimeOffset(
                new DateTime(2018, 04, 05, 13, 15, 0),
                new TimeSpan(4, 0, 0));

            DateTime? expectedUtcOutput = new DateTime(
                2018, 04, 05, 09, 15, 0, DateTimeKind.Utc);

            // act
            bool result = converter.TryConvert(
                typeof(DateTimeOffset), typeof(DateTime?),
                input, out object convertedValue);

            // assert
            Assert.True(result);
            Assert.IsType<DateTime>(convertedValue);
            Assert.Equal(expectedUtcOutput, ((DateTime)convertedValue).ToUniversalTime());
        }

        [Fact]
        public void Convert_Null_DateTimeDefault()
        {
            // arrange
            DateTimeValueConverter converter = new DateTimeValueConverter();
            DateTimeType type = new DateTimeType();

            DateTime expectedUtcOutput = default(DateTime);

            // act
            bool result = converter.TryConvert(
                typeof(DateTimeOffset), typeof(DateTime),
                null, out object convertedValue);

            // assert
            Assert.True(result);
            Assert.IsType<DateTime>(convertedValue);
            Assert.Equal(expectedUtcOutput, convertedValue);
        }

        [Fact]
        public void Convert_Null_NullableDateTimeDefault()
        {
            // arrange
            DateTimeValueConverter converter = new DateTimeValueConverter();
            DateTimeType type = new DateTimeType();

            DateTime? expectedUtcOutput = default(DateTime?);

            // act
            bool result = converter.TryConvert(
                typeof(DateTimeOffset), typeof(DateTime?),
                null, out object convertedValue);

            // assert
            Assert.True(result);
            Assert.Equal(expectedUtcOutput, convertedValue);
        }
    }
}
