using HotChocolate.Execution;
using NodaTime;
using NodaTime.Calendars;

namespace HotChocolate.Types.NodaTime.Tests;

public sealed class CalendarSystemTypeIntegrationTests
{
    private static class Schema
    {
        public class Query
        {
            public CalendarSystem Iso => CalendarSystem.Iso;
            public CalendarSystem Gregorian => CalendarSystem.Gregorian;

            public CalendarSystem HijriAstronomicalHabashAlHasib =>
                CalendarSystem.GetIslamicCalendar(IslamicLeapYearPattern.HabashAlHasib, IslamicEpoch.Astronomical);
        }

        public class Mutation
        {
            public CalendarSystem Test(CalendarSystem arg) => arg;
        }
    }

    private readonly IRequestExecutor _testExecutor = SchemaBuilder.New()
        .AddQueryType<Schema.Query>()
        .AddMutationType<Schema.Mutation>()
        .AddNodaTime()
        .Create()
        .MakeExecutable();

    [Fact]
    public void QueryReturnsIso()
    {
        var result = _testExecutor.Execute("query { test: iso }");
        Assert.Equal("ISO", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void QueryReturnsGregorian()
    {
        var result = _testExecutor.Execute("query { test: gregorian }");
        Assert.Equal("Gregorian", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void HijriAstronomicalHabashAlHasib()
    {
        var result = _testExecutor.Execute("query { test: hijriAstronomicalHabashAlHasib }");
        Assert.Equal("Hijri Astronomical-HabashAlHasib", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void ParsesVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: CalendarSystem!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { { "arg", "ISO" } })
                .Build());
        Assert.Equal("ISO", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void DoesntParseIncorrectVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: CalendarSystem!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { { "arg", "Christmas" } })
                .Build());
        Assert.Null(Assert.IsType<OperationResult>(result).Data);
        Assert.Single(Assert.IsType<OperationResult>(result).Errors!);
    }

    [Fact]
    public void ParsesLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"ISO\") }")
                .Build());
        Assert.Equal("ISO", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void DoesntParseIncorrectLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"Christmas\") }")
                .Build());
        Assert.Null(Assert.IsType<OperationResult>(result).Data);
        Assert.Single(Assert.IsType<OperationResult>(result).Errors!);
        Assert.Null(Assert.IsType<OperationResult>(result).Errors!.First().Code);
        Assert.Equal(
            "Unable to deserialize string to CalendarSystem",
            Assert.IsType<OperationResult>(result).Errors!.First().Message);
    }

    [Fact]
    public void CalendarSystemType_Description_MatchesSnapshot()
    {
        var calendarSystemType = new CalendarSystemType();

        calendarSystemType.Description.MatchInlineSnapshot(
            """
            A calendar system maps the non-calendar-specific "local time line" to human concepts such as years, months and days.

            Example: `ISO`
            """);
    }
}
