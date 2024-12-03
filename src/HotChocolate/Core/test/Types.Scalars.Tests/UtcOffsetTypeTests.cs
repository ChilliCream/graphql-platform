using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

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
        var result = scalar.IsInstanceOfType(valueSyntax);

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
        var result = scalar.IsInstanceOfType(valueSyntax);

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
        var result = scalar.IsInstanceOfType(valueSyntax);

        // assert
        Assert.True(result);
    }

    [Fact]
    protected void UtcOffset_ExpectIsUtcOffsetToMatch()
    {
        // arrange
        var scalar = CreateType<UtcOffsetType>();
        var valueSyntax = TimeSpan.FromHours(12);

        // act
        var result = scalar.IsInstanceOfType(valueSyntax);

        // assert
        Assert.True(result);
    }

    [Fact]
    protected void UtcOffset_ExpectParseLiteralToMatch()
    {
        // arrange
        var scalar = CreateType<UtcOffsetType>();
        var valueSyntax = new StringValueNode("-12:00");
        var expectedResult = new TimeSpan(-12, 0, 0);

        // act
        object result = (TimeSpan)scalar.ParseLiteral(valueSyntax)!;

        // assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    protected void UtcOffset_ExpectParseLiteralToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<UtcOffsetType>();
        var valueSyntax = new StringValueNode("+17:00");

        // act
        var result = Record.Exception(() => scalar.ParseLiteral(valueSyntax));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void UtcOffset_ExpectParseValueToMatchTimeSpan()
    {
        // arrange
        var scalar = CreateType<UtcOffsetType>();
        var valueSyntax = new TimeSpan(0, 0, 0);

        // act
        var result = scalar.ParseValue(valueSyntax);

        // assert
        Assert.Equal(typeof(StringValueNode), result.GetType());
    }

    [Fact]
    protected void UtcOffset_ExpectParseValueToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<UtcOffsetType>();
        var runtimeValue = new StringValueNode("foo");

        // act
        var result = Record.Exception(() => scalar.ParseValue(runtimeValue));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void UtcOffset_ExpectSerializeToMatch()
    {
        // arrange
        ScalarType scalar = new UtcOffsetType();
        var dateTime = new TimeSpan(10, 0, 0);

        var expectedValue = "+10:00";

        // act
        var serializedValue = (string)scalar.Serialize(dateTime)!;

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    protected void UtcOffset_ExpectDeserializeNullToMatch()
    {
        // arrange
        ScalarType scalar = new UtcOffsetType();

        // act
        var success = scalar.TryDeserialize(null, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Null(deserialized);
    }

    [Fact]
    public void UtcOffset_ExpectDeserializeNullableTimeSpanToTimeSpan()
    {
        // arrange
        ScalarType scalar = new UtcOffsetType();
        TimeSpan? time = null;

        // act
        var success = scalar.TryDeserialize(time, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Null(deserialized);
    }

    [Fact]
    protected void UtcOffset_ExpectDeserializeStringToMatch()
    {
        // arrange
        var scalar = CreateType<UtcOffsetType>();
        var runtimeValue = new TimeSpan(4, 0, 0);

        // act
        var deserializedValue = (TimeSpan)scalar
            .Deserialize("+04:00")!;

        // assert
        Assert.Equal(runtimeValue, deserializedValue);
    }

    [Fact]
    protected void UtcOffset_ExpectDeserializeTimeSpanToMatch()
    {
        // arrange
        var scalar = CreateType<UtcOffsetType>();
        object resultValue = new TimeSpan(4, 0, 0);
        object runtimeValue = new TimeSpan(4, 0, 0);

        // act
        var result = scalar.Deserialize(resultValue);

        // assert
        Assert.Equal(result, runtimeValue);
    }

    [Fact]
    public void UtcOffset_ExpectDeserializeInvalidStringToTimeSpan()
    {
        // arrange
        ScalarType scalar = new UtcOffsetType();

        // act
        var success = scalar.TryDeserialize("abc", out var _);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void UtcOffset_ExpectDeserializeNullToNull()
    {
        // arrange
        ScalarType scalar = new UtcOffsetType();

        // act
        var success = scalar.TryDeserialize(null, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Null(deserialized);
    }

    [Fact]
    protected void UtcOffset_ExpectSerializeToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<UtcOffsetType>();

        // act
        var result = Record.Exception(() => scalar.Serialize("foo"));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void UtcOffset_ExpectDeserializeToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<UtcOffsetType>();
        object runtimeValue = new IntValueNode(1);

        // act
        var result = Record.Exception(() => scalar.Deserialize(runtimeValue));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void UtcOffset_ExpectParseResultToMatchNull()
    {
        // arrange
        ScalarType scalar = new UtcOffsetType();

        // act
        var result = scalar.ParseResult(null);

        // assert
        Assert.Equal(typeof(NullValueNode), result.GetType());
    }

    [Fact]
    protected void UtcOffset_ExpectParseResultToMatchStringValue()
    {
        // arrange
        ScalarType scalar = new UtcOffsetType();
        const string valueSyntax = "-02:00";

        // act
        var result = scalar.ParseResult(valueSyntax);

        // assert
        Assert.Equal(typeof(StringValueNode), result.GetType());
    }

    [Fact]
    protected void UtcOffset_ExpectParseResultToThrowSerializationException()
    {
        // arrange
        ScalarType scalar = new UtcOffsetType();
        IValueNode runtimeValue = new IntValueNode(1);

        // act
        var result = Record.Exception(() => scalar.ParseResult(runtimeValue));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    public async Task Integration_DefaultUtcOffset()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<DefaultUtcOffsetType>()
            .BuildRequestExecutorAsync();

        // act
        var res = await executor.ExecuteAsync("{ test }");

        // assert
        res.ToJson().MatchSnapshot();
    }

    public class DefaultUtcOffset
    {
        public TimeSpan Test => new();
    }

    public class DefaultUtcOffsetType : ObjectType<DefaultUtcOffset>
    {
        protected override void Configure(IObjectTypeDescriptor<DefaultUtcOffset> descriptor)
        {
            descriptor.Field(x => x.Test).Type<UtcOffsetType>();
        }
    }
}
