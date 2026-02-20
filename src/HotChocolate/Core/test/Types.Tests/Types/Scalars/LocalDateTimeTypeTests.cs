using System.Globalization;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Tests;
using HotChocolate.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class LocalDateTimeTypeTests
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
        var expectedDateTime = new DateTime(2018, 6, 29, 8, 46, 14);

        // act
        var dateTime = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedDateTime, dateTime);
    }

    [Theory]
    [MemberData(nameof(ValidInput))]
    public void CoerceInputLiteral_Valid(byte precision, string dateTimeString, DateTime result)
    {
        // arrange
        var type = new LocalDateTimeType(new DateTimeOptions { InputPrecision = precision });
        var literal = new StringValueNode(dateTimeString);

        // act
        var dateTime = (DateTime?)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(result, dateTime);
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
        var expectedDateTime = new DateTime(2018, 6, 29, 8, 46, 14);

        // act
        var dateTime = (DateTime)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedDateTime, dateTime);
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
        var expectedDateTime = new DateTime(2018, 6, 11, 8, 46, 14);

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(expectedDateTime, runtimeValue);
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
    public void CoerceOutputValue_Valid(byte precision, DateTime dateTime, string result)
    {
        // arrange
        var type = new LocalDateTimeType(new DateTimeOptions { OutputPrecision = precision });

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(dateTime, resultValue);

        // assert
        resultValue.MatchInlineSnapshot($"\"{result}\"");
    }

    [Fact]
    public void CoerceOutputValue()
    {
        // arrange
        var type = new LocalDateTimeType();
        var dateTime = new DateTime(2018, 6, 11, 8, 46, 14);

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(dateTime, resultValue);

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
        var dateTime = new DateTime(2018, 6, 11, 8, 46, 14);
        const string expectedLiteralValue = "2018-06-11T08:46:14";

        // act
        var stringLiteral = type.ValueToLiteral(dateTime);

        // assert
        Assert.Equal(expectedLiteralValue, Assert.IsType<StringValueNode>(stringLiteral).Value);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new LocalDateTimeType();
        var literal = new StringValueNode("2018-06-29T08:46:14");
        var expectedDateTime = new DateTime(2018, 6, 29, 8, 46, 14);

        // act
        var dateTime = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedDateTime, Assert.IsType<DateTime>(dateTime));
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

    [Fact]
    public void LocalDateTimeType_Binds_Only_Explicitly()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddType(new LocalDateTimeType())
            .Create();

        // assert
        IType localDateTimeType = schema.QueryType.Fields["localDateTimeField"].Type;
        IType dateTimeType = schema.QueryType.Fields["dateTimeField"].Type;

        Assert.IsType<LocalDateTimeType>(localDateTimeType);
        Assert.IsType<DateTimeType>(dateTimeType);
    }

    [Fact]
    public async Task LocalDateTime_As_Argument_Schema()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime1>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task LocalDateTime_As_Argument()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime1>()
            .ExecuteRequestAsync(
                """
                {
                    foo {
                        localDateTime(localDateTime: "2017-12-30T11:24:00")
                    }
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task LocalDateTime_As_ReturnValue_Schema()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime2>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task LocalDateTime_As_ReturnValue()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime2>()
            .ExecuteRequestAsync(
                """
                {
                    bar {
                        localDateTime
                    }
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public void LocalDateTime_Relaxed_Format_Check()
    {
        // arrange
        const string s = "2011-08-30";

        // act
        var type = new LocalDateTimeType(disableFormatCheck: true);
        var inputValue = JsonDocument.Parse($"\"{s}\"").RootElement;
        var result = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.IsType<DateTime>(result);
    }

    public class Query
    {
        [GraphQLType<LocalDateTimeType>]
        public DateTime? LocalDateTimeField => new();

        public DateTime? DateTimeField => new();
    }

    public class QueryDateTime1
    {
        public Foo Foo => new();
    }

    public class Foo
    {
        [GraphQLType<LocalDateTimeType>]
        public DateTime GetLocalDateTime([GraphQLType<LocalDateTimeType>] DateTime localDateTime)
            => localDateTime;
    }

    public class QueryDateTime2
    {
        public Bar Bar => new();
    }

    public class Bar
    {
        [GraphQLType<LocalDateTimeType>]
        public DateTime GetLocalDateTime() => DateTime.MaxValue;
    }

    public static TheoryData<byte, string, DateTime> ValidInput()
    {
        return new TheoryData<byte, string, DateTime>
        {
            // https://scalars.graphql.org/chillicream/local-date-time.html#sec-Input-spec.Examples (Valid input values)
            {
                DateTimeOptions.DefaultInputPrecision,
                "2023-12-24T15:30:00",
                new DateTime(2023, 12, 24, 15, 30, 0, 0)
            },
            // Additional cases.
            // Up to 7 fractional second digits.
            {
                DateTimeOptions.DefaultInputPrecision,
                "2023-12-24t15:30:00.1234567",
                new DateTime(2023, 12, 24, 15, 30, 0, 123, 456).AddTicks(7)
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
            // More than 7 fractional second digits with default precision.
            { DateTimeOptions.DefaultInputPrecision, "2023-12-24T15:30:00.12345678" },
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

    public static TheoryData<byte, DateTime, string> ValidOutput()
    {
        return new TheoryData<byte, DateTime, string>
        {
            // Up to 7 fractional second digits with default precision.
            {
                DateTimeOptions.DefaultOutputPrecision,
                new DateTime(2023, 12, 24, 15, 30, 0, 123, 456).AddTicks(7),
                "2023-12-24T15:30:00.1234567"
            },
            // Up to 6 fractional second digits with precision set to 6.
            {
                6,
                new DateTime(2023, 12, 24, 15, 30, 0, 123, 456).AddTicks(7),
                "2023-12-24T15:30:00.123456"
            },
            // Up to 5 fractional second digits with precision set to 5.
            {
                5,
                new DateTime(2023, 12, 24, 15, 30, 0, 123, 456).AddTicks(7),
                "2023-12-24T15:30:00.12345"
            },
            // Up to 4 fractional second digits with precision set to 4.
            {
                4,
                new DateTime(2023, 12, 24, 15, 30, 0, 123, 456).AddTicks(7),
                "2023-12-24T15:30:00.1234"
            },
            // Up to 3 fractional second digits with precision set to 3.
            {
                3,
                new DateTime(2023, 12, 24, 15, 30, 0, 123, 456).AddTicks(7),
                "2023-12-24T15:30:00.123"
            },
            // Up to 2 fractional second digits with precision set to 2.
            {
                2,
                new DateTime(2023, 12, 24, 15, 30, 0, 123, 456).AddTicks(7),
                "2023-12-24T15:30:00.12"
            },
            // Up to 1 fractional second digit with precision set to 1.
            {
                1,
                new DateTime(2023, 12, 24, 15, 30, 0, 123, 456).AddTicks(7),
                "2023-12-24T15:30:00.1"
            },
            // No fractional second digits with precision set to 0.
            {
                0,
                new DateTime(2023, 12, 24, 15, 30, 0, 123, 456).AddTicks(7),
                "2023-12-24T15:30:00"
            }
        };
    }
}
