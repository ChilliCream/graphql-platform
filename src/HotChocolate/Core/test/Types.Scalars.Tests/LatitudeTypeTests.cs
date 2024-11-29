using CookieCrumble.Xunit.Attributes;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class LatitudeTypeTests : ScalarTypeTestBase
{
    [Fact]
    protected void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<LatitudeType>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Latitude_EnsureLatitudeTypeKindIsCorrect()
    {
        // arrange
        // act
        LatitudeType type = new()!;

        // assert
        Assert.Equal(TypeKind.Scalar, type.Kind);
    }

    [Fact]
    protected void Latitude_ExpectIsStringInstanceToMatch()
    {
        // arrange
        var scalar = CreateType<LatitudeType>();
        StringValueNode valueSyntax = new("89° 0' 0.000\" S");

        // act
        var result = scalar.IsInstanceOfType(valueSyntax);

        // assert
        Assert.True(result);
    }

    [Fact]
    protected void Latitude_ExpectIsDoubleInstanceToMatch()
    {
        // arrange
        var scalar = CreateType<LatitudeType>();
        const double valueSyntax = 89d;

        // act
        var result = scalar.IsInstanceOfType(valueSyntax);

        // assert
        Assert.True(result);
    }

    [Fact]
    protected void Latitude_ExpectIsDoubleInstanceToFail_LessThanMin()
    {
        // arrange
        var scalar = CreateType<LatitudeType>();
        const double valueSyntax = -91d;

        // act
        var result = scalar.IsInstanceOfType(valueSyntax);

        // assert
        Assert.False(result);
    }

    [Fact]
    protected void Latitude_ExpectIsDoubleInstanceToFail_GreaterThanMax()
    {
        // arrange
        var scalar = CreateType<LatitudeType>();
        const double valueSyntax = 91d;

        // act
        var result = scalar.IsInstanceOfType(valueSyntax);

        // assert
        Assert.False(result);
    }

    [Fact]
    protected void Latitude_ExpectParseResultToMatchNull()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        object valueSyntax = null!;

        // act
        var result = scalar.ParseResult(valueSyntax);

        // assert
        Assert.Equal(typeof(NullValueNode), result.GetType());
    }

    [Fact]
    protected void Latitude_ExpectParseResultToThrowOnInvalidString()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        var valueSyntax = "92° 0' 0.000\" S";

        // act
        var result = Record.Exception(() => scalar.ParseResult(valueSyntax));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void Latitude_ExpectParseResultToMatchInt()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const int valueSyntax = 89;

        // act
        var result = scalar.ParseResult(valueSyntax);

        // assert
        Assert.Equal(typeof(StringValueNode), result.GetType());
    }

    [Fact]
    protected void Latitude_ExpectParseResultToThrowOnInvalidInt()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const int valueSyntax = 92;

        // act
        var result = Record.Exception(() => scalar.ParseResult(valueSyntax));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void Latitude_ExpectParseResultToMatchDouble()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const double valueSyntax = 89d;

        // act
        var result = scalar.ParseResult(valueSyntax);

        // assert
        Assert.Equal(typeof(StringValueNode), result.GetType());
    }

    [Fact]
    protected void Latitude_ExpectParseResultToThrowOnInvalidDouble()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const double valueSyntax = 92d;

        // act
        var result = Record.Exception(() => scalar.ParseResult(valueSyntax));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void Latitude_ExpectParseResultToThrowOnInvalidType()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const char valueSyntax = 'c';

        // act
        var result = Record.Exception(() => scalar.ParseResult(valueSyntax));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Theory]
    [UseCulture("en-US")]
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
    protected void Latitude_ExpectParseLiteralToMatch(
        string literal,
        double runtime,
        int precision)
    {
        // arrange
        var scalar = CreateType<LatitudeType>();
        StringValueNode valueSyntax = new(literal);

        // act
        object result = ToPrecision(scalar, valueSyntax, precision);

        // assert
        Assert.Equal(runtime, result);
    }

    [Fact]
    protected void Latitude_ExpectParseLiteralToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<LatitudeType>();
        StringValueNode valueSyntax = new("foo");

        // act
        var result = Record.Exception(() => scalar.ParseLiteral(valueSyntax));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    public void Latitude_ParseLiteral_NullValueNode()
    {
        // arrange
        var scalar = CreateType<LatitudeType>();
        var literal = NullValueNode.Default;

        // act
        var value = scalar.ParseLiteral(literal)!;

        // assert
        Assert.Null(value);
    }

    [Fact]
    protected void Latitude_ExpectParseValueToMatchType()
    {
        // arrange
        var scalar = CreateType<LatitudeType>();
        const double valueSyntax = 74.3;

        // act
        var result = scalar.ParseValue(valueSyntax);

        // assert
        Assert.Equal(typeof(StringValueNode), result.GetType());
    }

    [Theory]
    [UseCulture("en-US")]
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
        var scalar = CreateType<LatitudeType>();
        StringValueNode expected = new(literal);

        // act
        var result = scalar.ParseValue(runtime);

        // assert
        Assert.Equal(expected, result, SyntaxComparer.BySyntax);
    }

    [Fact]
    protected void Latitude_ExpectParseValueToThrowSerializationException_GreaterThanMax()
    {
        // arrange
        var scalar = CreateType<LatitudeType>();
        const double runtimeValue = 91d;

        // act
        var result = Record.Exception(() => scalar.ParseValue(runtimeValue));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void Latitude_ExpectParseValueToThrowSerializationException_LessThanMin()
    {
        // arrange
        var scalar = CreateType<LatitudeType>();
        const double runtimeValue = -91d;

        // act
        var result = Record.Exception(() => scalar.ParseValue(runtimeValue));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void Latitude_ExpectDeserializeStringToMatch()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const double expectedValue = -89d;

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
        const string valueSyntax = "91° 0' 0.000\" S"!;

        // act

        var result = Record.Exception(() => scalar.Deserialize(valueSyntax));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void
        Latitude_ExpectDeserializeStringToThrowSerializationException_GreaterThanMax()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const string? valueSyntax = "92° 0' 0.000\" N"!;

        // act
        var result = Record.Exception(() => scalar.Deserialize(valueSyntax));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    public void Latitude_ExpectSerializeInt()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const int valueSyntax = 89;

        // act
        var success = scalar.TrySerialize(valueSyntax, out var s);

        // assert
        Assert.True(success);
        Assert.IsType<string>(s);
    }

    [Fact]
    protected void Latitude_ExpectSerializeIntToThrowSerializationException_LessThanMin()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const int valueSyntax = -91;

        // act
        var result = Record.Exception(() => scalar.Serialize(valueSyntax));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void Latitude_ExpectSerializeIntToThrowSerializationException_GreaterThanMax()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const int valueSyntax = 91;

        // act
        var result = Record.Exception(() => scalar.Serialize(valueSyntax));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    public void Latitude_ExpectSerializeDouble()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const double valueSyntax = 89d;

        // act
        var success = scalar.TrySerialize(valueSyntax, out var d);

        // assert
        Assert.True(success);
        Assert.IsType<string>(d);
    }

    [Fact]
    protected void Latitude_ExpectSerializeDoubleToThrowSerializationException_LessThanMin()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const double valueSyntax = -91d;

        // act
        var result = Record.Exception(() => scalar.Serialize(valueSyntax));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void Latitude_ExpectSerializeDoubleToThrowSerializationException_GreaterThanMax()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const double valueSyntax = 91d;

        // act
        var result = Record.Exception(() => scalar.Serialize(valueSyntax));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    public async Task Latitude_Integration()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<DefaultLatitudeType>()
            .BuildRequestExecutorAsync();

        // act
        var res = await executor.ExecuteAsync("{ test }");

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
        ILeafType scalar,
        IValueNode valueSyntax,
        int precision = 8)
    {
        return Math.Round(
            (double)scalar.ParseLiteral(valueSyntax)!,
            precision,
            MidpointRounding.AwayFromZero);
    }
}
