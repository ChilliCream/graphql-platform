using System.Globalization;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Tests;
using HotChocolate.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

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
        var expectedTimeOnly = new TimeOnly(8, 46, 14);

        // act
        var timeOnly = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedTimeOnly, timeOnly);
    }

    [Theory]
    [MemberData(nameof(ValidInput))]
    public void CoerceInputLiteral_Valid(byte precision, string time, TimeOnly result)
    {
        // arrange
        var type = new LocalTimeType(new DateTimeOptions { InputPrecision = precision });
        var literal = new StringValueNode(time);

        // act
        var timeOnly = (TimeOnly?)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(result, timeOnly);
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
        var expectedTimeOnly = new TimeOnly(8, 46, 14);

        // act
        var timeOnly = (TimeOnly)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedTimeOnly, timeOnly);
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
        var expectedTime = new TimeOnly(8, 46, 14);

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
    public void CoerceOutputValue_Valid(byte precision, TimeOnly time, string result)
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
        var timeOnly = new TimeOnly(8, 46, 14);

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(timeOnly, resultValue);

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
        var timeOnly = new TimeOnly(8, 46, 14);
        const string expectedLiteralValue = "08:46:14";

        // act
        var stringLiteral = type.ValueToLiteral(timeOnly);

        // assert
        Assert.Equal(expectedLiteralValue, Assert.IsType<StringValueNode>(stringLiteral).Value);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new LocalTimeType();
        var literal = new StringValueNode("08:46:14");
        var expectedTimeOnly = new TimeOnly(8, 46, 14);

        // act
        var timeOnly = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedTimeOnly, Assert.IsType<TimeOnly>(timeOnly));
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

    [Fact]
    public void LocalTimeType_Binds_Only_Explicitly()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddType(new LocalTimeType())
            .Create();

        // assert
        IType localTimeType = schema.QueryType.Fields["timeField"].Type;
        IType dateTimeType = schema.QueryType.Fields["dateTimeField"].Type;

        Assert.IsType<LocalTimeType>(localTimeType);
        Assert.IsType<DateTimeType>(dateTimeType);
    }

    [Fact]
    public async Task TimeOnly_As_Argument_Schema()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime1>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task TimeOnly_As_Argument()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime1>()
            .ExecuteRequestAsync(
                """
                {
                    foo {
                        time(time: "11:22:00")
                    }
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task TimeOnly_As_ReturnValue_Schema()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime2>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task TimeOnly_As_ReturnValue()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime2>()
            .ExecuteRequestAsync(
                """
                {
                    bar {
                        time
                    }
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public void LocalTime_Relaxed_Format_Check()
    {
        // arrange
        const string s = "15:30";

        // act
        var type = new LocalTimeType(disableFormatCheck: true);
        var inputValue = JsonDocument.Parse($"\"{s}\"").RootElement;
        var result = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.IsType<TimeOnly>(result);
    }

    public class Query
    {
        [GraphQLType(typeof(LocalTimeType))]
        public TimeOnly? TimeField => new();

        public DateTime? DateTimeField => new();
    }

    public class QueryDateTime1
    {
        public Foo Foo => new();
    }

    public class Foo
    {
        public TimeOnly GetTime(TimeOnly time) => time;
    }

    public class QueryDateTime2
    {
        public Bar Bar => new();
    }

    public class Bar
    {
        public TimeOnly GetTime() => TimeOnly.MaxValue;
    }

    public static TheoryData<byte, string, TimeOnly> ValidInput()
    {
        return new TheoryData<byte, string, TimeOnly>
        {
            // https://scalars.graphql.org/chillicream/local-time.html#sec-Input-spec.Examples (Valid input values)
            {
                DateTimeOptions.DefaultInputPrecision,
                "09:00:00",
                new TimeOnly(9, 0, 0)
            },
            {
                DateTimeOptions.DefaultInputPrecision,
                "07:30:00.500",
                new TimeOnly(7, 30, 0, 500)
            },
            // Additional cases.
            // Up to 7 fractional second digits.
            {
                DateTimeOptions.DefaultInputPrecision,
                "07:30:00.1234567",
                new TimeOnly(7, 30, 0, 123, 456).Add(TimeSpan.FromTicks(7))
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
            // More than 7 fractional second digits with default precision.
            { DateTimeOptions.DefaultInputPrecision, "15:30:00.12345678" },
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

    public static TheoryData<byte, TimeOnly, string> ValidOutput()
    {
        return new TheoryData<byte, TimeOnly, string>
        {
            // Up to 7 fractional second digits with default precision.
            {
                DateTimeOptions.DefaultOutputPrecision,
                new TimeOnly(15, 30, 0, 123, 456).Add(TimeSpan.FromTicks(7)),
                "15:30:00.1234567"
            },
            // Up to 6 fractional second digits with precision set to 6.
            {
                6,
                new TimeOnly(15, 30, 0, 123, 456).Add(TimeSpan.FromTicks(7)),
                "15:30:00.123456"
            },
            // Up to 5 fractional second digits with precision set to 5.
            {
                5,
                new TimeOnly(15, 30, 0, 123, 456).Add(TimeSpan.FromTicks(7)),
                "15:30:00.12345"
            },
            // Up to 4 fractional second digits with precision set to 4.
            {
                4,
                new TimeOnly(15, 30, 0, 123, 456).Add(TimeSpan.FromTicks(7)),
                "15:30:00.1234"
            },
            // Up to 3 fractional second digits with precision set to 3.
            {
                3,
                new TimeOnly(15, 30, 0, 123, 456).Add(TimeSpan.FromTicks(7)),
                "15:30:00.123"
            },
            // Up to 2 fractional second digits with precision set to 2.
            {
                2,
                new TimeOnly(15, 30, 0, 123, 456).Add(TimeSpan.FromTicks(7)),
                "15:30:00.12"
            },
            // Up to 1 fractional second digit with precision set to 1.
            {
                1,
                new TimeOnly(15, 30, 0, 123, 456).Add(TimeSpan.FromTicks(7)),
                "15:30:00.1"
            },
            // No fractional second digits with precision set to 0.
            {
                0,
                new TimeOnly(15, 30, 0, 123, 456).Add(TimeSpan.FromTicks(7)),
                "15:30:00"
            }
        };
    }
}
