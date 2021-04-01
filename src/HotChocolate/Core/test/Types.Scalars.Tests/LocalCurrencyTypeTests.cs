using System;
using System.Globalization;
using System.Threading;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class LocalCurrencyTypeTests : ScalarTypeTestBase
    {
        [Fact]
        public void LocalCurrency_EnsureLocalCurrencyTypeKindIsCorrect()
        {
            // arrange
            var type = new LocalCurrencyType("US","en-Us");

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, kind);
        }

        [Fact]
        public void LocalCurrency_EnsureLocalCurrencyTypeKindIsCorrect1()
        {
            // arrange
            var type = new LocalCurrencyType();

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, kind);
        }

        [Fact]
        protected void LocalCurrency_ExpectIsStringValueToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LocalCurrencyType>();
            var valueSyntax = new StringValueNode("10.99");

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Fact]
        protected void LocalCurrency_ExpectIsStringValueToMatchEuro()
        {
            // arrange
            ScalarType scalar = new LocalCurrencyType("Germany", "de-De");
            var valueSyntax = new StringValueNode("10,99");

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Fact]
        protected void LocalCurrency_ExpectParseLiteralToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LocalCurrencyType>();
            var valueSyntax = new StringValueNode("24.99");
            var expectedResult = (decimal) 24.99;

            // act
            object result = (decimal)scalar.ParseLiteral(valueSyntax)!;

            // assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        protected void LocalCurrency_ExpectParseLiteralToMatchEuro()
        {
            // arrange
            ScalarType scalar = new LocalCurrencyType("Germany", "de-DE");
            var valueSyntax = new StringValueNode("24,99");
            var expectedResult = (decimal) 24.99;

            // act
            object result = (decimal)scalar.ParseLiteral(valueSyntax)!;

            // assert
            Assert.Equal(expectedResult, result);
        }

        [InlineData("US", "en-US")]
        [InlineData("Australia", "en-AU")]
        [InlineData("UK", "en-GB")]
        [InlineData("Switzerland", "de-CH")]
        [Theory]
        public void LocalTime_ParseLiteralStringValueDifferentCulture(string name, string cultureName)
        {
            // arrange
            ScalarType scalar = new LocalCurrencyType(name, cultureName);
            var valueSyntax = new StringValueNode("9.99");
            var expectedDateTime = (decimal)9.99;

            // act
            var dateTime = (decimal)scalar.ParseLiteral(valueSyntax)!;

            // assert
            Assert.Equal(expectedDateTime, dateTime);
        }
    }
}
