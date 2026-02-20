using System.Globalization;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class DateTimeTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new DateTimeType();

        // assert
        Assert.Equal("DateTime", type.Name);
    }

    [Fact]
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new DateTimeType();
        var literal = new StringValueNode("2018-06-29T08:46:14+04:00");
        var expectedDateTime = new DateTimeOffset(
            new DateTime(2018, 6, 29, 8, 46, 14),
            new TimeSpan(4, 0, 0));

        // act
        var dateTime = (DateTimeOffset)type.CoerceInputLiteral(literal)!;

        // assert
        Assert.Equal(expectedDateTime, dateTime);
    }

    [Theory]
    [MemberData(nameof(ValidInput))]
    public void CoerceInputLiteral_Valid(byte precision, string dateTime, DateTimeOffset result)
    {
        // arrange
        var type = new DateTimeType(new DateTimeOptions { InputPrecision = precision });
        var literal = new StringValueNode(dateTime);

        // act
        var dateTimeOffset = (DateTimeOffset?)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(result, dateTimeOffset);
    }

    [Theory]
    [MemberData(nameof(InvalidInput))]
    public void CoerceInputLiteral_Invalid(byte precision, string dateTime)
    {
        // arrange
        var type = new DateTimeType(new DateTimeOptions { InputPrecision = precision });
        var literal = new StringValueNode(dateTime);

        // act
        void Action() => type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(
            "DateTime cannot coerce the given literal of type `StringValue` to a runtime value.",
            Assert.Throws<LeafCoercionException>(Action).Message);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("en-AU")]
    [InlineData("en-GB")]
    [InlineData("de-CH")]
    [InlineData("de-de")]
    public void CoerceInputLiteral_DifferentCulture(string cultureName)
    {
        // arrange
        Thread.CurrentThread.CurrentCulture =
            CultureInfo.GetCultureInfo(cultureName);

        var type = new DateTimeType();
        var literal = new StringValueNode("2018-06-29T08:46:14+04:00");
        var expectedDateTime = new DateTimeOffset(
            new DateTime(2018, 6, 29, 8, 46, 14),
            new TimeSpan(4, 0, 0));

        // act
        var dateTime = (DateTimeOffset)type.CoerceInputLiteral(literal)!;

        // assert
        Assert.Equal(expectedDateTime, dateTime);
    }

    [Fact]
    public void CoerceInputValue_IsoString()
    {
        // arrange
        var type = new DateTimeType();
        var inputValue = JsonDocument.Parse("\"2018-06-11T08:46:14+04:00\"").RootElement;
        var expectedDateTime = new DateTimeOffset(
            new DateTime(2018, 6, 11, 8, 46, 14),
            new TimeSpan(4, 0, 0));

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(expectedDateTime, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_ZuluString()
    {
        // arrange
        var type = new DateTimeType();
        var inputValue = JsonDocument.Parse("\"2018-06-11T08:46:14.000Z\"").RootElement;
        var expectedDateTime = new DateTimeOffset(
            new DateTime(2018, 6, 11, 8, 46, 14),
            TimeSpan.Zero);

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(expectedDateTime, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_Invalid_Format()
    {
        // arrange
        var type = new DateTimeType();
        var inputValue = JsonDocument.Parse("\"abc\"").RootElement;

        // act
        void Action() => type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Theory]
    [MemberData(nameof(ValidOutput))]
    public void CoerceOutputValue_Valid(byte precision, DateTimeOffset dateTime, string result)
    {
        // arrange
        var type = new DateTimeType(new DateTimeOptions { OutputPrecision = precision });

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(dateTime, resultValue);

        // assert
        resultValue.MatchInlineSnapshot($"\"{result}\"");
    }

    [Fact]
    public void CoerceOutputValue_Utc_DateTimeOffset()
    {
        // arrange
        var type = new DateTimeType();
        DateTimeOffset dateTime = new DateTime(
            2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(dateTime, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("\"2018-06-11T08:46:14Z\"");
    }

    [Fact]
    public void CoerceOutputValue_DateTimeOffset()
    {
        // arrange
        var type = new DateTimeType();
        var dateTime = new DateTimeOffset(
            new DateTime(2018, 6, 11, 8, 46, 14),
            new TimeSpan(4, 0, 0));

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(dateTime, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("\"2018-06-11T08:46:14+04:00\"");
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Format()
    {
        // arrange
        var type = new DateTimeType();

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        void Action() => type.CoerceOutputValue("foo", resultValue);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void ValueToLiteral_DateTimeOffset()
    {
        // arrange
        var type = new DateTimeType();
        var dateTime = new DateTimeOffset(
            new DateTime(2018, 6, 11, 8, 46, 14),
            new TimeSpan(4, 0, 0));
        const string expectedLiteralValue = "2018-06-11T08:46:14+04:00";

        // act
        var stringLiteral = (StringValueNode)type.ValueToLiteral(dateTime);

        // assert
        Assert.Equal(expectedLiteralValue, stringLiteral.Value);
    }

    [Fact]
    public void ValueToLiteral_Utc_DateTimeOffset()
    {
        // arrange
        var type = new DateTimeType();
        DateTimeOffset dateTime =
            new DateTime(2018, 6, 11, 8, 46, 14, DateTimeKind.Utc);
        const string expectedLiteralValue = "2018-06-11T08:46:14Z";

        // act
        var stringLiteral = (StringValueNode)type.ValueToLiteral(dateTime);

        // assert
        Assert.Equal(expectedLiteralValue, stringLiteral.Value);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new DateTimeType();
        var literal = new StringValueNode("2018-06-29T08:46:14+04:00");
        var expectedDateTime = new DateTimeOffset(
            new DateTime(2018, 6, 29, 8, 46, 14),
            new TimeSpan(4, 0, 0));

        // act
        var dateTime = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedDateTime, Assert.IsType<DateTimeOffset>(dateTime));
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new DateTimeType();

        // act
        void Action() => type.CoerceInputLiteral(new IntValueNode(123));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void Ensure_TypeKind_Is_Scalar()
    {
        // arrange
        var type = new DateTimeType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, kind);
    }

    [Fact]
    public async Task Integration_DefaultDateTime()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<DefaultDateTime>()
            .BuildRequestExecutorAsync();

        // act
        var res = await executor.ExecuteAsync("{ test }");

        // assert
        res.ToJson().MatchSnapshot();
    }

    [Fact]
    public void DateTime_Relaxed_Format_Check()
    {
        // arrange
        const string s = "2011-08-30";

        // act
        var type = new DateTimeType(disableFormatCheck: true);
        var inputValue = JsonDocument.Parse($"\"{s}\"").RootElement;
        var result = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.IsType<DateTimeOffset>(result);
    }

    public class DefaultDateTime
    {
        public DateTime Test => default;
    }

    public static TheoryData<byte, string, DateTimeOffset> ValidInput()
    {
        return new TheoryData<byte, string, DateTimeOffset>
        {
            // https://scalars.graphql.org/chillicream/date-time.html#sec-Input-spec.Examples (Valid input values)
            {
                DateTimeOptions.DefaultInputPrecision,
                "2023-12-24T15:30:00Z",
                new DateTimeOffset(2023, 12, 24, 15, 30, 0, 0, TimeSpan.Zero)
            },
            // Additional cases.
            // Up to 7 fractional second digits.
            {
                DateTimeOptions.DefaultInputPrecision,
                "2023-12-24T15:30:00.1234567+01:00",
                new DateTimeOffset(2023, 12, 24, 15, 30, 0, 123, 456, TimeSpan.FromHours(1)).AddTicks(7)
            }
        };
    }

    public static TheoryData<byte, string> InvalidInput()
    {
        return new TheoryData<byte, string>
        {
            // https://scalars.graphql.org/chillicream/date-time.html#sec-Input-spec.Examples (Invalid input values)
            // Missing time zone offset.
            { DateTimeOptions.DefaultInputPrecision, "2023-12-24T15:30:00" },
            // Space instead of T or t separator.
            { DateTimeOptions.DefaultInputPrecision, "2023-12-24 15:30:00Z" },
            // Invalid hour (25).
            { DateTimeOptions.DefaultInputPrecision, "2023-12-24T25:00:00Z" },
            // Invalid minute (60).
            { DateTimeOptions.DefaultInputPrecision, "2023-12-24T15:60:00Z" },
            // ReSharper disable once GrammarMistakeInComment
            // Invalid date (February 30th).
            { DateTimeOptions.DefaultInputPrecision, "2023-02-30T15:30:00Z" },
            // More than 9 fractional second digits.
            { DateTimeOptions.DefaultInputPrecision, "2023-12-24T15:30:00.1234567890Z" },
            // Invalid offset (exceeds maximum).
            { DateTimeOptions.DefaultInputPrecision, "2023-12-24T15:30:00+25:00" },
            // Invalid offset format.
            { DateTimeOptions.DefaultInputPrecision, "2023-12-24T15:30:00 UTC" },
            // Additional cases.
            // More than 7 fractional second digits with default precision.
            { DateTimeOptions.DefaultInputPrecision, "2023-12-24T15:30:00.12345678Z" },
            // More than 6 fractional second digits with precision set to 6.
            { 6, "2023-12-24T15:30:00.1234567Z" },
            // More than 5 fractional second digits with precision set to 5.
            { 5, "2023-12-24T15:30:00.123456Z" },
            // More than 4 fractional second digits with precision set to 4.
            { 4, "2023-12-24T15:30:00.12345Z" },
            // More than 3 fractional second digits with precision set to 3.
            { 3, "2023-12-24T15:30:00.1234Z" },
            // More than 2 fractional second digits with precision set to 2.
            { 2, "2023-12-24T15:30:00.123Z" },
            // More than 1 fractional second digit with precision set to 1.
            { 1, "2023-12-24T15:30:00.12Z" },
            // Fractional second digits with precision set to 0.
            { 0, "2023-12-24T15:30:00.1Z" }
        };
    }

    public static TheoryData<byte, DateTimeOffset, string> ValidOutput()
    {
        return new TheoryData<byte, DateTimeOffset, string>
        {
            // Up to 7 fractional second digits with default precision.
            {
                DateTimeOptions.DefaultOutputPrecision,
                new DateTimeOffset(2023, 12, 24, 15, 30, 0, 123, 456, TimeSpan.Zero).AddTicks(7),
                "2023-12-24T15:30:00.1234567Z"
            },
            // Up to 6 fractional second digits with precision set to 6.
            {
                6,
                new DateTimeOffset(2023, 12, 24, 15, 30, 0, 123, 456, TimeSpan.Zero).AddTicks(7),
                "2023-12-24T15:30:00.123456Z"
            },
            // Up to 5 fractional second digits with precision set to 5.
            {
                5,
                new DateTimeOffset(2023, 12, 24, 15, 30, 0, 123, 456, TimeSpan.Zero).AddTicks(7),
                "2023-12-24T15:30:00.12345Z"
            },
            // Up to 4 fractional second digits with precision set to 4.
            {
                4,
                new DateTimeOffset(2023, 12, 24, 15, 30, 0, 123, 456, TimeSpan.Zero).AddTicks(7),
                "2023-12-24T15:30:00.1234Z"
            },
            // Up to 3 fractional second digits with precision set to 3.
            {
                3,
                new DateTimeOffset(2023, 12, 24, 15, 30, 0, 123, 456, TimeSpan.Zero).AddTicks(7),
                "2023-12-24T15:30:00.123Z"
            },
            // Up to 2 fractional second digits with precision set to 2.
            {
                2,
                new DateTimeOffset(2023, 12, 24, 15, 30, 0, 123, 456, TimeSpan.Zero).AddTicks(7),
                "2023-12-24T15:30:00.12Z"
            },
            // Up to 1 fractional second digit with precision set to 1.
            {
                1,
                new DateTimeOffset(2023, 12, 24, 15, 30, 0, 123, 456, TimeSpan.Zero).AddTicks(7),
                "2023-12-24T15:30:00.1Z"
            },
            // No fractional second digits with precision set to 0.
            {
                0,
                new DateTimeOffset(2023, 12, 24, 15, 30, 0, 123, 456, TimeSpan.Zero).AddTicks(7),
                "2023-12-24T15:30:00Z"
            }
        };
    }
}
