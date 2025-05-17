using System.Globalization;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class OffsetDateTimeTypeIntegrationTests
{
    public static class Schema
    {
        public class Query
        {
            public OffsetDateTime Hours
                => OffsetDateTime
                    .FromDateTimeOffset(
                        new DateTimeOffset(
                            2020,
                            12,
                            31,
                            18,
                            30,
                            13,
                            TimeSpan.FromHours(2)))
                    .PlusNanoseconds(1234);

            public OffsetDateTime HoursAndMinutes
                => OffsetDateTime
                    .FromDateTimeOffset(
                        new DateTimeOffset(
                            2020,
                            12,
                            31,
                            18,
                            30,
                            13,
                            TimeSpan.FromHours(2) + TimeSpan.FromMinutes(30)))
                    .PlusNanoseconds(1234);
        }

        public class Mutation
        {
            public OffsetDateTime Test(OffsetDateTime arg)
            {
                return arg + Duration.FromMinutes(10);
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
        var result = _testExecutor.Execute("query { test: hours }");

        Assert.Equal("2020-12-31T18:30:13+02", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void QueryReturnsWithMinutes()
    {
        var result = _testExecutor.Execute("query { test: hoursAndMinutes }");

        Assert.Equal("2020-12-31T18:30:13+02:30", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariable()
    {
        var result = _testExecutor
            .Execute(
                OperationRequestBuilder.New()
                    .SetDocument("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { { "arg", "2020-12-31T18:30:13+02" }, })
                    .Build());

        Assert.Equal("2020-12-31T18:40:13+02", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariableWithMinutes()
    {
        var result = _testExecutor
            .Execute(
                OperationRequestBuilder.New()
                    .SetDocument("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { { "arg", "2020-12-31T18:30:13+02:35" }, })
                    .Build());

        Assert.Equal("2020-12-31T18:40:13+02:35", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseAnIncorrectVariable()
    {
        var result = _testExecutor
            .Execute(
                OperationRequestBuilder.New()
                    .SetDocument("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { { "arg", "2020-12-31T18:30:13" }, })
                    .Build());

        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
    }

    [Fact]
    public void ParsesLiteral()
    {
        var result = _testExecutor
            .Execute(
                OperationRequestBuilder.New()
                    .SetDocument("mutation { test(arg: \"2020-12-31T18:30:13+02\") }")
                    .Build());

        Assert.Equal("2020-12-31T18:40:13+02", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesLiteralWithMinutes()
    {
        var result = _testExecutor
            .Execute(
                OperationRequestBuilder.New()
                    .SetDocument("mutation { test(arg: \"2020-12-31T18:30:13+02:35\") }")
                    .Build());

        Assert.Equal("2020-12-31T18:40:13+02:35", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseIncorrectLiteral()
    {
        var result = _testExecutor
            .Execute(
                OperationRequestBuilder.New()
                    .SetDocument("mutation { test(arg: \"2020-12-31T18:30:13\") }")
                    .Build());

        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
        Assert.Null(result.ExpectOperationResult().Errors![0].Code);
        Assert.Equal(
            "Unable to deserialize string to OffsetDateTime",
            result.ExpectOperationResult().Errors![0].Message);
    }

    [Fact]
    public void PatternEmptyThrowSchemaException()
    {
        static object Call() => new OffsetDateTimeType([]);
        Assert.Throws<SchemaException>(Call);
    }

    [Fact]
    public void OffsetDateTimeType_DescriptionKnownPatterns_MatchesSnapshot()
    {
        var offsetDateTimeType = new OffsetDateTimeType(
            OffsetDateTimePattern.ExtendedIso,
            OffsetDateTimePattern.FullRoundtrip);

        offsetDateTimeType.Description.MatchInlineSnapshot(
            """
            A local date and time in a particular calendar system, combined with an offset from UTC.

            Allowed patterns:
            - `YYYY-MM-DDThh:mm:ss.sssssssss±hh:mm`
            - `YYYY-MM-DDThh:mm:ss.sssssssss±hh:mm (calendar)`

            Examples:
            - `2000-01-01T20:00:00.999Z`
            - `2000-01-01T20:00:00.999Z (ISO)`
            """);
    }

    [Fact]
    public void OffsetDateTimeType_DescriptionUnknownPatterns_MatchesSnapshot()
    {
        var offsetDateTimeType = new OffsetDateTimeType(
            OffsetDateTimePattern.Create("MM", CultureInfo.InvariantCulture, new OffsetDateTime()));

        offsetDateTimeType.Description.MatchInlineSnapshot(
            "A local date and time in a particular calendar system, combined with an offset from UTC.");
    }
}
