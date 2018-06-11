using System;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Execution.ValueConverters
{
    public class FloatValueConverterTests
    {
        [Fact]
        public void CanConvert_FloatType_True()
        {
            // arrange
            FloatValueConverter converter = new FloatValueConverter();
            FloatType type = new FloatType();

            // act
            bool result = converter.CanConvert(type);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void CanConvert_StringType_True()
        {
            // arrange
            FloatValueConverter converter = new FloatValueConverter();
            StringType type = new StringType();

            // act
            bool result = converter.CanConvert(type);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void Convert_Double_Float()
        {
            // arrange
            FloatValueConverter converter = new FloatValueConverter();
            FloatType type = new FloatType();
            double input = 1.0f;
            float expectedOutput = 1.0f;

            // act
            bool result = converter.TryConvert(
                typeof(double), typeof(float),
                input, out object convertedValue);

            // assert
            Assert.True(result);
            Assert.IsType<float>(convertedValue);
            Assert.Equal(expectedOutput, convertedValue);
        }

        [Fact]
        public void Convert_Null_Float()
        {
            // arrange
            FloatValueConverter converter = new FloatValueConverter();
            FloatType type = new FloatType();

            float expectedOutput = default(float);

            // act
            bool result = converter.TryConvert(
                typeof(double), typeof(float),
                null, out object convertedValue);

            // assert
            Assert.True(result);
            Assert.IsType<float>(convertedValue);
            Assert.Equal(expectedOutput, convertedValue);
        }

        [Fact]
        public void Convert_Null_NullableFloat()
        {
            // arrange
            FloatValueConverter converter = new FloatValueConverter();
            FloatType type = new FloatType();

            float? expectedOutput = default(float?);

            // act
            bool result = converter.TryConvert(
                typeof(double), typeof(float?),
                null, out object convertedValue);

            // assert
            Assert.True(result);
            Assert.Equal(expectedOutput, convertedValue);
        }
    }
}
