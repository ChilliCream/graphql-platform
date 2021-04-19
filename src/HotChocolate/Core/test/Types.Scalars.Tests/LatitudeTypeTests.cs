using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
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
        protected void Latitude_ExpectIsStringValueToMatch()
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
        protected void Latitude_ExpectIsDoubleMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = 90.00000001;

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("38° 36' 0.000\" S", -38.6, 1)]
        [InlineData("66° 54' 0.000\" S", -66.9, 1)]
        [InlineData("39° 51' 21.600\" N", 39.86, 2)]
        [InlineData("52° 19' 48.000\" N", 52.33, 2)]
        [InlineData("51° 30' 28.800\" N", 51.508, 3)]
        [InlineData("64° 45' 18.000\" N", 64.755, 3)]
        [InlineData("36° 16' 57.360\" N", 36.2826, 4)]
        [InlineData("6° 10' 50.160\" S", -6.1806, 4)]
        [InlineData("41° 53' 30.95\" N", 41.89193, 5)]
        [InlineData("40° 42' 51.37\" N", 40.71427, 5)]
        [InlineData("42° 49' 58.845\" N", 42.833013, 6)]
        [InlineData("6° 41' 37.353\" N", 6.693709, 6)]
        [InlineData("23° 6' 23.997\" S", -23.1066658, 7)]
        [InlineData("23° 19' 19.453\" S", -23.3220703, 7)]
        [InlineData("66° 0' 21.983\" N", 66.00610639, 8)]
        [InlineData("76° 49' 14.845\" N", 76.82079028, 8)]
        protected void Latitude_ExpectParseLiteralToMatch(string literal, double runtime, int precision )
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = new StringValueNode(literal);
            var expectedResult = runtime;

            // act
            object result = ToPrecision(scalar, valueSyntax, precision);

                // assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        protected void Latitude_ExpectParseLiteralToThrowSerializationException()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = new StringValueNode("foo");

            // act
            Exception? result = Record.Exception(() => scalar.ParseLiteral(valueSyntax));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void Latitude_ExpectParseValueToMatchType()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = 74.3;

            // act
            IValueNode result = scalar.ParseValue(valueSyntax);

            // assert
            Assert.Equal(typeof(StringValueNode), result.GetType());
        }

        [Fact]
        protected void Latitude_ExpectParseValueToMatchDouble()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = 74.3;

            // act
            IValueNode result = scalar.ParseValue(valueSyntax);

            // assert
            Assert.Equal(valueSyntax, Convert.ToDouble(result.Value));
        }

        [Fact]
        protected void Latitude_ExpectParseValueToThrowSerializationException()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var runtimeValue = new StringValueNode("foo");

            // act
            Exception? result = Record.Exception(() => scalar.ParseValue(runtimeValue));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void Latitude_ExpectDeserializeNullToMatch()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            var success = scalar.TryDeserialize(null, out var deserialized);

            // assert
            Assert.True(success);
            Assert.Null(deserialized);
        }

        [Fact]
        public void Latitude_ExpectDeserializeInvalidStringToDouble()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            var success = scalar.TryDeserialize("abc", out _);

            // assert
            Assert.False(success);
        }

        [Fact]
        public void Latitude_ExpectDeserializeNullToNull()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            var success = scalar.TryDeserialize(null, out var deserialized);

            // assert
            Assert.True(success);
            Assert.Null(deserialized);
        }

        [Fact]
        protected void Latitude_ExpectParseResultToMatchNull()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            IValueNode result = scalar.ParseResult(null);

            // assert
            Assert.Equal(typeof(NullValueNode), result.GetType());
        }

        [Fact]
        protected void Longitude_ExpectParseResultToMatchNull_Decimal()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            IValueNode result = scalar.ParseResult(-44.73392194d);

            // assert

            Assert.Equal("-44.73392194", Assert.IsType<StringValueNode>(result).Value);
        }

        [Fact]
        protected void Longitude_ExpectParseResultToMatchNull_Double()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            IValueNode result = scalar.ParseResult(-44.73392194);

            // assert

            Assert.Equal("-44.73392194", Assert.IsType<StringValueNode>(result).Value);
        }

        [Fact]
        protected void Longitude_ExpectParseResultToMatchNull_Int()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            IValueNode result = scalar.ParseResult(-44);

            // assert

            Assert.Equal("-44", Assert.IsType<StringValueNode>(result).Value);
        }

        [Fact]
        protected void Longitude_ExpectParseResultToMatchNull_ThrowOnInt()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            Exception? ex = Record.Exception(() => scalar.ParseResult('c'));

            // assert
            Assert.IsType<SerializationException>(ex);
        }

        [Fact]
        protected void Longitude_ExpectParseResultToMatchNull_BiggerThanMin()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            Exception? ex = Record.Exception(() => scalar.ParseResult(-90.1));

            // assert
            Assert.IsType<SerializationException>(ex);
        }

        [Fact]
        protected void Longitude_ExpectParseResultToMatchNull_BiggerThanMax()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            Exception? ex = Record.Exception(() => scalar.ParseResult(90.1));

            // assert
            Assert.IsType<SerializationException>(ex);
        }

        [Fact]
        public async Task Latitude_Integration()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<DefaultLatitudeType>()
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult res = await executor.ExecuteAsync("{ test }");

            // assert
            res.ToJson().MatchSnapshot();
        }

        public class DefaultLatitude
        {
            public double Test => new();
        }

        public class DefaultLatitudeType : ObjectType<DefaultLatitude>
        {
            protected override void Configure(IObjectTypeDescriptor<DefaultLatitude> descriptor)
            {
                descriptor.Field(x => x.Test).Type<LatitudeType>();
            }
        }

        private static double ToPrecision(
            IParsableType scalar,
            IValueNode valueSyntax,
            int precision = 8)
        {
            return Math.Round(
                (double)scalar.ParseLiteral(valueSyntax)!,
                precision,
                MidpointRounding.AwayFromZero);
        }
    }
}
