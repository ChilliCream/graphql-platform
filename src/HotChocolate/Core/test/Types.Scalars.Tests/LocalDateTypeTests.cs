using Xunit;
using System;
using System.Globalization;
using System.Threading;
using HotChocolate.Language;
using Snapshooter.Xunit;

namespace HotChocolate.Types;

public class LocalDateTypeTests : ScalarTypeTestBase
{
    [Fact]
    protected void Schema_WithScalar_IsMatch()
    {
        // arrange
        var schema = BuildSchema<LocalDateType>();

        // act
        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void LocalDate_EnsureDateTimeTypeKindIsCorret()
    {
        // arrange
        var type = new LocalDateType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, type.Kind);
    }

    [Fact]
    protected void LocalDate_ExpectIsStringValueToMatch()
    {
        // arrange
        var scalar = CreateType<LocalDateType>();
        var valueSyntax = new StringValueNode("2018-06-29T08:46:14+04:00");

        // act
        var result = scalar.IsInstanceOfType(valueSyntax);

        // assert
        Assert.True(result);
    }

    [Fact]
    protected void LocalDate_ExpectIsDateTimeToMatch()
    {
        // arrange
        var scalar = CreateType<LocalDateType>();
        var valueSyntax = new DateTime(2018, 6, 29, 8, 46, 14);

        // act
        var result = scalar.IsInstanceOfType(valueSyntax);

        // assert
        Assert.True(result);
    }

    [Fact]
    protected void LocalDate_ExpectParseLiteralToMatch()
    {
        // arrange
        var scalar = CreateType<LocalDateType>();
        var valueSyntax = new StringValueNode("2018-06-29T08:46:14");
        var expectedResult = new DateTime(2018, 6, 29, 8, 46, 14);

        // act
        object result = (DateTime)scalar.ParseLiteral(valueSyntax)!;

        // assert
        Assert.Equal(expectedResult, result);
    }

    [InlineData("en-US")]
    [InlineData("en-AU")]
    [InlineData("en-GB")]
    [InlineData("de-CH")]
    [InlineData("de-de")]
    [Theory]
    public void LocalDate_ParseLiteralStringValueDifferentCulture(
        string cultureName)
    {
        // arrange
        Thread.CurrentThread.CurrentCulture =
            CultureInfo.GetCultureInfo(cultureName);

        ScalarType scalar = new LocalDateType();
        var valueSyntax = new StringValueNode("2018-06-29T08:46:14+04:00");
        var expectedDateTime = new DateTimeOffset(new DateTime(2018, 6, 29, 8, 46, 14),
            new TimeSpan(4, 0, 0));

        // act
        var dateTime = (DateTime)scalar.ParseLiteral(valueSyntax)!;

        // assert
        Assert.Equal(expectedDateTime, dateTime);
    }

    [Fact]
    protected void LocalDate_ExpectParseLiteralToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<LocalDateType>();
        var valueSyntax = new StringValueNode("foo");

        // act
        var result = Record.Exception(() => scalar.ParseLiteral(valueSyntax));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void LocalDate_ExpectParseValueToMatchDateTime()
    {
        // arrange
        var scalar = CreateType<LocalDateType>();
        var valueSyntax = new DateTime(2018, 6, 29, 8, 46, 14);

        // act
        var result = scalar.ParseValue(valueSyntax);

        // assert
        Assert.Equal(typeof(StringValueNode), result.GetType());
    }

    [Fact]
    protected void LocalDate_ExpectParseValueToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<LocalDateType>();
        var runtimeValue = new StringValueNode("foo");

