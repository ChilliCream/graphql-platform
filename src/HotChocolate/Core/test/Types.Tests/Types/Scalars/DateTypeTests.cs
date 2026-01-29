using System.Globalization;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Tests;
using HotChocolate.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class DateTypeTests
{
    [Fact]
    public void Ensure_Type_Name_Is_Correct()
    {
        // arrange
        // act
        var type = new DateType();

        // assert
        Assert.Equal("Date", type.Name);
    }

    [Fact]
    public void CoerceInputLiteral()
    {
        // arrange
        var type = new DateType();
        var literal = new StringValueNode("2018-06-29");
        var expectedDate = new DateOnly(2018, 6, 29);

        // act
        var date = (DateOnly)type.CoerceInputLiteral(literal)!;

        // assert
        Assert.Equal(expectedDate, date);
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

        var type = new DateType();
        var literal = new StringValueNode("2018-06-29");
        var expectedDate = new DateOnly(2018, 6, 29);

        // act
        var date = (DateOnly)type.CoerceInputLiteral(literal)!;

        // assert
        Assert.Equal(expectedDate, date);
    }

    [Fact]
    public void CoerceInputLiteral_Invalid_Format()
    {
        // arrange
        var type = new DateType();
        var literal = new StringValueNode("foo");

        // act
        void Action() => type.CoerceInputLiteral(literal);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputValue()
    {
        // arrange
        var type = new DateType();
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
        var type = new DateType();
        var inputValue = JsonDocument.Parse("\"2018/06/11\"").RootElement;

        // act
        void Action() => type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceInputValue_Invalid_String()
    {
        // arrange
        var type = new DateType();
        var inputValue = JsonDocument.Parse("\"abc\"").RootElement;

        // act
        void Action() => type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void CoerceOutputValue_DateOnly()
    {
        // arrange
        var type = new DateType();
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
    public void ValueToLiteral()
    {
        // arrange
        var type = new DateType();
        var dateOnly = new DateOnly(2018, 6, 11);
        const string expectedLiteralValue = "2018-06-11";

        // act
        var stringLiteral = (StringValueNode)type.ValueToLiteral(dateOnly);

        // assert
        Assert.Equal(expectedLiteralValue, stringLiteral.Value);
    }

    [Fact]
    public void ParseLiteral()
    {
        // arrange
        var type = new DateType();
        var literal = new StringValueNode("2018-06-29");
        var expectedDate = new DateOnly(2018, 6, 29);

        // act
        var date = type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(expectedDate, Assert.IsType<DateOnly>(date));
    }

    [Fact]
    public void ParseLiteral_InvalidValue()
    {
        // arrange
        var type = new DateType();

        // act
        void Action() => type.CoerceInputLiteral(new IntValueNode(123));

        // assert
        Assert.Throws<LeafCoercionException>(Action);
    }

    [Fact]
    public void Ensure_TypeKind_Is_Scalar()
    {
        // arrange
        var type = new DateType();

        // act
        var kind = type.Kind;

        // assert
        Assert.Equal(TypeKind.Scalar, kind);
    }

    [Fact]
    public void DateType_Binds_Only_Explicitly()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddType(new DateType())
            .Create();

        // assert
        IType dateType = schema.QueryType.Fields["dateField"].Type;
        IType dateTimeType = schema.QueryType.Fields["dateTimeField"].Type;

        Assert.IsType<DateType>(dateType);
        Assert.IsType<DateTimeType>(dateTimeType);
    }

    [Fact]
    public async Task DateOnly_As_Argument_Schema()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDate1>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task DateOnly_As_Argument()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDate1>()
            .AddType(() => new TimeSpanType(TimeSpanFormat.DotNet))
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
            .AddQueryType<QueryDate2>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task DateOnly_As_ReturnValue()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryDate2>()
            .AddType(() => new TimeSpanType(TimeSpanFormat.DotNet))
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

    [Fact]
    public void DateType_Relaxed_Format_Check()
    {
        // arrange
        const string s = "2011-08-30T08:46:14.116";

        // act
        var type = new DateType(disableFormatCheck: true);
        var inputValue = JsonDocument.Parse($"\"{s}\"").RootElement;
        var result = type.CoerceInputValue(inputValue, null!);

        // assert
        Assert.IsType<DateOnly>(result);
    }

    public class Query
    {
        [GraphQLType(typeof(DateType))]
        public DateOnly? DateField => new();

        public DateTime? DateTimeField => DateTime.UtcNow;
    }

    public class QueryDate1
    {
        public Foo Foo => new();
    }

    public class Foo
    {
        [GraphQLType(typeof(DateType))]
        public DateOnly GetDate([GraphQLType(typeof(DateType))] DateOnly date) => date;
    }

    public class QueryDate2
    {
        public Bar Bar => new();
    }

    public class Bar
    {
        [GraphQLType(typeof(DateType))]
        public DateOnly GetDate() => DateOnly.MaxValue;
    }
}
