using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class LongitudeTypeTests : ScalarTypeTestBase
    {
        [Fact]
        protected void Schema_WithScalar_IsMatch()
        {
            // arrange
            ISchema schema = BuildSchema<LongitudeType>();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Longitude_EnsureLongitudeTypeKindIsCorrect()
        {
            // arrange
            // act
            var type = new LongitudeType();

            // assert
            Assert.Equal(TypeKind.Scalar, type.Kind);
        }

        [Fact]
        protected void Longitude_ExpectIsStringValueToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LongitudeType>();
            var valueSyntax = new StringValueNode("180° 0' 0.000\" W");

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Fact]
        protected void Longitude_ExpectIsDoubleMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LongitudeType>();
            var valueSyntax = 180.00000001;

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("180° 0' 0.000\" E", 180.0, 1)]
        [InlineData("176° 19' 26.576\" E", 176.3, 1)]
        [InlineData("62° 12' 48.831\" W", -62.2, 1)]
        [InlineData("4° 46' 6.456\" W", -4.77, 2)]
        [InlineData("6° 28' 33.481\" W", -6.48, 2)]
        [InlineData("0° 10' 6.902\" W", -0.169, 3)]
        [InlineData("118° 45' 3.780\" E", 118.751, 3)]
        [InlineData("139° 19' 8.803\" E", 139.3191, 4)]
        [InlineData("141° 59' 27.377\" E", 141.9909, 4)]
        [InlineData("12° 30' 40.79\" E", 12.51133, 5)]
        [InlineData("74° 0' 21.49\" W", -74.00597, 5)]
        [InlineData("99° 44' 56.030\" W", -99.748897, 6)]
        [InlineData("21° 55' 56.083\" E", 21.932245, 6)]
        [InlineData("129° 39' 38.704\" E", 129.6607511, 7)]
        [InlineData("54° 33' 12.699\" W", -54.5535275, 7)]
        [InlineData("148° 34' 9.124\" W", -148.56920111, 8)]
        [InlineData("44° 44' 2.119\" W", -44.73392194, 8)]
        protected void Longitude_ExpectParseLiteralToMatch(string literal, double runtime, int precision )
        {
            // arrange
            ScalarType scalar = CreateType<LongitudeType>();
            var valueSyntax = new StringValueNode(literal);
            var expectedResult = runtime;

            // act
            object result = ToPrecision(scalar, valueSyntax, precision);

                // assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        protected void Longitude_ExpectParseLiteralToThrowSerializationException()
        {
            // arrange
            ScalarType scalar = CreateType<LongitudeType>();
            var valueSyntax = new StringValueNode("foo");

            // act
            Exception? result = Record.Exception(() => scalar.ParseLiteral(valueSyntax));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void Longitude_ExpectParseValueToMatchType()
        {
            // arrange
            ScalarType scalar = CreateType<LongitudeType>();
            var valueSyntax = 180.1;

            // act
            IValueNode result = scalar.ParseValue(valueSyntax);

            // assert
            Assert.Equal(typeof(StringValueNode), result.GetType());
        }

        [Fact]
        protected void Longitude_ExpectParseValueToMatchDouble()
        {
            // arrange
            ScalarType scalar = CreateType<LongitudeType>();
            var valueSyntax = 180.1;

            // act
            IValueNode result = scalar.ParseValue(valueSyntax);

            // assert
            Assert.Equal(valueSyntax, Convert.ToDouble(result.Value));
        }

        [Fact]
        protected void Longitude_ExpectParseValueToThrowSerializationException()
        {
            // arrange
            ScalarType scalar = CreateType<LongitudeType>();
            var runtimeValue = new StringValueNode("foo");

            // act
            Exception? result = Record.Exception(() => scalar.ParseValue(runtimeValue));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void Longitude_ExpectDeserializeNullToMatch()
        {
            // arrange
            ScalarType scalar = new LongitudeType();

            // act
            var success = scalar.TryDeserialize(null, out var deserialized);

            // assert
            Assert.True(success);
            Assert.Null(deserialized);
        }

        [Fact]
        public void Longitude_ExpectDeserializeInvalidStringToDouble()
        {
            // arrange
            ScalarType scalar = new LongitudeType();

            // act
            var success = scalar.TryDeserialize("abc", out _);

            // assert
            Assert.False(success);
        }

        [Fact]
        public void Longitude_ExpectDeserializeNullToNull()
        {
            // arrange
            ScalarType scalar = new LongitudeType();

            // act
            var success = scalar.TryDeserialize(null, out var deserialized);

            // assert
            Assert.True(success);
            Assert.Null(deserialized);
        }

        [Fact]
        protected void Longitude_ExpectParseResultToMatchNull()
        {
            // arrange
            ScalarType scalar = new LongitudeType();

            // act
            IValueNode result = scalar.ParseResult(null);

            // assert
            Assert.Equal(typeof(NullValueNode), result.GetType());
        }

        [Fact]
        public async Task Longitude_Integration()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<DefaultLongitudeType>()
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult res = await executor.ExecuteAsync("{ test }");

            // assert
            res.ToJson().MatchSnapshot();
        }

        public class DefaultLongitude
        {
            public double Test => new();
        }

        public class DefaultLongitudeType : ObjectType<DefaultLongitude>
        {
            protected override void Configure(IObjectTypeDescriptor<DefaultLongitude> descriptor)
            {
                descriptor.Field(x => x.Test).Type<LongitudeType>();
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
