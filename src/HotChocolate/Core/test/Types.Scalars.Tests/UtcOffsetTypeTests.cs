using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using Moq;

namespace HotChocolate.Types;

public class UtcOffsetTypeTests : ScalarTypeTestBase
{
    [Fact]
    protected void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<UtcOffsetType>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void UtcOffset_EnsureUtcOffsetTypeKindIsCorrect()
    {
        // arrange
        // act
        var type = new UtcOffsetType();

        // assert
        Assert.Equal(TypeKind.Scalar, type.Kind);
    }

    [Fact]
    protected void UtcOffset_ExpectIsStringValueToMatch()
    {
        // arrange
        var scalar = CreateType<UtcOffsetType>();
        var valueSyntax = new StringValueNode("+12:00");

        // act
        var result = scalar.IsValueCompatible(valueSyntax);

        // assert
        Assert.True(result);
    }

    [Fact]
    protected void UtcOffset_ExpectNegativeIsStringValueToMatch()
    {
        // arrange
        var scalar = CreateType<UtcOffsetType>();
        var valueSyntax = new StringValueNode("-12:00");

        // act
        var result = scalar.IsValueCompatible(valueSyntax);

        // assert
        Assert.True(result);
    }

    [Fact]
    protected void UtcOffset_ExpectPositiveIsStringValueToMatch()
    {
        // arrange
        var scalar = CreateType<UtcOffsetType>();
        var valueSyntax = new StringValueNode("-00:00");

        // act
        var result = scalar.IsValueCompatible(valueSyntax);

        // assert
        Assert.True(result);
    }

    [Fact]
    protected void UtcOffset_ExpectCoerceInputLiteralToMatch()
    {
        // arrange
        var scalar = CreateType<UtcOffsetType>();
        var valueSyntax = new StringValueNode("-12:00");
        var expectedResult = new TimeSpan(-12, 0, 0);

        // act
        object result = (TimeSpan)scalar.CoerceInputLiteral(valueSyntax)!;

        // assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    protected void UtcOffset_ExpectCoerceInputLiteralToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<UtcOffsetType>();
        var valueSyntax = new StringValueNode("+17:00");

        // act
        var result = Record.Exception(() => scalar.CoerceInputLiteral(valueSyntax));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void UtcOffset_ExpectValueToLiteralToMatchTimeSpan()
    {
        // arrange
        var scalar = CreateType<UtcOffsetType>();
        var valueSyntax = new TimeSpan(0, 0, 0);

        // act
        var result = scalar.ValueToLiteral(valueSyntax);

        // assert
        Assert.Equal(typeof(StringValueNode), result.GetType());
    }

    [Fact]
    protected void UtcOffset_ExpectValueToLiteralToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<UtcOffsetType>();
        var runtimeValue = new StringValueNode("foo");

        // act
        var result = Record.Exception(() => scalar.ValueToLiteral(runtimeValue));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void UtcOffset_ExpectCoerceOutputValueToMatch()
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToMatch<UtcOffsetType>(new TimeSpan(10, 0, 0));
    }

    [Fact]
    protected void UtcOffset_ExpectCoerceInputValueStringToMatch()
    {
        // arrange
        var scalar = CreateType<UtcOffsetType>();
        var runtimeValue = new TimeSpan(4, 0, 0);
        using var doc = JsonDocument.Parse("\"+04:00\"");
        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var deserializedValue = (TimeSpan)scalar.CoerceInputValue(doc.RootElement, context.Object)!;

        // assert
        Assert.Equal(runtimeValue, deserializedValue);
    }

    [Fact]
    public void UtcOffset_ExpectCoerceInputValueInvalidStringToThrow()
    {
        // arrange
        var scalar = CreateType<UtcOffsetType>();
        using var doc = JsonDocument.Parse("\"abc\"");
        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var result = Record.Exception(() => scalar.CoerceInputValue(doc.RootElement, context.Object));

        // assert
        Assert.IsType<LeafCoercionException>(result);
    }

    [Fact]
    protected void UtcOffset_ExpectCoerceOutputValueToThrowSerializationException()
    {
        // arrange
        // act
        // assert
        ExpectCoerceOutputValueToThrow<UtcOffsetType>("foo");
    }
}
