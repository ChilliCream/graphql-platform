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
    [MemberData(nameof(ValidLocalTimeScalarStrings))]
    public void CoerceInputLiteral_Valid(string time, TimeOnly result)
    {
        // arrange
        var type = new LocalTimeType();
        var literal = new StringValueNode(time);

        // act
        var timeOnly = (TimeOnly?)type.CoerceInputLiteral(literal);

        // assert
        Assert.Equal(result, timeOnly);
    }

    [Theory]
    [MemberData(nameof(InvalidLocalTimeScalarStrings))]
    public void CoerceInputLiteral_Invalid(string time)
    {
        // arrange
        var type = new LocalTimeType();
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

    public static TheoryData<string, TimeOnly> ValidLocalTimeScalarStrings()
    {
        return new TheoryData<string, TimeOnly>
        {
            // https://scalars.graphql.org/chillicream/local-time.html#sec-Input-spec.Examples (Valid input values)
            {
                "09:00:00",
                new TimeOnly(9, 0, 0)
            },
            {
                "07:30:00.500",
                new TimeOnly(7, 30, 0, 500)
            }
        };
    }

    public static TheoryData<string> InvalidLocalTimeScalarStrings()
    {
        return
        [
            // https://scalars.graphql.org/chillicream/local-time.html#sec-Input-spec.Examples (Invalid input values)
            // Contains time zone indicator Z.
            "15:30:00Z",
            // Contains time zone offset.
            "15:30:00+05:30",
            // Contains date component.
            "2023-12-24T15:30:00",
            // Missing seconds component.
            "15:30",
            // Invalid hour (24).
            "24:00:00",
            // Invalid minute (60).
            "15:60:00",
            // More than 9 fractional second digits.
            "15:30:00.1234567890"
        ];
    }
}
