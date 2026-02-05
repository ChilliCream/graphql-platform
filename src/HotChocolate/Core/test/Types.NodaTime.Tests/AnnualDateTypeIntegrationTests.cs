using System.Globalization;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public sealed class AnnualDateTypeIntegrationTests
{
    private static class Schema
    {
        public class Query
        {
            public AnnualDate One => new(09, 03);
        }

        public class Mutation
        {
            public LocalDate Test(AnnualDate arg)
            {
                return arg.InYear(2025);
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

        Assert.Equal("09-03", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: AnnualDate!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { { "arg", "09-03" } })
                .Build());

        Assert.Equal("2025-09-03", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseAnIncorrectVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: AnnualDate!) { test(arg: $arg) }")
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
                .SetDocument("mutation { test(arg: \"09-03\") }")
                .Build());

        Assert.Equal("2025-09-03", result.ExpectOperationResult().Data!["test"]);
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
            "Unable to deserialize string to AnnualDate",
            result.ExpectOperationResult().Errors![0].Message);
    }

    [Fact]
    public void PatternEmpty_ThrowSchemaException()
    {
        Assert.Throws<SchemaException>(() => new AnnualDateType([]));
    }

    [Fact]
    public void AnnualDateType_DescriptionKnownPatterns_MatchesSnapshot()
    {
        var AnnualDateType = new AnnualDateType(AnnualDatePattern.Iso);

        AnnualDateType.Description.MatchInlineSnapshot(
            """
            AnnualDate represents a date within the calendar, with no reference to a particular time zone, year, or time.

            Allowed patterns:
            - `MM-DD`

            Examples:
            - `01-01`
            """);
    }

    [Fact]
    public void AnnualDateType_DescriptionUnknownPatterns_MatchesSnapshot()
    {
        var AnnualDateType = new AnnualDateType(AnnualDatePattern.Create("MM", CultureInfo.InvariantCulture));

        AnnualDateType.Description.MatchInlineSnapshot(
            "AnnualDate represents a date within the calendar, with no reference to a particular time zone, year, or time.");
    }
}
