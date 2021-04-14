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

        [Fact]
        protected void Latitude_ExpectParseLiteralToMatchNegativePrecision2()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = new StringValueNode("6° 10' 50.160\" S");
            var expectedResult = -6.1806;

            // act
            object result = (double)scalar.ParseLiteral(valueSyntax)!;

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
        protected void UtcOffset_ExpectParseValueToMatchDouble()
        {
            // arrange
            ScalarType scalar = CreateType<LatitudeType>();
            var valueSyntax = 90.1;

            // act
            IValueNode result = scalar.ParseValue(valueSyntax);

            // assert
            Assert.Equal(typeof(StringValueNode), result.GetType());
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
        protected void UtcOffset_ExpectDeserializeNullToMatch()
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
        protected void UtcOffset_ExpectParseResultToMatchNull()
        {
            // arrange
            ScalarType scalar = new LatitudeType();

            // act
            IValueNode result = scalar.ParseResult(null);

            // assert
            Assert.Equal(typeof(NullValueNode), result.GetType());
        }

        [Fact]
        public async Task Integration_DefaultUtcOffset()
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
    }
}
