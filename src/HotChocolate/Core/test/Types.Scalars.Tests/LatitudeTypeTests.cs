using System.Text.Json;
using CookieCrumble.Xunit.Attributes;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Moq;

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
        LatitudeType type = new();

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
        var result = scalar.IsValueCompatible(valueSyntax);

        // assert
        Assert.True(result);
    }

    [Fact]
    protected void Latitude_ExpectValueToLiteralToThrowOnInvalidString()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const string valueSyntax = "92° 0' 0.000\" S";

        // act
        var result = Record.Exception(() => scalar.ValueToLiteral(valueSyntax));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void Latitude_ExpectValueToLiteralToMatchInt()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const double valueSyntax = 89;

        // act
        var result = scalar.ValueToLiteral(valueSyntax);

        // assert
        Assert.Equal(typeof(StringValueNode), result.GetType());
    }

    [Fact]
    protected void Latitude_ExpectValueToLiteralToThrowOnInvalidInt()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const double valueSyntax = 92;

        // act
        var result = Record.Exception(() => scalar.ValueToLiteral(valueSyntax));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void Latitude_ExpectValueToLiteralToMatchDouble()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const double valueSyntax = 89d;

        // act
        var result = scalar.ValueToLiteral(valueSyntax);

        // assert
        Assert.Equal(typeof(StringValueNode), result.GetType());
    }

    [Fact]
    protected void Latitude_ExpectValueToLiteralToThrowOnInvalidDouble()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const double valueSyntax = 92d;

        // act
        var result = Record.Exception(() => scalar.ValueToLiteral(valueSyntax));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void Latitude_ExpectValueToLiteralToThrowOnInvalidType()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const char valueSyntax = 'c';

        // act
        var result = Record.Exception(() => scalar.ValueToLiteral(valueSyntax));

        // assert
        Assert.IsType<LeafCoercionException>(result);
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
        var result = ToPrecision(scalar, valueSyntax, precision);

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
        var result = Record.Exception(() => scalar.CoerceInputLiteral(valueSyntax));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void Latitude_ExpectValueToLiteralToMatchType()
    {
        // arrange
        var scalar = CreateType<LatitudeType>();
        const double valueSyntax = 74.3;

        // act
        var result = scalar.ValueToLiteral(valueSyntax);

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
    protected void Latitude_ExpectValueToLiteralToMatch(double runtime, string literal)
    {
        // arrange
        var scalar = CreateType<LatitudeType>();
        StringValueNode expected = new(literal);

        // act
        var result = scalar.ValueToLiteral(runtime);

        // assert
        Assert.Equal(expected, result, SyntaxComparer.BySyntax);
    }

    [Fact]
    protected void Latitude_ExpectValueToLiteralToThrowSerializationException_GreaterThanMax()
    {
        // arrange
        var scalar = CreateType<LatitudeType>();
        const double runtimeValue = 91d;

        // act
        var result = Record.Exception(() => scalar.ValueToLiteral(runtimeValue));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void Latitude_ExpectValueToLiteralToThrowSerializationException_LessThanMin()
    {
        // arrange
        var scalar = CreateType<LatitudeType>();
        const double runtimeValue = -91d;

        // act
        var result = Record.Exception(() => scalar.ValueToLiteral(runtimeValue));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void Latitude_ExpectCoerceInputValueToMatch()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const double expectedValue = -89d;
        var inputValue = JsonDocument.Parse("\"89° 0' 0.000\\\" S\"").RootElement;

        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var result = scalar.CoerceInputValue(inputValue, context.Object);

        // assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    protected void Latitude_ExpectCoerceInputValueToThrowSerializationException_LessThanMin()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        var inputValue = JsonDocument.Parse("\"91° 0' 0.000\\\" S\"").RootElement;

        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var result = Record.Exception(() => scalar.CoerceInputValue(inputValue, context.Object));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void Latitude_ExpectCoerceInputValueToThrowSerializationException_GreaterThanMax()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        var inputValue = JsonDocument.Parse("\"92° 0' 0.000\\\" N\"").RootElement;

        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var result = Record.Exception(() => scalar.CoerceInputValue(inputValue, context.Object));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    public void Latitude_ExpectCoerceOutputValueInt()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const double runtimeValue = 89;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultElement = resultDocument.Data.GetProperty("first");
        scalar.CoerceOutputValue(runtimeValue, resultElement);

        // assert
        resultElement.MatchSnapshot();
    }

    [Fact]
    protected void Latitude_ExpectCoerceOutputValueIntToThrowSerializationException_LessThanMin()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const double runtimeValue = -91;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultElement = resultDocument.Data.GetProperty("first");
        var result = Record.Exception(() => scalar.CoerceOutputValue(runtimeValue, resultElement));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void Latitude_ExpectCoerceOutputValueIntToThrowSerializationException_GreaterThanMax()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const double runtimeValue = 91;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultElement = resultDocument.Data.GetProperty("first");
        var result = Record.Exception(() => scalar.CoerceOutputValue(runtimeValue, resultElement));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    public void Latitude_ExpectCoerceOutputValueDouble()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const double runtimeValue = 89d;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultElement = resultDocument.Data.GetProperty("first");
        scalar.CoerceOutputValue(runtimeValue, resultElement);

        // assert
        resultElement.MatchSnapshot();
    }

    [Fact]
    protected void Latitude_ExpectCoerceOutputValueDoubleToThrowSerializationException_LessThanMin()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const double runtimeValue = -91d;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultElement = resultDocument.Data.GetProperty("first");
        var result = Record.Exception(() => scalar.CoerceOutputValue(runtimeValue, resultElement));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void Latitude_ExpectCoerceOutputValueDoubleToThrowSerializationException_GreaterThanMax()
    {
        // arrange
        ScalarType scalar = new LatitudeType();
        const double runtimeValue = 91d;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultElement = resultDocument.Data.GetProperty("first");
        var result = Record.Exception(() => scalar.CoerceOutputValue(runtimeValue, resultElement));

        // assert
        Assert.IsType<LeafCoercionException>(result);
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
            (double)scalar.CoerceInputLiteral(valueSyntax)!,
            precision,
            MidpointRounding.AwayFromZero);
    }
}
