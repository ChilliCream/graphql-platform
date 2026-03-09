using System.Globalization;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Tests;
using HotChocolate.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

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
        var expectedDateOnly = new DateOnly(2018, 6, 29);

        // act
        var dateOnly = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedDateOnly, dateOnly);
    }

    [Theory]
    [MemberData(nameof(ValidLocalDateScalarStrings))]
    public void CoerceInputLiteral_Valid_Formats(string dateTime, DateOnly result)
    {
        // arrange
        var type = new LocalDateType();
        var literal = new StringValueNode(dateTime);

        // act
        var dateOnly = (DateOnly?)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(result, dateOnly);
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
        var expectedDateOnly = new DateOnly(2018, 6, 29);

        // act
        var dateOnly = (DateOnly)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedDateOnly, dateOnly);
    }

    [Theory]
    [MemberData(nameof(InvalidLocalDateScalarStrings))]
    public void CoerceInputLiteral_Invalid_Format(string dateTime)
    {
        // arrange
        var type = new LocalDateType();
        var literal = new StringValueNode(dateTime);

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
        var expectedDate = new DateOnly(2018, 6, 11);

        // act
        var runtimeValue = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Equal(expectedDate, runtimeValue);
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
        var dateOnly = new DateOnly(2018, 6, 11);

        // act
        var operation = CommonTestExtensions.CreateOperation();
        var resultDocument = new ResultDocument(operation, 0);
        var resultValue = resultDocument.Data.GetProperty("first");
        type.CoerceOutputValue(dateOnly, resultValue);

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
        var dateOnly = new DateOnly(2018, 6, 11);
        const string expectedLiteralValue = "2018-06-11";

        // act
        var stringLiteral = type.ValueToLiteral(dateOnly);

        // assert
        Assert.Equal(expectedLiteralValue, Assert.IsType<StringValueNode>(stringLiteral).Value);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new LocalDateType();
        var literal = new StringValueNode("2018-06-29");
        var expectedDateOnly = new DateOnly(2018, 6, 29);

        // act
        var dateOnly = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedDateOnly, Assert.IsType<DateOnly>(dateOnly));
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

    [Fact]
    public void LocalDateType_Binds_Only_Explicitly()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddType(new LocalDateType())
            .Create();

        // assert
        IType localDateType = schema.QueryType.Fields["dateField"].Type;
        IType dateTimeType = schema.QueryType.Fields["dateTimeField"].Type;

        Assert.IsType<LocalDateType>(localDateType);
        Assert.IsType<DateTimeType>(dateTimeType);
    }

    [Fact]
    public async Task DateOnly_As_Argument_Schema()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime1>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task DateOnly_As_Argument()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime1>()
            .ExecuteRequestAsync(
                """
                {
                    foo {
                        date(date: "2017-12-30")
                    }
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task DateOnly_As_ReturnValue_Schema()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime2>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task DateOnly_As_ReturnValue()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDateTime2>()
            .ExecuteRequestAsync(
                """
                {
                    bar {
                        date
                    }
                }
                """)
            .MatchSnapshotAsync();
    }

    public class Query
    {
        [GraphQLType(typeof(LocalDateType))]
        public DateOnly? DateField => new();

        public DateTime? DateTimeField => new();
    }

    public class QueryDateTime1
    {
        public Foo Foo => new();
    }

    public class Foo
    {
        public DateOnly GetDate(DateOnly date) => date;
    }

    public class QueryDateTime2
    {
        public Bar Bar => new();
    }

    public class Bar
    {
        public DateOnly GetDate() => DateOnly.MaxValue;
    }

    public static TheoryData<string, DateOnly> ValidLocalDateScalarStrings()
    {
        return new TheoryData<string, DateOnly>
        {
            // https://scalars.graphql.org/chillicream/local-date.html#sec-Input-spec.Examples (Valid input values)
            {
                "2000-12-24",
                new DateOnly(2000, 12, 24)
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
