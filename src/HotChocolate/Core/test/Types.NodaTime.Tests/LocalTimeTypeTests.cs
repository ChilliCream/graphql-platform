using System.Globalization;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NodaTime;

namespace HotChocolate.Types.NodaTime;

public class LocalTimeTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new LocalTimeType();

        // assert
        Assert.Equal("LocalTime", type.Name);
    }

    [Fact]
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new LocalTimeType();
        var literal = new StringValueNode("08:46:14");
        var expectedLocalTime = new LocalTime(8, 46, 14);

        // act
        var localTime = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedLocalTime, localTime);
    }

    [Theory]
    [MemberData(nameof(ValidInput))]
    public void CoerceInputLiteral_Valid(byte precision, string time, LocalTime result)
    {
        // arrange
        var type = new LocalTimeType(new DateTimeOptions { InputPrecision = precision });
        var literal = new StringValueNode(time);

        // act
        var localTime = (LocalTime?)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(result, localTime);
    }

    [Theory]
    [MemberData(nameof(InvalidInput))]
    public void CoerceInputLiteral_Invalid(byte precision, string time)
    {
        // arrange
        var type = new LocalTimeType(new DateTimeOptions { InputPrecision = precision });
        var literal = new StringValueNode(time);

        // act
        void Action() => type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(
            "LocalTime cannot coerce the given literal of type `StringValue` to a runtime value.",
            Assert.Throws<LeafCoercionException>(Action).Message);
    }

    [InlineData("en-US")]
    [InlineData("en-AU")]
    [InlineData("en-GB")]
    [InlineData("de-CH")]
    [InlineData("de-de")]
    [Theory]
    public void CoerceInputLiteral_DifferentCulture(string cultureName)
    {
        // arrange
        Thread.CurrentThread.CurrentCulture =
            CultureInfo.GetCultureInfo(cultureName);

        var type = new LocalTimeType();
        var literal = new StringValueNode("08:46:14");
        var expectedLocalTime = new LocalTime(8, 46, 14);

        // act
        var localTime = (LocalTime)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedLocalTime, localTime);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Format()
    {
        // arrange
        var type = new LocalTimeType();
        var literal = new StringValueNode("abc");

        // act
        void Action() => type.CoerceInputLiteral(literal);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputValue()
    {
        // arrange
        var type = new LocalTimeType();
        var inputValue = JsonDocument.Parse("\"08:46:14\"").RootElement;
        var expectedTime = new LocalTime(8, 46, 14);

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(expectedTime, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_Invalid_Format()
    {
        // arrange
        var type = new LocalTimeType();
        var inputValue = JsonDocument.Parse("\"abc\"").RootElement;

        // act
        void Action() => type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Theory]
    [MemberData(nameof(ValidOutput))]
    public void CoerceOutputValue_Valid(byte precision, LocalTime time, string result)
    {
        // arrange
        var type = new LocalTimeType(new DateTimeOptions { OutputPrecision = precision });

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(time, resultValue);

        // assert
        resultValue.MatchInlineSnapshot($"\"{result}\"");
    }

    [Fact]
    public void CoerceOutputValue()
    {
        // arrange
        var type = new LocalTimeType();
        var localTime = new LocalTime(8, 46, 14);

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(localTime, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("\"08:46:14\"");
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Format()
    {
        // arrange
        var type = new LocalTimeType();

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        void Action() => type.CoerceOutputValue(123, resultValue);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void ValueToLiteral()
    {
        // arrange
        var type = new LocalTimeType();
        var localTime = new LocalTime(8, 46, 14);
        const string expectedLiteralValue = "08:46:14";

        // act
        var stringLiteral = type.ValueToLiteral(localTime);

        // assert
        Assert.Equal(expectedLiteralValue, Assert.IsType<StringValueNode>(stringLiteral).Value);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new LocalTimeType();
        var literal = new StringValueNode("08:46:14");
        var expectedLocalTime = new LocalTime(8, 46, 14);

        // act
        var localTime = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedLocalTime, Assert.IsType<LocalTime>(localTime));
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new LocalTimeType();

        // act
        void Action() => type.CoerceInputLiteral(new IntValueNode(123));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Theory]
    [InlineData(0, @"^\d{2}:\d{2}:\d{2}$")]
    [InlineData(1, @"^\d{2}:\d{2}:\d{2}(?:\.\d{1,1})?$")]
    [InlineData(2, @"^\d{2}:\d{2}:\d{2}(?:\.\d{1,2})?$")]
    [InlineData(3, @"^\d{2}:\d{2}:\d{2}(?:\.\d{1,3})?$")]
    [InlineData(4, @"^\d{2}:\d{2}:\d{2}(?:\.\d{1,4})?$")]
    [InlineData(5, @"^\d{2}:\d{2}:\d{2}(?:\.\d{1,5})?$")]
    [InlineData(6, @"^\d{2}:\d{2}:\d{2}(?:\.\d{1,6})?$")]
    [InlineData(7, @"^\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?$")]
    [InlineData(8, @"^\d{2}:\d{2}:\d{2}(?:\.\d{1,8})?$")]
    [InlineData(9, @"^\d{2}:\d{2}:\d{2}(?:\.\d{1,9})?$")]
    public void Pattern_Should_Match_InputPrecision(byte precision, string expectedPattern)
    {
        // arrange & act
        var type = new LocalTimeType(new DateTimeOptions { InputPrecision = precision });

        // assert
        Assert.Equal(expectedPattern, type.Pattern);
    }

    public static TheoryData<byte, string, LocalTime> ValidInput()
    {
        return new TheoryData<byte, string, LocalTime>
        {
            // https://scalars.graphql.org/chillicream/local-time.html#sec-Input-spec.Examples (Valid input values)
            {
                DateTimeOptions.DefaultInputPrecision,
                "09:00:00",
                new LocalTime(9, 0, 0)
            },
            {
                DateTimeOptions.DefaultInputPrecision,
                "07:30:00.123456789",
                new LocalTime(7, 30, 0, 123).PlusNanoseconds(456_789)
            }
        };
    }

    public static TheoryData<byte, string> InvalidInput()
    {
        return new TheoryData<byte, string>
        {
            // https://scalars.graphql.org/chillicream/local-time.html#sec-Input-spec.Examples (Invalid input values)
            // Contains time zone indicator Z.
            { DateTimeOptions.DefaultInputPrecision, "15:30:00Z" },
            // Contains time zone offset.
            { DateTimeOptions.DefaultInputPrecision, "15:30:00+05:30" },
            // Contains date component.
            { DateTimeOptions.DefaultInputPrecision, "2023-12-24T15:30:00" },
            // Missing seconds component.
            { DateTimeOptions.DefaultInputPrecision, "15:30" },
            // Invalid hour (24).
            { DateTimeOptions.DefaultInputPrecision, "24:00:00" },
            // Invalid minute (60).
            { DateTimeOptions.DefaultInputPrecision, "15:60:00" },
            // More than 9 fractional second digits.
            { DateTimeOptions.DefaultInputPrecision, "15:30:00.1234567890" },
            // Additional cases.
            // More than 8 fractional second digits with precision set to 8.
            { 8, "15:30:00.123456789" },
            // More than 7 fractional second digits with precision set to 7.
            { 7, "15:30:00.12345678" },
            // More than 6 fractional second digits with precision set to 6.
            { 6, "15:30:00.1234567" },
            // More than 5 fractional second digits with precision set to 5.
            { 5, "15:30:00.123456" },
            // More than 4 fractional second digits with precision set to 4.
            { 4, "15:30:00.12345" },
            // More than 3 fractional second digits with precision set to 3.
            { 3, "15:30:00.1234" },
            // More than 2 fractional second digits with precision set to 2.
            { 2, "15:30:00.123" },
            // More than 1 fractional second digit with precision set to 1.
            { 1, "15:30:00.12" },
            // Fractional second digits with precision set to 0.
            { 0, "15:30:00.1" }
        };
    }

    public static TheoryData<byte, LocalTime, string> ValidOutput()
    {
        return new TheoryData<byte, LocalTime, string>
        {
            // Up to 9 fractional second digits with default precision.
            {
                DateTimeOptions.DefaultOutputPrecision,
                new LocalTime(15, 30, 0, 123).PlusNanoseconds(456_789),
                "15:30:00.123456789"
            },
            // Up to 8 fractional second digits with precision set to 8.
            {
                8,
                new LocalTime(15, 30, 0, 123).PlusNanoseconds(456_789),
                "15:30:00.12345678"
            },
            // Up to 7 fractional second digits with precision set to 7.
            {
                7,
                new LocalTime(15, 30, 0, 123).PlusNanoseconds(456_789),
                "15:30:00.1234567"
            },
            // Up to 6 fractional second digits with precision set to 6.
            {
                6,
                new LocalTime(15, 30, 0, 123).PlusNanoseconds(456_789),
                "15:30:00.123456"
            },
            // Up to 5 fractional second digits with precision set to 5.
            {
                5,
                new LocalTime(15, 30, 0, 123).PlusNanoseconds(456_789),
                "15:30:00.12345"
            },
            // Up to 4 fractional second digits with precision set to 4.
            {
                4,
                new LocalTime(15, 30, 0, 123).PlusNanoseconds(456_789),
                "15:30:00.1234"
            },
            // Up to 3 fractional second digits with precision set to 3.
            {
                3,
                new LocalTime(15, 30, 0, 123).PlusNanoseconds(456_789),
                "15:30:00.123"
            },
            // Up to 2 fractional second digits with precision set to 2.
            {
                2,
                new LocalTime(15, 30, 0, 123).PlusNanoseconds(456_789),
                "15:30:00.12"
            },
            // Up to 1 fractional second digit with precision set to 1.
            {
                1,
                new LocalTime(15, 30, 0, 123).PlusNanoseconds(456_789),
                "15:30:00.1"
            },
            // No fractional second digits with precision set to 0.
            {
                0,
                new LocalTime(15, 30, 0, 123).PlusNanoseconds(456_789),
                "15:30:00"
            }
        };
    }
}
