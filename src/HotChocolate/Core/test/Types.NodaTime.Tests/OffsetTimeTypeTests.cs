using System.Globalization;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class OffsetTimeTypeIntegrationTests
{
    public static class Schema
    {
        public class Query
        {
            public OffsetTime Hours =>
                new OffsetTime(
                    LocalTime
                        .FromHourMinuteSecondMillisecondTick(18, 30, 13, 10, 100)
                        .PlusNanoseconds(1234),
                    Offset.FromHours(2));

            public OffsetTime HoursAndMinutes =>
                new OffsetTime(
                    LocalTime
                        .FromHourMinuteSecondMillisecondTick(18, 30, 13, 10, 100)
                        .PlusNanoseconds(1234),
                    Offset.FromHoursAndMinutes(2, 35));
        }

        public class Mutation
        {
            public OffsetTime Test(OffsetTime arg) => arg;
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
        Assert.Equal("18:30:13+02", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void QueryReturnsWithMinutes()
    {
        var result = _testExecutor.Execute("query { test: hoursAndMinutes }");
        Assert.Equal("18:30:13+02:35", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: OffsetTime!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "18:30:13+02" }, })
                .Build());
        Assert.Equal("18:30:13+02", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariableWithMinutes()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: OffsetTime!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "18:30:13+02:35" }, })
                .Build());
        Assert.Equal("18:30:13+02:35", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseAnIncorrectVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: OffsetTime!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "18:30:13" }, })
                .Build());
        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
    }

    [Fact]
    public void ParsesLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"18:30:13+02\") }")
                .Build());
        Assert.Equal("18:30:13+02", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesLiteralWithMinutes()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"18:30:13+02:35\") }")
                .Build());
        Assert.Equal("18:30:13+02:35", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseIncorrectLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"18:30:13\") }")
                .Build());

        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
        Assert.Null(result.ExpectOperationResult().Errors![0].Code);
        Assert.Equal(
            "Unable to deserialize string to OffsetTime",
            result.ExpectOperationResult().Errors![0].Message);
    }

    [Fact]
    public void PatternEmptyThrowSchemaException()
    {
        static object Call() => new OffsetTimeType([]);
        Assert.Throws<SchemaException>(Call);
    }

    [Fact]
    public void OffsetTimeType_DescriptionKnownPatterns_MatchesSnapshot()
    {
        var offsetTimeType = new OffsetTimeType(
            OffsetTimePattern.GeneralIso,
            OffsetTimePattern.ExtendedIso);

        offsetTimeType.Description.MatchInlineSnapshot(
            """
            A combination of a LocalTime and an Offset, to represent a time-of-day at a specific offset from UTC but without any date information.

            Allowed patterns:
            - `hh:mm:ss±hh:mm`
            - `hh:mm:ss.sssssssss±hh:mm`

            Examples:
            - `20:00:00Z`
            - `20:00:00.999Z`
            """);
    }

    [Fact]
    public void OffsetTimeType_DescriptionUnknownPatterns_MatchesSnapshot()
    {
        var offsetTimeType = new OffsetTimeType(
            OffsetTimePattern.Create("mm", CultureInfo.InvariantCulture, new OffsetTime()));

        offsetTimeType.Description.MatchInlineSnapshot(
            "A combination of a LocalTime and an Offset, to represent a time-of-day at a specific offset from UTC but without any date information.");
    }
}
