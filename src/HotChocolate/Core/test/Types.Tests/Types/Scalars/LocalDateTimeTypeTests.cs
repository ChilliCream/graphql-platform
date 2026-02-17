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
    [MemberData(nameof(ValidLocalDateTimeScalarStrings))]
    public void CoerceInputLiteral_Valid(string dateTimeString, DateTime result)
    {
        // arrange
        var type = new LocalDateTimeType();
        var literal = new StringValueNode(dateTimeString);

        // act
        var dateTime = (DateTime?)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(result, dateTime);
    }

    [Theory]
    [MemberData(nameof(InvalidLocalDateTimeScalarStrings))]
    public void CoerceInputLiteral_Invalid(string dateTime)
    {
        // arrange
        var type = new LocalDateTimeType();
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

    public static TheoryData<string, DateTime> ValidLocalDateTimeScalarStrings()
    {
        return new TheoryData<string, DateTime>
        {
            // https://scalars.graphql.org/chillicream/local-date-time.html#sec-Input-spec.Examples (Valid input values)
            {
                "2023-12-24T15:30:00",
                new DateTime(2023, 12, 24, 15, 30, 0, 0)
            },
            {
                "2023-12-24t15:30:00.123456789", // Rounded to ".1234568".
                new DateTime(2023, 12, 24, 15, 30, 0, 123, 456).AddTicks(8)
            }
        };
    }

    public static TheoryData<string> InvalidLocalDateTimeScalarStrings()
    {
        return
        [
            // https://scalars.graphql.org/chillicream/local-date-time.html#sec-Input-spec.Examples (Invalid input values)
            // Contains time zone indicator Z.
            "2023-12-24T15:30:00Z",
            // Contains time zone offset.
            "2023-12-24T15:30:00+05:30",
            // Invalid separator (space instead of T or t).
            "2023-12-24 15:30:00",
            // Invalid hour (25).
            "2023-12-24T25:00:00",
            // Invalid minute (60).
            "2023-12-24T15:60:00",
            // ReSharper disable once GrammarMistakeInComment
            // Invalid date (February 30th).
            "2023-02-30T15:30:00",
            // More than 9 fractional second digits.
            "2023-12-24T15:30:00.1234567890"
        ];
    }
}
