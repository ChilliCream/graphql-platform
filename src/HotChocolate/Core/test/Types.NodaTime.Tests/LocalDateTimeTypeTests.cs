using System.Globalization;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace HotChocolate.Types.NodaTime;

public sealed class LocalDateTimeTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new LocalDateTimeType();

        // assert
        Assert.Equal("LocalDateTime", type.Name);
    }

    [Fact]
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new LocalDateTimeType();
        var literal = new StringValueNode("2018-06-29T08:46:14");
        var expectedLocalDateTime = new LocalDateTime(2018, 6, 29, 8, 46, 14);

        // act
        var localDateTime = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedLocalDateTime, localDateTime);
    }

    [Theory]
    [MemberData(nameof(ValidInput))]
    public void CoerceInputLiteral_Valid(byte precision, string dateTimeString, LocalDateTime result)
    {
        // arrange
        var type = new LocalDateTimeType(new DateTimeOptions { InputPrecision = precision });
        var literal = new StringValueNode(dateTimeString);

        // act
        var localDateTime = (LocalDateTime?)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(result, localDateTime);
    }

    [Theory]
    [MemberData(nameof(InvalidInput))]
    public void CoerceInputLiteral_Invalid(byte precision, string dateTime)
    {
        // arrange
        var type = new LocalDateTimeType(new DateTimeOptions { InputPrecision = precision });
        var literal = new StringValueNode(dateTime);

        // act
        void Action() => type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(
            "LocalDateTime cannot coerce the given literal of type `StringValue` to a runtime value.",
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

        var type = new LocalDateTimeType();
        var literal = new StringValueNode("2018-06-29T08:46:14");
        var expectedLocalDateTime = new LocalDateTime(2018, 6, 29, 8, 46, 14);

        // act
        var localDateTime = (LocalDateTime)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedLocalDateTime, localDateTime);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Format()
    {
        // arrange
        var type = new LocalDateTimeType();
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
        var type = new LocalDateTimeType();
        var inputValue = JsonDocument.Parse("\"2018-06-11T08:46:14\"").RootElement;
        var expectedLocalDateTime = new LocalDateTime(2018, 6, 11, 8, 46, 14);

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(expectedLocalDateTime, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_Invalid_Format()
    {
        // arrange
        var type = new LocalDateTimeType();
        var inputValue = JsonDocument.Parse("\"abc\"").RootElement;

        // act
        void Action() => type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Theory]
    [MemberData(nameof(ValidOutput))]
    public void CoerceOutputValue_Valid(byte precision, LocalDateTime localDateTime, string result)
    {
        // arrange
        var type = new LocalDateTimeType(new DateTimeOptions { OutputPrecision = precision });

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(localDateTime, resultValue);

        // assert
        resultValue.MatchInlineSnapshot($"\"{result}\"");
    }

    [Fact]
    public void CoerceOutputValue()
    {
        // arrange
        var type = new LocalDateTimeType();
        var localDateTime = new LocalDateTime(2018, 6, 11, 8, 46, 14);

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(localDateTime, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("\"2018-06-11T08:46:14\"");
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Format()
    {
        // arrange
        var type = new LocalDateTimeType();

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
        var type = new LocalDateTimeType();
        var localDateTime = new LocalDateTime(2018, 6, 11, 8, 46, 14);
        const string expectedLiteralValue = "2018-06-11T08:46:14";

        // act
        var stringLiteral = type.ValueToLiteral(localDateTime);

        // assert
        Assert.Equal(expectedLiteralValue, Assert.IsType<StringValueNode>(stringLiteral).Value);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new LocalDateTimeType();
        var literal = new StringValueNode("2018-06-29T08:46:14");
        var expectedLocalDateTime = new LocalDateTime(2018, 6, 29, 8, 46, 14);

        // act
        var localDateTime = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedLocalDateTime, Assert.IsType<LocalDateTime>(localDateTime));
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new LocalDateTimeType();

        // act
        void Action() => type.CoerceInputLiteral(new IntValueNode(123));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Theory]
    [InlineData(0, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}$")]
    [InlineData(1, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,1})?$")]
    [InlineData(2, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,2})?$")]
    [InlineData(3, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,3})?$")]
    [InlineData(4, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,4})?$")]
    [InlineData(5, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,5})?$")]
    [InlineData(6, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,6})?$")]
    [InlineData(7, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?$")]
    [InlineData(8, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,8})?$")]
    [InlineData(9, @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,9})?$")]
    public void Pattern_Should_Match_InputPrecision(byte precision, string expectedPattern)
    {
        // arrange & act
        var type = new LocalDateTimeType(new DateTimeOptions { InputPrecision = precision });

        // assert
        Assert.Equal(expectedPattern, type.Pattern);
    }

    [Fact]
    public async Task Integration_SingleRuntimeType()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(b => b.Name(OperationTypeNames.Query))
            .AddType(typeof(QuerySingleRuntimeType))
            .AddNodaTime()
            .BuildRequestExecutorAsync();

        // act
        var result =
            await executor.ExecuteAsync(
                """{ localDateTime(input: "9999-12-31T23:59:59.999999999") }""");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "localDateTime": "9999-12-31T23:59:59.999999999"
              }
            }
            """);
    }

    [Fact]
    public async Task Integration_TwoRuntimeTypes()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(b => b.Name(OperationTypeNames.Query))
            .AddType(typeof(QueryTwoRuntimeTypes))
            .AddNodaTime(bindBclTypes: true)
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                localDateTime1(input: "9999-12-31T23:59:59.999999999")
                localDateTime2(input: "9999-12-31T23:59:59.999999999")
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "localDateTime1": "9999-12-31T23:59:59.9999999",
                "localDateTime2": "9999-12-31T23:59:59.999999999"
              }
            }
            """);
    }

    [QueryType]
    private static class QuerySingleRuntimeType
    {
        public static LocalDateTime GetLocalDateTime(LocalDateTime input) => input;
    }

    [QueryType]
    private static class QueryTwoRuntimeTypes
    {
        public static DateTime GetLocalDateTime1(DateTime input) => input;

        public static LocalDateTime GetLocalDateTime2(LocalDateTime input) => input;
    }

    public static TheoryData<byte, string, LocalDateTime> ValidInput()
    {
        return new TheoryData<byte, string, LocalDateTime>
        {
            // https://scalars.graphql.org/chillicream/local-date-time.html#sec-Input-spec.Examples (Valid input values)
            {
                DateTimeOptions.DefaultInputPrecision,
                "2023-12-24T15:30:00",
                new LocalDateTime(2023, 12, 24, 15, 30, 0, 0)
            },
            {
                DateTimeOptions.DefaultInputPrecision,
                "2023-12-24t15:30:00.123456789",
                new LocalDateTime(2023, 12, 24, 15, 30, 0, 123).PlusNanoseconds(456_789)
            },
            // Additional cases.
            // Lowercase t separator without fractional seconds.
            {
                DateTimeOptions.DefaultInputPrecision,
                "2023-12-24t15:30:00",
                new LocalDateTime(2023, 12, 24, 15, 30, 0, 0)
            }
        };
    }

    public static TheoryData<byte, string> InvalidInput()
    {
        return new TheoryData<byte, string>
        {
            // https://scalars.graphql.org/chillicream/local-date-time.html#sec-Input-spec.Examples (Invalid input values)
            // Contains time zone indicator Z.
            { DateTimeOptions.DefaultInputPrecision, "2023-12-24T15:30:00Z" },
            // Contains time zone offset.
            { DateTimeOptions.DefaultInputPrecision, "2023-12-24T15:30:00+05:30" },
            // Invalid separator (space instead of T or t).
            { DateTimeOptions.DefaultInputPrecision, "2023-12-24 15:30:00" },
            // Invalid hour (25).
            { DateTimeOptions.DefaultInputPrecision, "2023-12-24T25:00:00" },
            // Invalid minute (60).
            { DateTimeOptions.DefaultInputPrecision, "2023-12-24T15:60:00" },
            // ReSharper disable once GrammarMistakeInComment
            // Invalid date (February 30th).
            { DateTimeOptions.DefaultInputPrecision, "2023-02-30T15:30:00" },
            // More than 9 fractional second digits.
            { DateTimeOptions.DefaultInputPrecision, "2023-12-24T15:30:00.1234567890" },
            // Additional cases.
            // More than 8 fractional second digits with precision set to 8.
            { 8, "2023-12-24T15:30:00.123456789" },
            // More than 7 fractional second digits with precision set to 7.
            { 7, "2023-12-24T15:30:00.12345678" },
            // More than 6 fractional second digits with precision set to 6.
            { 6, "2023-12-24T15:30:00.1234567" },
            // More than 5 fractional second digits with precision set to 5.
            { 5, "2023-12-24T15:30:00.123456" },
            // More than 4 fractional second digits with precision set to 4.
            { 4, "2023-12-24T15:30:00.12345" },
            // More than 3 fractional second digits with precision set to 3.
            { 3, "2023-12-24T15:30:00.1234" },
            // More than 2 fractional second digits with precision set to 2.
            { 2, "2023-12-24T15:30:00.123" },
            // More than 1 fractional second digit with precision set to 1.
            { 1, "2023-12-24T15:30:00.12" },
            // Fractional second digits with precision set to 0.
            { 0, "2023-12-24T15:30:00.1" }
        };
    }

    public static TheoryData<byte, LocalDateTime, string> ValidOutput()
    {
        return new TheoryData<byte, LocalDateTime, string>
        {
            // Up to 9 fractional second digits with default precision.
            {
                DateTimeOptions.DefaultOutputPrecision,
                new LocalDateTime(2023, 12, 24, 15, 30, 0, 123).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00.123456789"
            },
            // Up to 8 fractional second digits with precision set to 8.
            {
                8,
                new LocalDateTime(2023, 12, 24, 15, 30, 0, 123).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00.12345678"
            },
            // Up to 7 fractional second digits with precision set to 7.
            {
                7,
                new LocalDateTime(2023, 12, 24, 15, 30, 0, 123).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00.1234567"
            },
            // Up to 6 fractional second digits with precision set to 6.
            {
                6,
                new LocalDateTime(2023, 12, 24, 15, 30, 0, 123).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00.123456"
            },
            // Up to 5 fractional second digits with precision set to 5.
            {
                5,
                new LocalDateTime(2023, 12, 24, 15, 30, 0, 123).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00.12345"
            },
            // Up to 4 fractional second digits with precision set to 4.
            {
                4,
                new LocalDateTime(2023, 12, 24, 15, 30, 0, 123).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00.1234"
            },
            // Up to 3 fractional second digits with precision set to 3.
            {
                3,
                new LocalDateTime(2023, 12, 24, 15, 30, 0, 123).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00.123"
            },
            // Up to 2 fractional second digits with precision set to 2.
            {
                2,
                new LocalDateTime(2023, 12, 24, 15, 30, 0, 123).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00.12"
            },
            // Up to 1 fractional second digit with precision set to 1.
            {
                1,
                new LocalDateTime(2023, 12, 24, 15, 30, 0, 123).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00.1"
            },
            // No fractional second digits with precision set to 0.
            {
                0,
                new LocalDateTime(2023, 12, 24, 15, 30, 0, 123).PlusNanoseconds(456_789),
                "2023-12-24T15:30:00"
            }
        };
    }
}
