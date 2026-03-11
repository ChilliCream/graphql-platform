using System.Globalization;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using NodaTime;

namespace HotChocolate.Types.NodaTime.Tests;

public class LocalDateTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new LocalDateType();

        // assert
        Assert.Equal("LocalDate", type.Name);
    }

    [Fact]
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new LocalDateType();
        var literal = new StringValueNode("2018-06-29");
        var expectedLocalDate = new LocalDate(2018, 6, 29);

        // act
        var localDate = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedLocalDate, localDate);
    }

    [Theory]
    [MemberData(nameof(ValidLocalDateScalarStrings))]
    public void CoerceInputLiteral_Valid_Formats(string dateTime, LocalDate result)
    {
        // arrange
        var type = new LocalDateType();
        var literal = new StringValueNode(dateTime);

        // act
        var localDate = (LocalDate?)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(result, localDate);
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

        var type = new LocalDateType();
        var literal = new StringValueNode("2018-06-29");
        var expectedLocalDate = new LocalDate(2018, 6, 29);

        // act
        var localDate = (LocalDate)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedLocalDate, localDate);
    }

    [Theory]
    [MemberData(nameof(InvalidLocalDateScalarStrings))]
    public void CoerceInputLiteral_Invalid_Format(string localDate)
    {
        // arrange
        var type = new LocalDateType();
        var literal = new StringValueNode(localDate);

        // act
        void Action() => type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(
            "LocalDate cannot coerce the given literal of type `StringValue` to a runtime value.",
            Assert.Throws<LeafCoercionException>(Action).Message);
    }

    [Fact]
    public void CoerceInputValue()
    {
        // arrange
        var type = new LocalDateType();
        var inputValue = JsonDocument.Parse("\"2018-06-11\"").RootElement;
        var expectedLocalDate = new LocalDate(2018, 6, 11);

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(expectedLocalDate, runtimeValue);
    }

    [Fact]
    public void CoerceInputValue_Invalid_Format()
    {
        // arrange
        var type = new LocalDateType();
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
        var type = new LocalDateType();
        var localDate = new LocalDate(2018, 6, 11);

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(localDate, resultValue);

        // assert
        resultValue.MatchInlineSnapshot("\"2018-06-11\"");
    }

    [Fact]
    public void CoerceOutputValue_Invalid_Format()
    {
        // arrange
        var type = new LocalDateType();

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
        var type = new LocalDateType();
        var localDate = new LocalDate(2018, 6, 11);
        const string expectedLiteralValue = "2018-06-11";

        // act
        var stringLiteral = type.ValueToLiteral(localDate);

        // assert
        Assert.Equal(expectedLiteralValue, Assert.IsType<StringValueNode>(stringLiteral).Value);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new LocalDateType();
        var literal = new StringValueNode("2018-06-29");
        var expectedLocalDate = new LocalDate(2018, 6, 29);

        // act
        var dateOnly = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedLocalDate, Assert.IsType<LocalDate>(dateOnly));
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new LocalDateType();

        // act
        void Action() => type.CoerceInputLiteral(new IntValueNode(123));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    public static TheoryData<string, LocalDate> ValidLocalDateScalarStrings()
    {
        return new TheoryData<string, LocalDate>
        {
            // https://scalars.graphql.org/chillicream/local-date.html#sec-Input-spec.Examples (Valid input values)
            {
                "2000-12-24",
                new LocalDate(2000, 12, 24)
            }
        };
    }

    public static TheoryData<string> InvalidLocalDateScalarStrings()
    {
        return
        [
            // https://scalars.graphql.org/chillicream/local-date.html#sec-Input-spec.Examples (Invalid input values)
            // Contains time component.
            "2023-12-24T15:30:00",
            // Invalid month (13).
            "2023-13-01",
            // Invalid day (32).
            "2023-12-32",
            // Month and day must be zero-padded.
            "2023-2-5",
            // Invalid separator.
            "2023/12/24",
            // ReSharper disable once GrammarMistakeInComment
            // Invalid date (February 30th).
            "2023-02-30"
        ];
    }
}
