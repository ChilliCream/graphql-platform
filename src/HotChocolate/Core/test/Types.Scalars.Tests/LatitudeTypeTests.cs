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
        protected void Latitude_ExpectIsStringInstanceToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = new StringValueNode("89° 0' 0.000\" S");

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Fact]
        protected void Latitude_ExpectIsStringInstanceToThrowOnInvalidString_GreaterThanMax()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            Exception? result = Record.Exception(() => scalar.ParseResult("92° 0' 0.000\" N"));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void Latitude_ExpectIsStringInstanceToThrowOnInvalidString_LessThanMin()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            Exception? result = Record.Exception(() => scalar.ParseResult("92° 0' 0.000\" S"));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void Latitude_ExpectIsDoubleInstanceToMatch()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = 89.0;

            // act
            var result = scalar.IsInstanceOfType(valueSyntax);

            // assert
            Assert.True(result);
        }

        [Fact]
        protected void Latitude_ExpectIsDoubleInstanceToThrowOnInvalidString_LessThanMin()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();

            // act
            Exception? result = Record.Exception(() => scalar.ParseResult(-91d));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void Latitude_ExpectIsDoubleInstanceToThrowOnInvalidString_GreaterThanMax()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();

            // act
            Exception? result = Record.Exception(() => scalar.ParseResult(91d));

            // assert
            Assert.IsType<SerializationException>(result);
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
        protected void Latitude_ExpectParseResultToThrowOnInvalidString()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            Exception? result = Record.Exception(() => scalar.ParseResult("92° 0' 0.000\" S"));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void Latitude_ExpectParseResultToMatchInt()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            IValueNode result = scalar.ParseResult(89);

            // assert
            Assert.Equal(typeof(StringValueNode), result.GetType());
        }

        [Fact]
        protected void Latitude_ExpectParseResultToThrowOnInvalidInt()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            Exception? result = Record.Exception(() => scalar.ParseResult(92));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void Latitude_ExpectParseResultToMatchDouble()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            IValueNode result = scalar.ParseResult(89.0);

            // assert
            Assert.Equal(typeof(StringValueNode), result.GetType());
        }

        [Fact]
        protected void Latitude_ExpectParseResultToThrowOnInvalidDouble()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            Exception? result = Record.Exception(() => scalar.ParseResult(92.0));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void Latitude_ExpectParseResultToThrowOnInvalidType()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            Exception? result = Record.Exception(() => scalar.ParseResult('c'));

            // assert
            Assert.IsType<SerializationException>(result);
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
        public void Latitude_ParseLiteral_NullValueNode()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            NullValueNode literal = NullValueNode.Default;

            // act
            object value = scalar.ParseLiteral(literal)!;

            // assert
            Assert.Null(value);
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

        [Theory]
        [InlineData(-38.6, "38° 36' 0\" S")]
        [InlineData(-66.9, "66° 54' 0\" S")]
        [InlineData(52.33, "52° 19' 48\" N")]
        [InlineData(51.508, "51° 30' 28.8\" N")]
        [InlineData(64.755, "64° 45' 18\" N")]
        [InlineData(36.2826, "36° 16' 57.36\" N")]
        [InlineData(-6.1806, "6° 10' 50.16\" S")]
        [InlineData(41.89193, "41° 53' 30.948\" N")]
        [InlineData(40.71427, "40° 42' 51.372\" N")]
        [InlineData(42.833013, "42° 49' 58.8468\" N")]
        [InlineData(6.693709, "6° 41' 37.3524\" N")]
        [InlineData(-23.1066658, "23° 6' 23.99688\" S")]
        [InlineData(-23.3220703, "23° 19' 19.45308\" S")]
        [InlineData(66.00610639, "66° 0' 21.983004\" N")]
        [InlineData(76.82079028, "76° 49' 14.845008\" N")]
        protected void Latitude_ExpectParseValueToMatch(double runtime, string literal)
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = runtime;
            var expected = new StringValueNode(literal);

            // act
            IValueNode result = scalar.ParseValue(valueSyntax);

            // assert
            Assert.Equal(expected, result);
        }

        [Fact]
        protected void Latitude_ExpectParseValueToThrowSerializationException_GreaterThanMax()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var runtimeValue = 91.0;

            // act
            Exception? result = Record.Exception(() => scalar.ParseValue(runtimeValue));

            // assert
            Assert.IsType<SerializationException>(result);
        }

        [Fact]
        protected void Latitude_ExpectParseValueToThrowSerializationException_LessThanMin()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var runtimeValue = -91.0;

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
        protected void Latitude_ExpectDeserializeStringToMatch()
        {
            // arrange
            ScalarType scalar = new LatitudeType();
            var expectedValue = -89.0;

            // act
            var success = scalar.TryDeserialize("89° 0' 0.000\" S",
                out var deserialized);

            // assert
            Assert.True(success);
            Assert.Equal(expectedValue, deserialized);
        }

        [Fact]
        protected void Latitude_ExpectDeserializeStringToThrowSerializationException_LessThanMin()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            var success = scalar.TryDeserialize("91° 0' 0.000\" S", out var _);

            // assert
            Assert.False(success);
        }

        [Fact]
        protected void Latitude_ExpectDeserializeStringToThrowSerializationException_GreaterThanMax()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            var success = scalar.TryDeserialize("92° 0' 0.000\" N", out var _);

            // assert
            Assert.False(success);
        }

        [Fact]
        protected void Latitude_ExpectDeserializeDoubleToMatch()
        {
            // arrange
            ScalarType scalar = new LatitudeType();
            var expectedValue = -89.0;

            // act
            var success = scalar.TryDeserialize(-89d, out var deserialized);

            // assert
            Assert.True(success);
            Assert.Equal(expectedValue, deserialized);
        }

        [Fact]
        protected void Latitude_ExpectDeserializeDoubleToThrowSerializationException_LessThanMin()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            var success = scalar.TryDeserialize(-91d, out var _);

            // assert
            Assert.False(success);
        }

        [Fact]
        protected void Latitude_ExpectDeserializeDoubleToThrowSerializationException_GreaterThanMax()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            var success = scalar.TryDeserialize(91d, out var _);

            // assert
            Assert.False(success);
        }

        [Fact]
        protected void Latitude_ExpectDeserializeIntToMatch()
        {
            // arrange
            ScalarType scalar = new LatitudeType();
            var expectedValue = -89;

            // act
            var success = scalar.TryDeserialize(-89, out var deserialized);

            // assert
            Assert.True(success);
            Assert.Equal(expectedValue, deserialized);
        }

        [Fact]
        protected void Latitude_ExpectDeserializeIntToThrowSerializationException_LessThanMin()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            var success = scalar.TryDeserialize(-91, out var _);

            // assert
            Assert.False(success);
        }

        [Fact]
        protected void Latitude_ExpectDeserializeIntToThrowSerializationException_GreaterThanMax()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            var success = scalar.TryDeserialize(91, out var _);

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
        protected void Latitude_ExpectSerializeNullToMatch()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            var success = scalar.TrySerialize(null, out var deserialized);

            // assert
            Assert.True(success);
            Assert.Null(deserialized);
        }

        [Fact]
        public void Latitude_ExpectSerializeInt()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            var success = scalar.TrySerialize(89, out var s);

            // assert
            Assert.True(success);
            Assert.IsType<string>(s);
        }

        [Fact]
        protected void Latitude_ExpectSerializeIntToThrowSerializationException_LessThanMin()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            var success = scalar.TrySerialize(-91, out var _);

            // assert
            Assert.False(success);
        }

        [Fact]
        protected void Latitude_ExpectSerializeIntToThrowSerializationException_GreaterThanMax()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            var success = scalar.TryDeserialize(91, out var _);

            // assert
            Assert.False(success);
        }

        [Fact]
        public void Latitude_ExpectSerializeDouble()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            var success = scalar.TrySerialize(89.0, out var d);

            // assert
            Assert.True(success);
            Assert.IsType<string>(d);
        }

        [Fact]
        protected void Latitude_ExpectSerializeDoubleToThrowSerializationException_LessThanMin()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            var success = scalar.TrySerialize(-91d, out var _);

            // assert
            Assert.False(success);
        }

        [Fact]
        protected void Latitude_ExpectSerializeDoubleToThrowSerializationException_GreaterThanMax()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            var success = scalar.TryDeserialize(91d, out var _);

            // assert
            Assert.False(success);
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
            public double Test => 0;
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
