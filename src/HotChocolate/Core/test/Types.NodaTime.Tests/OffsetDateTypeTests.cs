using System.Globalization;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class OffsetDateTypeIntegrationTests
{
    public static class Schema
    {
        public class Query
        {
            public OffsetDate Hours
                => new OffsetDate(
                    LocalDate.FromDateTime(new DateTime(2020, 12, 31, 18, 30, 13)),
                    Offset.FromHours(2)).WithCalendar(CalendarSystem.Gregorian);

            public OffsetDate HoursAndMinutes
                => new OffsetDate(
                    LocalDate.FromDateTime(new DateTime(2020, 12, 31, 18, 30, 13)),
                    Offset.FromHoursAndMinutes(2, 35)).WithCalendar(CalendarSystem.Gregorian);
        }

        public class Mutation
        {
            public OffsetDate Test(OffsetDate arg) => arg;
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
        var result = _testExecutor.Execute("query { test: hours }");
        Assert.Equal("2020-12-31+02", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void QueryReturnsWithMinutes()
    {
        var result = _testExecutor.Execute("query { test: hoursAndMinutes }");
        Assert.Equal("2020-12-31+02:35", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: OffsetDate!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-12-31+02" }, })
                .Build());
        Assert.Equal("2020-12-31+02", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariableWithMinutes()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: OffsetDate!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-12-31+02:35" }, })
                .Build());
        Assert.Equal("2020-12-31+02:35", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseAnIncorrectVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: OffsetDate!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-12-31" }, })
                .Build());
        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
    }

    [Fact]
    public void ParsesLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"2020-12-31+02\") }")
                .Build());
        Assert.Equal("2020-12-31+02", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesLiteralWithMinutes()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"2020-12-31+02:35\") }")
                .Build());
        Assert.Equal("2020-12-31+02:35", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseIncorrectLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"2020-12-31\") }")
                .Build());
        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
        Assert.Null(result.ExpectOperationResult().Errors![0].Code);
        Assert.Equal(
            "Unable to deserialize string to OffsetDate",
            result.ExpectOperationResult().Errors![0].Message);
    }

    [Fact]
    public void PatternEmptyThrowSchemaException()
    {
        static object Call() => new OffsetDateType([]);
        Assert.Throws<SchemaException>(Call);
    }

    [Fact]
    public void OffsetDateType_DescriptionKnownPatterns_MatchesSnapshot()
    {
        var offsetDateType = new OffsetDateType(
            OffsetDatePattern.GeneralIso,
            OffsetDatePattern.FullRoundtrip);

        offsetDateType.Description.MatchInlineSnapshot(
            """
            A combination of a LocalDate and an Offset, to represent a date at a specific offset from UTC but without any time-of-day information.

            Allowed patterns:
            - `YYYY-MM-DD±hh:mm`
            - `YYYY-MM-DD±hh:mm (calendar)`

            Examples:
            - `2000-01-01Z`
            - `2000-01-01Z (ISO)`
            """);
    }

    [Fact]
    public void OffsetDateType_DescriptionUnknownPatterns_MatchesSnapshot()
    {
        var offsetDateType = new OffsetDateType(
            OffsetDatePattern.Create("MM", CultureInfo.InvariantCulture, new OffsetDate()));

        offsetDateType.Description.MatchInlineSnapshot(
            "A combination of a LocalDate and an Offset, to represent a date at a specific offset from UTC but without any time-of-day information.");
    }
}
