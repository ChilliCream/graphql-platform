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
        protected void Latitude_ExpectParseLiteralToMatchNegative()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = new StringValueNode("90° 0' 0.000\" S");
            var expectedResult = -90.0;

            // act
            object result = (double)scalar.ParseLiteral(valueSyntax)!;

                // assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        protected void Latitude_ExpectParseLiteralToMatchPositive()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = new StringValueNode("90° 0' 0.000\" N");
            var expectedResult = 90.0;

            // act
            object result = (double)scalar.ParseLiteral(valueSyntax)!;

            // assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        protected void Latitude_ExpectParseLiteralToMatchPositivePrecision()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = new StringValueNode("39° 51' 21.600\" N");
            var expectedResult = 39.856;

            // act
            object result = (double)scalar.ParseLiteral(valueSyntax)!;

            // assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        protected void Latitude_ExpectParseLiteralToMatchPositivePrecision1()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = new StringValueNode("66° 0' 21.983\" N");
            var expectedResult = 66.00610639;

            // act
            object result = (double)scalar.ParseLiteral(valueSyntax)!;

                // assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        protected void Latitude_ExpectParseLiteralToMatchNegativePrecision1()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = new StringValueNode("23° 6' 23.997\" S");
            var expectedResult = -23.10666583;

            // act
            object result = (double)scalar.ParseLiteral(valueSyntax)!;

            // assert
            Assert.Equal(expectedResult, result);
        }
    }
}
