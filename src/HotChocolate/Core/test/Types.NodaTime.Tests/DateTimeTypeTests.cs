using System.Globalization;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NodaTime;

namespace HotChocolate.Types.NodaTime;

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
        var expectedDateTime = new OffsetDateTime(
            new LocalDateTime(2018, 6, 29, 8, 46, 14),
            Offset.FromHours(4));

        // act
        var dateTime = (OffsetDateTime)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedDateTime, dateTime);
    }

    [Theory]
    [MemberData(nameof(ValidInput))]
    public void CoerceInputLiteral_Valid(byte precision, string dateTime, OffsetDateTime result)
    {
        // arrange
        var type = new DateTimeType(new DateTimeOptions { InputPrecision = precision });
        var literal = new StringValueNode(dateTime);

        // act
        var offsetDateTime = (OffsetDateTime)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(result, offsetDateTime);
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
        var expectedDateTime = new OffsetDateTime(
            new LocalDateTime(2018, 6, 29, 8, 46, 14),
            Offset.FromHours(4));

        // act
        var dateTime = (OffsetDateTime)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedDateTime, dateTime);
    }

    [Fact]
    public void CoerceInputValue_IsoString()
    {
        // arrange
        var type = new DateTimeType();
        var inputValue = JsonDocument.Parse("\"2018-06-11T08:46:14+04:00\"").RootElement;
        var expectedDateTime = new OffsetDateTime(
            new LocalDateTime(2018, 6, 11, 8, 46, 14),
            Offset.FromHours(4));

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
        var expectedDateTime = new OffsetDateTime(
            new LocalDateTime(2018, 6, 11, 8, 46, 14),
            Offset.Zero);

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
    public void CoerceOutputValue_Valid(byte precision, OffsetDateTime dateTime, string result)
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
    public void CoerceOutputValue_OffsetDateTime()
    {
        // arrange
        var type = new DateTimeType();
        var dateTime = new OffsetDateTime(
            new LocalDateTime(2018, 6, 11, 8, 46, 14),
            Offset.FromHours(4));

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
    public void ValueToLiteral_OffsetDateTime()
    {
        // arrange
        var type = new DateTimeType();
        var dateTime = new OffsetDateTime(
            new LocalDateTime(2018, 6, 11, 8, 46, 14),
            Offset.FromHours(4));
        const string expectedLiteralValue = "2018-06-11T08:46:14+04:00";

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
        var expectedDateTime = new OffsetDateTime(
            new LocalDateTime(2018, 6, 29, 8, 46, 14),
            Offset.FromHours(4));

        // act
        var dateTime = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedDateTime, Assert.IsType<OffsetDateTime>(dateTime));
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

    [Theory]
    [InlineData(0, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:[Zz]|[+-]\d{2}:\d{2})$")]
    [InlineData(1, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,1})?(?:[Zz]|[+-]\d{2}:\d{2})$")]
    [InlineData(2, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,2})?(?:[Zz]|[+-]\d{2}:\d{2})$")]
    [InlineData(3, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,3})?(?:[Zz]|[+-]\d{2}:\d{2})$")]
    [InlineData(4, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,4})?(?:[Zz]|[+-]\d{2}:\d{2})$")]
    [InlineData(5, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,5})?(?:[Zz]|[+-]\d{2}:\d{2})$")]
    [InlineData(6, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,6})?(?:[Zz]|[+-]\d{2}:\d{2})$")]
    [InlineData(7, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:[Zz]|[+-]\d{2}:\d{2})$")]
    [InlineData(8, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,8})?(?:[Zz]|[+-]\d{2}:\d{2})$")]
    [InlineData(9, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,9})?(?:[Zz]|[+-]\d{2}:\d{2})$")]
    public void Pattern_Should_Match_InputPrecision(byte precision, string expectedPattern)
    {
        // arrange & act
        var type = new DateTimeType(new DateTimeOptions { InputPrecision = precision });

        // assert
        Assert.Equal(expectedPattern, type.Pattern);
    }

    public static TheoryData<byte, string, OffsetDateTime> ValidInput()
    {
        return new TheoryData<byte, string, OffsetDateTime>
        {
            // https://scalars.graphql.org/chillicream/date-time.html#sec-Input-spec.Examples (Valid input values)
            {
                DateTimeOptions.DefaultInputPrecision,
                "2023-12-24T15:30:00Z",
                new OffsetDateTime(new LocalDateTime(2023, 12, 24, 15, 30, 0, 0), Offset.Zero)
            },
            {
                DateTimeOptions.DefaultInputPrecision,
                "2023-12-24T15:30:00.123456789+01:00",
                new OffsetDateTime(
                    new LocalDateTime(2023, 12, 24, 15, 30, 0, 123),
                    Offset.FromHours(1)).PlusNanoseconds(456_789)
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
            // More than 8 fractional second digits with precision set to 8.
            { 8, "2023-12-24T15:30:00.123456789Z" },
            // More than 7 fractional second digits with precision set to 7.
            { 7, "2023-12-24T15:30:00.12345678Z" },
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

    public static TheoryData<byte, OffsetDateTime, string> ValidOutput()
    {
        return new TheoryData<byte, OffsetDateTime, string>
        {
            // Up to 9 fractional second digits with default precision.
            {
                DateTimeOptions.DefaultOutputPrecision,
                new OffsetDateTime(
                    new LocalDateTime(2023, 12, 24, 15, 30, 0, 123),
                    Offset.Zero).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00.123456789Z"
            },
            // Up to 8 fractional second digits with precision set to 8.
            {
                8,
                new OffsetDateTime(
                    new LocalDateTime(2023, 12, 24, 15, 30, 0, 123),
                    Offset.Zero).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00.12345678Z"
            },
            // Up to 7 fractional second digits with precision set to 7.
            {
                7,
                new OffsetDateTime(
                    new LocalDateTime(2023, 12, 24, 15, 30, 0, 123),
                    Offset.Zero).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00.1234567Z"
            },
            // Up to 6 fractional second digits with precision set to 6.
            {
                6,
                new OffsetDateTime(
                    new LocalDateTime(2023, 12, 24, 15, 30, 0, 123),
                    Offset.Zero).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00.123456Z"
            },
            // Up to 5 fractional second digits with precision set to 5.
            {
                5,
                new OffsetDateTime(
                    new LocalDateTime(2023, 12, 24, 15, 30, 0, 123),
                    Offset.Zero).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00.12345Z"
            },
            // Up to 4 fractional second digits with precision set to 4.
            {
                4,
                new OffsetDateTime(
                    new LocalDateTime(2023, 12, 24, 15, 30, 0, 123),
                    Offset.Zero).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00.1234Z"
            },
            // Up to 3 fractional second digits with precision set to 3.
            {
                3,
                new OffsetDateTime(
                    new LocalDateTime(2023, 12, 24, 15, 30, 0, 123),
                    Offset.Zero).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00.123Z"
            },
            // Up to 2 fractional second digits with precision set to 2.
            {
                2,
                new OffsetDateTime(
                    new LocalDateTime(2023, 12, 24, 15, 30, 0, 123),
                    Offset.Zero).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00.12Z"
            },
            // Up to 1 fractional second digit with precision set to 1.
            {
                1,
                new OffsetDateTime(
                    new LocalDateTime(2023, 12, 24, 15, 30, 0, 123),
                    Offset.Zero).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00.1Z"
            },
            // No fractional second digits with precision set to 0.
            {
                0,
                new OffsetDateTime(
                    new LocalDateTime(2023, 12, 24, 15, 30, 0, 123),
                    Offset.Zero).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00Z"
            }
        };
    }
}
