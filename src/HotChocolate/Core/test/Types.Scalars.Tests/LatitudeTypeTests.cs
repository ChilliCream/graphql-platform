using System;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class LatitudeTypeTests : ScalarTypeTestBase
    {
        [Fact]
        protected void Schema_WithScalar_IsMatch()
        {
            // arrange
            ISchema schema = BuildSchema<LatitudeType>();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Latitude_EnsureLatitudeTypeKindIsCorrect()
        {
            // arrange
            // act
            var type = new LatitudeType();

            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }

        [Fact]
        protected void UtcOffset_ExpectIsStringValueToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = new StringValueNode("90° 0' 0.000\" S");

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Fact]
        protected void UtcOffset_ExpectNegativeIsStringValueToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = new StringValueNode("90° 0' 0.000\" N");

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Fact]
        protected void Latitude_ExpectIsDoubleMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = 89.9;

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Fact]
        protected void Latitude_ExpectParseLiteralToMatchNegativePrecision()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = new StringValueNode("90° 0' 0.000\" S");
            var expectedResult = -90.0;

            // act
            object result = Math.Round(
                (double) scalar.ParseLiteral(valueSyntax)!,
                0,
                MidpointRounding.AwayFromZero);

            // assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        protected void Latitude_ExpectParseLiteralToMatchPositivePrecision()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = new StringValueNode("90° 0' 0.000\" N");
            var expectedResult = 90.0;

            // act
            object result = Math.Round(
                (double) scalar.ParseLiteral(valueSyntax)!,
                0,
                MidpointRounding.AwayFromZero);

            // assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        protected void Latitude_ExpectParseLiteralToMatchNegativePrecision_OneDecimal()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = new StringValueNode("38° 36' 0.000\" S");
            var expectedResult = -38.6;

            // act
            object result = Math.Round(
                (double) scalar.ParseLiteral(valueSyntax)!,
                1,
                MidpointRounding.AwayFromZero);

            // assert
            Assert.Equal(expectedResult, result);
        }
    }
}
