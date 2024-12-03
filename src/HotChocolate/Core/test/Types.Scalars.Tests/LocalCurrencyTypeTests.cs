using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class LocalCurrencyTypeTests : ScalarTypeTestBase
{
    [Fact]
    protected void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<LocalCurrencyType>();

        // act

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void LocalCurrency_EnsureLocalCurrencyTypeKindIsCorrect()
    {
        // arrange
        var type = new LocalCurrencyType("Germany","de-DE");

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, kind);
    }

    [Fact]
    public void LocalCurrency_EnsureLocalCurrencyTypeKindIsCorrect1()
    {
        // arrange
        var type = new LocalCurrencyType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, kind);
    }

    [Fact]
    protected void LocalCurrency_ExpectIsStringValueToMatch()
    {
        // arrange
        var scalar = CreateType<LocalCurrencyType>();
        var valueSyntax = new StringValueNode("$10.99");

        // act
        var result = scalar.IsInstanceOfType(valueSyntax);

        // assert
        Assert.True(result);
    }

    [Fact]
    protected void LocalCurrency_ParseResult_Null()
    {
        // arrange
        var scalar = CreateType<LocalCurrencyType>();

        // act
        var result = scalar.ParseResult(null);

        // assert
        Assert.Equal(NullValueNode.Default, result);
    }

    [Fact]
    protected void LocalCurrency_ExpectIsStringValueToMatchEuro()
    {
        // arrange
        ScalarType scalar = new LocalCurrencyType("Germany", "de-De");
        var valueSyntax = new StringValueNode("10,99 €");

        // act
        var result = scalar.IsInstanceOfType(valueSyntax);

        // assert
        Assert.True(result);
    }

    [Fact]
    protected void LocalCurrency_ExpectIsStringValueToNotMatchEuro()
    {
        // arrange
        ScalarType scalar = new LocalCurrencyType("Germany", "de-De");
        var valueSyntax = new StringValueNode("$10.99");

        // act
        var result = Record.Exception(() => scalar.ParseLiteral(valueSyntax));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void LocalCurrency_ExpectParseLiteralToMatch()
    {
        // arrange
        var scalar = CreateType<LocalCurrencyType>();
        var valueSyntax = new StringValueNode("$24.99");
        const decimal expectedResult = 24.99m;

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
        var valueSyntax = new StringValueNode("24,99 €");
        const decimal expectedResult = 24.99m;

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
    public void LocalCurrency_ParseLiteralStringValueDifferentCulture(string name, string cultureName)
    {
        // arrange
        ScalarType scalar = new LocalCurrencyType(name, cultureName);
        var valueSyntax = new StringValueNode("9.99");
        const decimal expectedDecimal = 9.99m;

        // act
        var result = (decimal)scalar.ParseLiteral(valueSyntax)!;

        // assert
        Assert.Equal(expectedDecimal, result);
    }

    [Fact]
    protected void LocalCurrency_ExpectParseLiteralToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<LocalCurrencyType>();
        var valueSyntax = new StringValueNode("foo");

        // act
        var result = Record.Exception(() => scalar.ParseLiteral(valueSyntax));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void LocalCurrency_ExpectParseValueToMatchDecimal()
    {
        // arrange
        var scalar = CreateType<LocalCurrencyType>();
        const decimal valueSyntax = 24.95m;

        // act
        var result = scalar.ParseValue(valueSyntax);

        // assert
        Assert.Equal(typeof(StringValueNode), result.GetType());
    }

    [Fact]
    protected void LocalCurrency_ExpectParseValueToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<LocalCurrencyType>();
        var runtimeValue = new StringValueNode("foo");

        // act
        var result = Record.Exception(() => scalar.ParseValue(runtimeValue));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void LocalCurrency_ExpectSerializeDecimalToMatch()
    {
        // arrange
        ScalarType scalar = new LocalCurrencyType();
        const decimal runtimeValue = 9.99m;
        const string expectedValue = "$9.99";

        // act
        var serializedValue = (string)scalar.Serialize(runtimeValue)!;

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    protected void LocalCurrency_ExpectDeserializeNullToMatch()
    {
        // arrange
        ScalarType scalar = new LocalCurrencyType();

        // act
        var success = scalar.TryDeserialize(null, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Null(deserialized);
    }

    [Fact]
    public void LocalCurrency_ExpectDeserializeNullableDecimalToDecimal()
    {
        // arrange
        ScalarType scalar = new LocalCurrencyType();
        decimal? runtimeValue = null;

        // act
        var success = scalar.TryDeserialize(runtimeValue, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Null(deserialized);
    }

    [Fact]
    protected void LocalCurrency_ExpectDeserializeStringToMatch()
    {
        // arrange
        var scalar = CreateType<LocalCurrencyType>();
        const decimal runtimeValue = 7.99m;

        // act
        var deserializedValue = (decimal)scalar.Deserialize("$7.99")!;

        // assert
        Assert.Equal(runtimeValue, deserializedValue);
    }

    [Fact]
    protected void LocalCurrency_ExpectDeserializeDecimalToMatch()
    {
        // arrange
        var scalar = CreateType<LocalCurrencyType>();
        object resultValue = 0.99m;

        // act
        var result = scalar.Deserialize(resultValue);

        // assert
        Assert.Equal(resultValue, result);
    }

    [Fact]
    public void LocalCurrency_ExpectDeserializeInvalidStringToDecimal()
    {
        // arrange
        ScalarType scalar = new LocalCurrencyType();

        // act
        var success = scalar.TryDeserialize("abc", out var _);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void LocalCurrency_ExpectDeserializeNullToNull()
    {
        // arrange
        ScalarType scalar = new LocalCurrencyType();

        // act
        var success = scalar.TryDeserialize(null, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Null(deserialized);
    }

    [Fact]
    protected void LocalCurrency_ExpectSerializeToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<LocalCurrencyType>();

        // act
        var result = Record.Exception(() => scalar.Serialize("foo"));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void LocalCurrency_ExpectDeserializeToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<LocalCurrencyType>();
        object runtimeValue = new IntValueNode(1);

        // act
        var result = Record.Exception(() => scalar.Deserialize(runtimeValue));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void LocalCurrency_ExpectParseResultToMatchNull()
    {
        // arrange
        ScalarType scalar = new LocalCurrencyType();

        // act
        var result = scalar.ParseResult(null);

        // assert
        Assert.Equal(typeof(NullValueNode), result.GetType());
    }

    [Fact]
    protected void LocalCurrency_ExpectParseResultToMatchStringValue()
    {
        // arrange
        ScalarType scalar = new LocalCurrencyType();
        const string valueSyntax = "$9.99";

        // act
        var result = scalar.ParseResult(valueSyntax);

        // assert
        Assert.Equal(typeof(StringValueNode), result.GetType());
    }

    [Fact]
    protected void LocalCurrency_ExpectParseResultToThrowSerializationException()
    {
        // arrange
        ScalarType scalar = new LocalCurrencyType();
        IValueNode runtimeValue = new IntValueNode(1);

        // act
        var result = Record.Exception(() => scalar.ParseResult(runtimeValue));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    public async Task Integration_DefaultLocalCurrency()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<DefaultLocalCurrencyType>()
            .BuildRequestExecutorAsync();

        // act
        var res = await executor.ExecuteAsync("{ test }");

        // assert
        res.ToJson().MatchSnapshot();
    }

    public class DefaultLocalCurrency
    {
        public decimal Test => new();
    }

    public class DefaultLocalCurrencyType : ObjectType<DefaultLocalCurrency>
    {
        protected override void Configure(IObjectTypeDescriptor<DefaultLocalCurrency> descriptor)
        {
            descriptor.Field(x => x.Test).Type<LocalCurrencyType>();
        }
    }
}
