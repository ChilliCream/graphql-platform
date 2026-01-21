using System.Globalization;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public sealed class YearMonthTypeIntegrationTests
{
    private static class Schema
    {
        public class Query
        {
            public YearMonth One => new(2025, 09);
        }

        public class Mutation
        {
            public YearMonth Test(YearMonth arg)
            {
                return arg.PlusMonths(3);
            }
        }
    }

    private readonly IRequestExecutor _testExecutor =
        SchemaBuilder.New()
            .AddQueryType<Schema.Query>()
            .AddMutationType<Schema.Mutation>()
            .AddNodaTime()
            .Create()
            .MakeExecutable();

    [Fact]
    public void QueryReturns()
    {
        var result = _testExecutor.Execute("query { test: one }");

        Assert.Equal("2025-09", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: YearMonth!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { { "arg", "2025-09" } })
                .Build());

        Assert.Equal("2025-12", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseAnIncorrectVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: YearMonth!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { { "arg", "2025-09-03" } })
                .Build());

        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
    }

    [Fact]
    public void ParsesLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"2025-09\") }")
                .Build());

        Assert.Equal("2025-12", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseIncorrectLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"2025-09-03\") }")
                .Build());

        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
        Assert.Null(result.ExpectOperationResult().Errors![0].Code);
        Assert.Equal(
            "Unable to deserialize string to YearMonth",
            result.ExpectOperationResult().Errors![0].Message);
    }

    [Fact]
    public void PatternEmpty_ThrowSchemaException()
    {
        Assert.Throws<SchemaException>(() => new YearMonthType([]));
    }

    [Fact]
    public void YearMonthType_DescriptionKnownPatterns_MatchesSnapshot()
    {
        var yearMonthType = new YearMonthType(YearMonthPattern.Iso);

        yearMonthType.Description.MatchInlineSnapshot(
            """
            YearMonth represents a month within the calendar, with no reference to a particular time zone, date, or time.

            Allowed patterns:
            - `YYYY-MM`

            Examples:
            - `2000-01`
            """);
    }

    [Fact]
    public void YearMonthType_DescriptionUnknownPatterns_MatchesSnapshot()
    {
        var yearMonthType = new YearMonthType(YearMonthPattern.Create("MM", CultureInfo.InvariantCulture));

        yearMonthType.Description.MatchInlineSnapshot(
            "YearMonth represents a month within the calendar, with no reference to a particular time zone, date, or time.");
    }
}
