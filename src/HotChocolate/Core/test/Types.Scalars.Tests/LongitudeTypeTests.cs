using System.Text.Json;
using CookieCrumble.Xunit.Attributes;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using static HotChocolate.Language.SyntaxComparer;

namespace HotChocolate.Types;

public class LongitudeTypeTests : ScalarTypeTestBase
{
    [Fact]
    protected void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<LongitudeType>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Longitude_EnsureLongitudeTypeKindIsCorrect()
    {
        // arrange
        // act
        ScalarType type = new LongitudeType();

        // assert
        Assert.Equal(TypeKind.Scalar, type.Kind);
    }

    [Fact]
    protected void Longitude_ExpectIsStringInstanceToMatch()
    {
        // arrange
        var scalar = CreateType<LongitudeType>();
        StringValueNode valueSyntax = new("179° 0' 0.000\" E");

        // act
        var result = scalar.IsValueCompatible(valueSyntax);

        // assert
        Assert.True(result);
    }

    [Fact]
    protected void Longitude_ExpectValueToLiteralToThrowOnInvalidString()
    {
        // arrange
        ScalarType scalar = new LongitudeType();
        const string valueSyntax = "-181° 0' 0.000\" W";

        // act
        var result = Record.Exception(() => scalar.ValueToLiteral(valueSyntax));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void Longitude_ExpectValueToLiteralToMatchInt()
    {
        // arrange
        ScalarType scalar = new LongitudeType();
        const double valueSyntax = 179;

        // act
        var result = scalar.ValueToLiteral(valueSyntax);

        // assert
        Assert.Equal(typeof(StringValueNode), result.GetType());
    }

    [Fact]
    protected void Longitude_ExpectValueToLiteralToThrowOnInvalidInt()
    {
        // arrange
        ScalarType scalar = new LongitudeType();
        const double valueSyntax = 181;

        // act
        var result = Record.Exception(() => scalar.ValueToLiteral(valueSyntax));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void Longitude_ExpectValueToLiteralToMatchDouble()
    {
        // arrange
        ScalarType scalar = new LongitudeType();
        const double valueSyntax = 179d;

        // act
        var result = scalar.ValueToLiteral(valueSyntax);

        // assert
        Assert.Equal(typeof(StringValueNode), result.GetType());
    }

    [Fact]
    protected void Longitude_ExpectValueToLiteralToThrowOnInvalidDouble()
    {
        // arrange
        ScalarType scalar = new LongitudeType();
        const double valueSyntax = -182d;

        // act
        var result = Record.Exception(() => scalar.ValueToLiteral(valueSyntax));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void Longitude_ExpectValueToLiteralToThrowOnInvalidType()
    {
        // arrange
        ScalarType scalar = new LongitudeType();
        const char valueSyntax = 'c';

        // act
        var result = Record.Exception(() => scalar.ValueToLiteral(valueSyntax));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Theory]
    [UseCulture("en-US")]
    [InlineData("176° 19' 26.576\" E", 176.3, 1)]
    [InlineData("62° 12' 48.831\" W", -62.2, 1)]
    [InlineData("4° 46' 6.456\" W", -4.77, 2)]
    [InlineData("6° 28' 33.481\" W", -6.48, 2)]
    [InlineData("0° 10' 6.902\" W", -0.169, 3)]
    [InlineData("118° 45' 3.780\" E", 118.751, 3)]
    [InlineData("139° 19' 8.803\" E", 139.3191, 4)]
    [InlineData("141° 59' 27.377\" E", 141.9909, 4)]
    [InlineData("12°30'40.79\"E", 12.51133, 5)]
    [InlineData("74°0'21.49\"W", -74.00597, 5)]
    [InlineData("99° 44' 56.030\" W", -99.748897, 6)]
    [InlineData("21° 55' 56.083\" E", 21.932245, 6)]
    [InlineData("129° 39' 38.704\" E", 129.6607511, 7)]
    [InlineData("54° 33' 12.699\" W", -54.5535275, 7)]
    [InlineData("148° 34' 9.124\" W", -148.56920111, 8)]
    [InlineData("44° 44' 2.119\" W", -44.73392194, 8)]
    protected void Longitude_ExpectParseLiteralToMatch(
        string literal,
        double runtime,
        int precision)
    {
        // arrange
        var scalar = CreateType<LongitudeType>();
        StringValueNode valueSyntax = new(literal);

        // act
        object result = ToPrecision(scalar, valueSyntax, precision);

        // assert
        Assert.Equal(runtime, result);
    }

    [Fact]
    protected void Longitude_ExpectParseLiteralToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<LongitudeType>();
        StringValueNode valueSyntax = new("foo");

        // act
        var result = Record.Exception(() => scalar.CoerceInputLiteral(valueSyntax));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void Longitude_ExpectValueToLiteralToMatchType()
    {
        // arrange
        var scalar = CreateType<LongitudeType>();
        const double valueSyntax = 74.3d;

        // act
        var result = scalar.ValueToLiteral(valueSyntax);

        // assert
        Assert.Equal(typeof(StringValueNode), result.GetType());
    }

    [Theory]
    [UseCulture("en-US")]
    [InlineData(179d, "179° 0' 0\" E")]
    [InlineData(-179d, "179° 0' 0\" W")]
    [InlineData(174.3, "174° 18' 0\" E")]
    [InlineData(-82.2, "82° 12' 0\" W")]
    [InlineData(-165.77, "165° 46' 12\" W")]
    [InlineData(-9.48, "9° 28' 48\" W")]
    [InlineData(0.189, "0° 11' 20.4\" E")]
    [InlineData(134.296, "134° 17' 45.6\" E")]
    [InlineData(153.7891, "153° 47' 20.76\" E")]
    [InlineData(111.2939, "111° 17' 38.04\" E")]
    [InlineData(8.51133, "8° 30' 40.788\" E")]
    [InlineData(-76.00157, "76° 0' 5.652\" W")]
    [InlineData(-44.28764, "44° 17' 15.504\" W")]
    [InlineData(22.783647, "22° 47' 1.1292\" E")]
    [InlineData(175.8855460, "175° 53' 7.9656\" E")]
    [InlineData(-79.0000275, "79° 0' 0.099\" W")]
    [InlineData(-148.56920111, "148° 34' 9.123996\" W")]
    [InlineData(-44.73392194, "44° 44' 2.118984\" W")]
    protected void Longitude_ExpectValueToLiteralToMatch(double runtime, string literal)
    {
        // arrange
        var scalar = CreateType<LongitudeType>();
        StringValueNode expected = new(literal);

        // act
        ISyntaxNode result = scalar.ValueToLiteral(runtime);

        // assert
        Assert.Equal(expected, result, BySyntax);
    }

    [Fact]
    protected void Longitude_ExpectValueToLiteralToThrowSerializationException_GreaterThanMax()
    {
        // arrange
        var scalar = CreateType<LongitudeType>();
        const double runtimeValue = 181d;

        // act
        var result = Record.Exception(() => scalar.ValueToLiteral(runtimeValue));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void Longitude_ExpectValueToLiteralToThrowSerializationException_LessThanMin()
    {
        // arrange
        var scalar = CreateType<LongitudeType>();
        const double runtimeValue = -181d;

        // act
        var result = Record.Exception(() => scalar.ValueToLiteral(runtimeValue));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void Longitude_ExpectCoerceInputValueToMatch()
    {
        // arrange
        ScalarType scalar = new LongitudeType();
        const double expectedValue = -179d;
        var inputValue = JsonDocument.Parse("\"179° 0' 0.000\\\" W\"").RootElement;

        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var result = scalar.CoerceInputValue(inputValue, context.Object);

        // assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    protected void Longitude_ExpectCoerceInputValueToThrowSerializationException_LessThanMin()
    {
        // arrange
        ScalarType scalar = new LongitudeType();
        var inputValue = JsonDocument.Parse("\"-181° 0' 0.000\\\" W\"").RootElement;

        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var result = Record.Exception(() => scalar.CoerceInputValue(inputValue, context.Object));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void Longitude_ExpectCoerceInputValueToThrowSerializationException_GreaterThanMax()
    {
        // arrange
        ScalarType scalar = new LongitudeType();
        var inputValue = JsonDocument.Parse("\"182° 0' 0.000\\\" E\"").RootElement;

        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var result = Record.Exception(() => scalar.CoerceInputValue(inputValue, context.Object));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    public void Longitude_ExpectCoerceOutputValueInt()
    {
        // arrange
        ScalarType scalar = new LongitudeType();
        const double runtimeValue = 179;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultElement = resultDocument.Data.GetProperty("first");
        scalar.CoerceOutputValue(runtimeValue, resultElement);

        // assert
        resultElement.MatchSnapshot();
    }

    [Fact]
    protected void Longitude_ExpectCoerceOutputValueIntToThrowSerializationException_LessThanMin()
    {
        // arrange
        ScalarType scalar = new LongitudeType();
        const double runtimeValue = -181;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultElement = resultDocument.Data.GetProperty("first");
        var result = Record.Exception(() => scalar.CoerceOutputValue(runtimeValue, resultElement));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void Longitude_ExpectCoerceOutputValueIntToThrowSerializationException_GreaterThanMax()
    {
        // arrange
        ScalarType scalar = new LongitudeType();
        const double runtimeValue = 181;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultElement = resultDocument.Data.GetProperty("first");
        var result = Record.Exception(() => scalar.CoerceOutputValue(runtimeValue, resultElement));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    public void Longitude_ExpectCoerceOutputValueDouble()
    {
        // arrange
        ScalarType scalar = new LongitudeType();
        const double runtimeValue = 179d;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultElement = resultDocument.Data.GetProperty("first");
        scalar.CoerceOutputValue(runtimeValue, resultElement);

        // assert
        resultElement.MatchSnapshot();
    }

    [Fact]
    protected void Longitude_ExpectCoerceOutputValueDoubleToThrowSerializationException_LessThanMin()
    {
        // arrange
        ScalarType scalar = new LongitudeType();
        const double runtimeValue = -181d;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultElement = resultDocument.Data.GetProperty("first");
        var result = Record.Exception(() => scalar.CoerceOutputValue(runtimeValue, resultElement));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void Longitude_ExpectCoerceOutputValueDoubleToThrowSerializationException_GreaterThanMax()
    {
        // arrange
        ScalarType scalar = new LongitudeType();
        const double runtimeValue = 181d;

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultElement = resultDocument.Data.GetProperty("first");
        var result = Record.Exception(() => scalar.CoerceOutputValue(runtimeValue, resultElement));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    public async Task Longitude_Integration()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<DefaultLongitudeType>()
            .BuildRequestExecutorAsync();

        // act
        var res = await executor.ExecuteAsync("{ test }");

        // assert
        res.ToJson().MatchSnapshot();
    }

    public class DefaultLongitude
    {
        public double Test => 0;
    }

    public class DefaultLongitudeType : ObjectType<DefaultLongitude>
    {
        protected override void Configure(IObjectTypeDescriptor<DefaultLongitude> descriptor)
        {
            descriptor.Field(x => x.Test).Type<LongitudeType>();
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