        // act
        var result = Record.Exception(() => scalar.ParseValue(runtimeValue));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void LocalDate_ExpectSerializeUtcToMatch()
    {
        // arrange
        ScalarType scalar = new LocalDateType();
        DateTimeOffset dateTime = new DateTime(
            2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);

        var expectedValue = "2018-06-11";

        // act
        var serializedValue = (string)scalar.Serialize(dateTime)!;

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    protected void LocalDate_ExpectSerializeDateTimeOffsetToMatch()
    {
        // arrange
        ScalarType scalar = new LocalDateType();
        var dateTime = new DateTimeOffset(
            new DateTime(2018, 6, 11, 8, 46, 14),
            new TimeSpan(4, 0, 0));
        var expectedValue = "2018-06-11";

        // act
        var serializedValue = (string)scalar.Serialize(dateTime)!;

        // assert
        Assert.Equal(expectedValue, serializedValue);
    }

    [Fact]
    protected void LocalDate_ExpectDeserializeNullToMatch()
    {
        // arrange
        ScalarType scalar = new LocalDateType();

        // act
        var success = scalar.TryDeserialize(null, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Null(deserialized);
    }

    [Fact]
    public void LocalDate_ExpectDeserializeNullableDateTimeToDateTime()
    {
        // arrange
        ScalarType scalar = new LocalDateType();
        DateTime? time = null;

        // act
        var success = scalar.TryDeserialize(time, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Null(deserialized);
    }

    [Fact]
    protected void LocalDate_ExpectDeserializeStringToMatch()
    {
        // arrange
        var scalar = CreateType<LocalDateType>();
        var runtimeValue = new DateTimeOffset(
            new DateTime(2018, 6, 11, 8, 46, 14),
            new TimeSpan(4, 0, 0));

        // act
        var deserializedValue = (DateTime)scalar
            .Deserialize("2018-06-11T08:46:14+04:00")!;

        // assert
        Assert.Equal(runtimeValue, deserializedValue);
    }

    [Fact]
    protected void LocalDate_ExpectDeserializeDateTimeOffsetToMatch()
    {
        // arrange
        var scalar = CreateType<LocalTimeType>();
        object input = new DateTimeOffset(
            new DateTime(2018, 6, 11, 8, 46, 14),
            new TimeSpan(4, 0, 0));
        object expected = new DateTime(2018, 6, 11, 8, 46, 14);

        // act
        var result = scalar.Deserialize(input);

        // assert
        Assert.Equal(result, expected);
    }

    [Fact]
    protected void LocalDate_ExpectDeserializeDateTimeToMatch()
    {
        // arrange
        var scalar = CreateType<LocalDateType>();
        object? resultValue =  new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);


        // act
        var result = scalar.Deserialize(resultValue);

        // assert
        Assert.Equal(resultValue, result);
    }

    [Fact]
    public void LocalDate_ExpectDeserializeInvalidStringToDateTime()
    {
        // arrange
        ScalarType scalar = new LocalDateType();

        // act
        var success = scalar.TryDeserialize("abc", out var _);

        // assert
        Assert.False(success);
    }

    [Fact]
    public void LocalDate_ExpectDeserializeNullToNull()
    {
        // arrange
        ScalarType scalar = new LocalDateType();

        // act
        var success = scalar.TryDeserialize(null, out var deserialized);

        // assert
        Assert.True(success);
        Assert.Null(deserialized);
    }

    [Fact]
    protected void LocalDate_ExpectSerializeToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<LocalDateType>();

        // act
        var result = Record.Exception(() => scalar.Serialize("foo"));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void LocalDate_ExpectDeserializeToThrowSerializationException()
    {
        // arrange
        var scalar = CreateType<LocalDateType>();
        object? runtimeValue = new IntValueNode(1);

        // act
        var result = Record.Exception(() => scalar.Deserialize(runtimeValue));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    protected void ParseResult_Null()
    {
        // arrange
        ScalarType scalar = new LocalDateType();

        // act
        var result = scalar.ParseResult(null);

        // assert
        Assert.Equal(typeof(NullValueNode), result.GetType());
    }

    [Fact]
    protected void ParseResult_String()
    {
        // arrange
        ScalarType scalar = new LocalDateType();
        const string valueSyntax = "2018-06-29T08:46:14+04:00";

        // act
        var result = scalar.ParseResult(valueSyntax);

        // assert
        Assert.Equal(typeof(StringValueNode), result.GetType());
    }

    [Fact]
    protected void ParseResult_SerializationException()
    {
        // arrange
        ScalarType scalar = new LocalDateType();
        IValueNode runtimeValue = new IntValueNode(1);

        // act
        var result = Record.Exception(() => scalar.ParseResult(runtimeValue));

        // assert
        Assert.IsType<SerializationException>(result);
    }

    [Fact]
    public void ParseResult_DateTime()
    {
        // arrange
        var scalar = new LocalDateType();
        var resultValue = new DateTime(2023, 6, 19, 11, 24, 0, DateTimeKind.Utc);
        var expectedLiteralValue = "2023-06-19";

        // act
        var literal = scalar.ParseResult(resultValue);

        // assert
        Assert.Equal(typeof(StringValueNode), literal.GetType());
        Assert.Equal(expectedLiteralValue, literal.Value);
    }

    [Fact]
    public void ParseResult_DateTimeOffset()
    {
        // arrange
        var scalar = new LocalDateType();
        var resultValue = new DateTimeOffset(2023, 6, 19, 11, 24, 0, new TimeSpan(6, 0, 0));
        var expectedLiteralValue = "2023-06-19";

        // act
        var literal = scalar.ParseResult(resultValue);

        // assert
        Assert.Equal(typeof(StringValueNode), literal.GetType());
        Assert.Equal(expectedLiteralValue, literal.Value);
    }
}
