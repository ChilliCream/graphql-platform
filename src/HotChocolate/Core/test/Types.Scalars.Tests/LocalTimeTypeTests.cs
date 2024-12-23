using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types;

public class LocalTimeTypeTests : ScalarTypeTestBase
{
    [Fact]
    protected void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<LocalTimeType>();

        // act

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void LocalTime_EnsureLocalTimeTypeKindIsCorrect()
    {
        // arrange
        var type = new LocalTimeType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, kind);
    }

    [Fact]
    protected void LocalTime_ExpectIsStringValueToMatch()
    {
        // arrange
        var scalar = CreateType<LocalTimeType>();
        var valueSyntax = new StringValueNode("08:46:14");

        // act
        var result = scalar.IsInstanceOfType(valueSyntax);

        // assert
        Assert.True(result);
    }

    [Fact]
    protected void LocalTime_ExpectIsTimeOnlyToMatch()
    {
        // arrange
        var scalar = CreateType<LocalTimeType>();
        var valueSyntax = new TimeOnly(8, 46, 14);

        // act
        var result = scalar.IsInstanceOfType(valueSyntax);

        // assert
        Assert.True(result);
    }

    [Fact]
    protected void LocalTime_ExpectParseLiteralToMatch()
    {
        // arrange
        var scalar = CreateType<LocalTimeType>();
        var valueSyntax = new StringValueNode("14:46:14");
        var expectedResult = new TimeOnly(14, 46, 14);

        // act
        object result = (TimeOnly)scalar.ParseLiteral(valueSyntax)!;

        // assert
        Assert.Equal(expectedResult, result);
    }

    [InlineData("en-US")]
    [InlineData("en-AU")]
    [InlineData("en-GB")]
    [InlineData("de-CH")]
    [InlineData("de-de")]
    [Theory]
    public void LocalTime_ParseLiteralStringValueDifferentCulture(string cultureName)
    {
        // arrange
        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

        ScalarType scalar = new LocalTimeType();
        var valueSyntax = new StringValueNode("08:46:14");
        var expectedTimeOnly = new TimeOnly(8, 46, 14);

        // act
        var dateTime = (TimeOnly)scalar.ParseLiteral(valueSyntax)!;

        // assert
        Assert.Equal(expectedTimeOnly, dateTime);
    }

    [Fact]
    protected void LocalTime_ExpectParseLiteralToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<LocalTimeType>();
        var valueSyntax = new StringValueNode("foo");

        // act
        var result = Record.Exception(() => scalar.ParseLiteral(valueSyntax));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void LocalTime_ExpectParseValueToMatchTimeOnly()
    {
        // arrange
        var scalar = CreateType<LocalTimeType>();
        var valueSyntax = new TimeOnly(8, 46, 14);

        // act
        var result = scalar.ParseValue(valueSyntax);

        // assert
        Assert.Equal(typeof(StringValueNode), result.GetType());
    }

    [Fact]
    protected void LocalTime_ExpectParseValueToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<LocalTimeType>();
        var runtimeValue = new StringValueNode("foo");

        // act
        var result = Record.Exception(() => scalar.ParseValue(runtimeValue));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void LocalTime_ExpectSerializeUtcToMatch()
    {
        // arrange
        ScalarType scalar = new LocalTimeType();
        DateTimeOffset dateTime = new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);
        const string expectedValue = "08:46:14";

        // act
        var serializedValue = (string)scalar.Serialize(dateTime)!;

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    protected void LocalTime_ExpectSerializeTimeOnlyToMatch()
    {
        // arrange
        ScalarType scalar = new LocalTimeType();
        var timeOnly = new TimeOnly(8, 46, 14);
        var expectedValue = "08:46:14";

        // act
        var serializedValue = (string)scalar.Serialize(timeOnly)!;

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    protected void LocalTime_ExpectSerializeDateTimeToMatch()
    {
        // arrange
        ScalarType scalar = new LocalTimeType();
        var dateTime = new DateTime(2018, 6, 11, 8, 46, 14);
        var expectedValue = "08:46:14";

        // act
        var serializedValue = (string)scalar.Serialize(dateTime)!;

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    protected void LocalTime_ExpectSerializeDateTimeOffsetToMatch()
    {
        // arrange
        ScalarType scalar = new LocalTimeType();
        var dateTime = new DateTimeOffset(
            new DateTime(2018, 6, 11, 8, 46, 14),
            new TimeSpan(4, 0, 0));
        var expectedValue = "08:46:14";

        // act
        var serializedValue = (string)scalar.Serialize(dateTime)!;

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    protected void LocalTime_ExpectDeserializeNullToMatch()
    {
        // arrange
        ScalarType scalar = new LocalTimeType();

        // act
        var success = scalar.TryDeserialize(null, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Null(deserialized);
    }

    [Fact]
    public void LocalTime_ExpectDeserializeNullableTimeOnlyToTimeOnly()
    {
        // arrange
        ScalarType scalar = new LocalTimeType();
        TimeOnly? time = null;

        // act
        var success = scalar.TryDeserialize(time, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Null(deserialized);
    }

    [Fact]
    protected void LocalTime_ExpectDeserializeStringToMatch()
    {
        // arrange
        var scalar = CreateType<LocalTimeType>();
        var runtimeValue = new TimeOnly(8, 46, 14);

        // act
        var deserializedValue = (TimeOnly)scalar.Deserialize("08:46:14")!;

        // assert
        Assert.Equal(runtimeValue, deserializedValue);
    }

    [Fact]
    protected void LocalTime_ExpectDeserializeTimeOnlyToMatch()
    {
        // arrange
        var scalar = CreateType<LocalTimeType>();
        object resultValue = new TimeOnly(8, 46, 14);

        // act
        var result = scalar.Deserialize(resultValue);

        // assert
        Assert.Equal(resultValue, result);
    }

    [Fact]
    protected void LocalTime_ExpectDeserializeDateTimeToMatch()
    {
        // arrange
        var scalar = CreateType<LocalTimeType>();
        object resultValue = new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);
        var expected = new TimeOnly(8, 46, 14);

        // act
        var result = scalar.Deserialize(resultValue);

        // assert
        Assert.Equal(expected, result);
    }

    [Fact]
    protected void LocalTime_ExpectDeserializeDateTimeOffsetToMatch()
    {
        // arrange
        var scalar = CreateType<LocalTimeType>();
        var input = new DateTimeOffset(
            new DateTime(2018, 6, 11, 8, 46, 14),
            new TimeSpan(4, 0, 0));
        var expected = new TimeOnly(8, 46, 14);

        // act
        var result = scalar.Deserialize(input);

        // assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void LocalTime_ExpectDeserializeInvalidFormatToTimeOnly()
    {
        // arrange
        ScalarType scalar = new LocalTimeType();

        // act
        var success = scalar.TryDeserialize("08:46:14 pm", out var _);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void LocalTime_ExpectDeserializeInvalidStringToTimeOnly()
    {
        // arrange
        ScalarType scalar = new LocalTimeType();

        // act
        var success = scalar.TryDeserialize("abc", out var _);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void LocalTime_ExpectDeserializeNullToNull()
    {
        // arrange
        ScalarType scalar = new LocalTimeType();

        // act
        var success = scalar.TryDeserialize(null, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Null(deserialized);
    }

    [Fact]
    protected void LocalTime_ExpectSerializeToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<LocalTimeType>();

        // act
        var result = Record.Exception(() => scalar.Serialize("foo"));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void LocalTime_ExpectDeserializeToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<LocalTimeType>();
        object runtimeValue = new IntValueNode(1);

        // act
        var result = Record.Exception(() => scalar.Deserialize(runtimeValue));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void LocalTime_ExpectParseResultToMatchNull()
    {
        // arrange
        ScalarType scalar = new LocalTimeType();

        // act
        var result = scalar.ParseResult(null);

        // assert
        Assert.Equal(typeof(NullValueNode), result.GetType());
    }

    [Fact]
    protected void LocalTime_ExpectParseResultToMatchStringValue()
    {
        // arrange
        ScalarType scalar = new LocalTimeType();
        const string valueSyntax = "08:46:14";

        // act
        var result = scalar.ParseResult(valueSyntax);

        // assert
        Assert.Equal(typeof(StringValueNode), result.GetType());
    }

    [Fact]
    protected void LocalTime_ExpectParseResultToThrowSerializationException()
    {
        // arrange
        ScalarType scalar = new LocalTimeType();
        IValueNode runtimeValue = new IntValueNode(1);

        // act
        var result = Record.Exception(() => scalar.ParseResult(runtimeValue));

        // assert
        Assert.IsType<SerializationException>(result);
    }
}
