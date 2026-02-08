using System.Globalization;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Tests;
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
    [MemberData(nameof(ValidDateTimeScalarStrings))]
    public void CoerceInputLiteral_Valid(string dateTime, DateTimeOffset result)
    {
        // arrange
        var type = new DateTimeType();
        var literal = new StringValueNode(dateTime);

        // act
        var dateTimeOffset = (DateTimeOffset?)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(result, dateTimeOffset);
    }

    [Theory]
    [MemberData(nameof(InvalidDateTimeScalarStrings))]
    public void CoerceInputLiteral_Invalid(string dateTime)
    {
        // arrange
        var type = new DateTimeType();
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
        resultValue.MatchInlineSnapshot("\"2018-06-11T08:46:14.000Z\"");
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
        resultValue.MatchInlineSnapshot("\"2018-06-11T08:46:14.000+04:00\"");
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
        const string expectedLiteralValue = "2018-06-11T08:46:14.000+04:00";

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
        const string expectedLiteralValue = "2018-06-11T08:46:14.000Z";

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

    public static TheoryData<string, DateTimeOffset> ValidDateTimeScalarStrings()
    {
        return new TheoryData<string, DateTimeOffset>
        {
            // https://scalars.graphql.org/andimarek/date-time.html#sec-Overview.Examples (valid examples)
            {
                // A DateTime with UTC offset (+00:00).
                "2011-08-30T13:22:53.108Z",
                new(2011, 8, 30, 13, 22, 53, 108, TimeSpan.Zero)
            },
            {
                // A DateTime with +00:00 which is the same as UTC.
                "2011-08-30T13:22:53.108+00:00",
                new(2011, 8, 30, 13, 22, 53, 108, TimeSpan.Zero)
            },
            {
                // The z and t may be lower case.
                "2011-08-30t13:22:53.108z",
                new(2011, 8, 30, 13, 22, 53, 108, TimeSpan.Zero)
            },
            {
                // A DateTime with -3h offset.
                "2011-08-30T13:22:53.108-03:00",
                new(2011, 8, 30, 13, 22, 53, 108, new TimeSpan(-3, 0, 0))
            },
            {
                // A DateTime with +3h 30min offset.
                "2011-08-30T13:22:53.108+03:30",
                new(2011, 8, 30, 13, 22, 53, 108, new TimeSpan(3, 30, 0))
            },
            // Additional test cases.
            {
                // A DateTime with 7 fractional digits.
                "2011-08-30T13:22:53.1230000+03:30",
                new(2011, 8, 30, 13, 22, 53, 123, new TimeSpan(3, 30, 0))
            },
            {
                // A DateTime with no fractional seconds.
                "2011-08-30T13:22:53+03:30",
                new(2011, 8, 30, 13, 22, 53, 0, new TimeSpan(3, 30, 0))
            }
        };
    }

    public static TheoryData<string> InvalidDateTimeScalarStrings()
    {
        return
        [
            // https://scalars.graphql.org/andimarek/date-time.html#sec-Overview.Examples (invalid examples)
            // The minutes of the offset are missing.
            "2011-08-30T13:22:53.108-03",
            // Too many digits for fractions of a second. Exactly three expected.
            // -> We diverge from the specification here, and allow up to 7 fractional digits.
            // Fractions of a second are missing.
            // -> We diverge from the specification here, and do not require fractional seconds.
            // No offset provided.
            "2011-08-30T13:22:53.108",
            // No time provided.
            "2011-08-30",
            // Negative offset (-00:00) is not allowed.
            "2011-08-30T13:22:53.108-00:00",
            // Seconds are not allowed for the offset.
            "2011-08-30T13:22:53.108+03:30:15",
            // 24 is not allowed as hour of the time.
            "2011-08-30T24:22:53.108Z",
            // ReSharper disable once GrammarMistakeInComment
            // 30th of February is not a valid date.
            "2010-02-30T21:22:53.108Z",
            // 25 is not a valid hour for offset.
            "2010-02-11T21:22:53.108+25:11",
            // Additional test cases.
            // A DateTime with 8 fractional digits.
            "2011-08-30T13:22:53.12345678+03:30"
        ];
    }
}
