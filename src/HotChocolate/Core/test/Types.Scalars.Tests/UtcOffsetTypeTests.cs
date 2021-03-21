using System;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Scalars
{
    public class UtcOffsetTypeTests : ScalarTypeTestBase
    {
        [Fact]
        protected void Schema_WithScalar_IsMatch()
        {
            // arrange
            ISchema schema = BuildSchema<UtcOffsetType>();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void UtcOffset_EnsureUtcOffsetTypeKindIsCorrect()
        {
            // arrange
            var type = new UtcOffsetType();

            // act
            TypeKind kind = type.Kind;

            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }

        [Fact]
        protected void UtcOffset_ExpectIsStringValueToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<UtcOffsetType>();
            var valueSyntax = new StringValueNode("12:00");

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Fact]
        protected void UtcOffset_ExpectNegativeIsStringValueToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<UtcOffsetType>();
            var valueSyntax = new StringValueNode("-13:53");

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Fact]
        protected void UtcOffset_ExpectPositiveIsStringValueToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<UtcOffsetType>();
            var valueSyntax = new StringValueNode("+23:12");

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Fact]
        protected void UtcOffset_ExpectIsUtcOffsetToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<UtcOffsetType>();
            var valueSyntax = TimeSpan.FromHours(12);

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Fact]
        protected void LocalTime_ExpectParseLiteralToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<UtcOffsetType>();
            var valueSyntax = new StringValueNode("03:15:00");
            var expectedResult = new TimeSpan(3, 15, 0);

            // act
            object result = (TimeSpan)scalar.ParseLiteral(valueSyntax)!;

            // assert
            Assert.Equal(expectedResult, result);
        }
    }
}
