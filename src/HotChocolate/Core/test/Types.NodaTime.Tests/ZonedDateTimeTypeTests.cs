using System.Globalization;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class ZonedDateTimeTypeIntegrationTests
{
    public static class Schema
    {
        public class Query
        {
            public ZonedDateTime Rome =>
                new ZonedDateTime(
                    LocalDateTime.FromDateTime(new DateTime(2020, 12, 31, 18, 30, 13)),
                    DateTimeZoneProviders.Tzdb["Asia/Kathmandu"],
                    Offset.FromHoursAndMinutes(5, 45));

            public ZonedDateTime Utc =>
                new ZonedDateTime(
                    LocalDateTime.FromDateTime(new DateTime(2020, 12, 31, 18, 30, 13)),
                    DateTimeZoneProviders.Tzdb["UTC"],
                    Offset.FromHours(0));
        }

        public class Mutation
        {
            public ZonedDateTime Test(ZonedDateTime arg)
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
        var result = _testExecutor.Execute("query { test: rome }");
        Assert.Equal(
            "2020-12-31T18:30:13 Asia/Kathmandu +05:45",
            Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void QueryReturnsUtc()
    {
        var result = _testExecutor.Execute("query { test: utc }");
        Assert.Equal(
            "2020-12-31T18:30:13 UTC +00",
            Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void ParsesVariable()
    {
        var result = _testExecutor
            .Execute(
                OperationRequestBuilder.New()
                    .SetDocument("mutation($arg: ZonedDateTime!) { test(arg: $arg) }")
                    .SetVariableValues(
                        new Dictionary<string, object?>
                        {
                            { "arg", "2020-12-31T19:30:13 Asia/Kathmandu +05:45" },
                        })
                    .Build());
        Assert.Equal(
            "2020-12-31T19:40:13 Asia/Kathmandu +05:45",
            Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void ParsesVariableWithUtc()
    {
        var result = _testExecutor
            .Execute(
                OperationRequestBuilder.New()
                    .SetDocument("mutation($arg: ZonedDateTime!) { test(arg: $arg) }")
                    .SetVariableValues(
                        new Dictionary<string, object?>
                        {
                            { "arg", "2020-12-31T19:30:13 UTC +00" }
                        })
                    .Build());
        Assert.Equal(
            "2020-12-31T19:40:13 UTC +00",
            Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void DoesntParseAnIncorrectVariable()
    {
        var result = _testExecutor
            .Execute(
                OperationRequestBuilder.New()
                    .SetDocument("mutation($arg: ZonedDateTime!) { test(arg: $arg) }")
                    .SetVariableValues(
                        new Dictionary<string, object?>
                        {
                            { "arg", "2020-12-31T19:30:13 UTC" },
                        })
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
                    .SetDocument("mutation{test(arg:\"2020-12-31T19:30:13 Asia/Kathmandu +05:45\")}")
                    .Build());
        Assert.Equal(
            "2020-12-31T19:40:13 Asia/Kathmandu +05:45",
            result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesLiteralWithUtc()
    {
        var result = _testExecutor
            .Execute(
                OperationRequestBuilder.New()
                    .SetDocument("mutation { test(arg: \"2020-12-31T19:30:13 UTC +00\") }")
                    .Build());
        Assert.Equal("2020-12-31T19:40:13 UTC +00", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseIncorrectLiteral()
    {
        var result = _testExecutor
            .Execute(
                OperationRequestBuilder.New()
                    .SetDocument("mutation { test(arg: \"2020-12-31T19:30:13 UTC\") }")
                    .Build());
        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
        Assert.Null(result.ExpectOperationResult().Errors![0].Code);
        Assert.Equal(
            "Unable to deserialize string to ZonedDateTime",
            result.ExpectOperationResult().Errors![0].Message);
    }

    [Fact]
    public void PatternEmpty_ThrowSchemaException()
    {
        static object Call() => new ZonedDateTimeType([]);
        Assert.Throws<SchemaException>(Call);
    }

    [Fact]
    public void ZonedDateTimeType_DescriptionKnownPatterns_MatchesSnapshot()
    {
        var zonedDateTimeType = new ZonedDateTimeType(
            ZonedDateTimePattern.GeneralFormatOnlyIso,
            ZonedDateTimePattern.ExtendedFormatOnlyIso);

        zonedDateTimeType.Description.MatchInlineSnapshot(
            """
            A LocalDateTime in a specific time zone and with a particular offset to distinguish between otherwise-ambiguous instants.
            A ZonedDateTime is global, in that it maps to a single Instant.

            Allowed patterns:
            - `YYYY-MM-DDThh:mm:ss z (±hh:mm)`
            - `YYYY-MM-DDThh:mm:ss.sssssssss z (±hh:mm)`

            Examples:
            - `2000-01-01T20:00:00 Europe/Zurich (+01)`
            - `2000-01-01T20:00:00.999999999 Europe/Zurich (+01)`
            """);
    }

    [Fact]
    public void ZonedDateTimeType_DescriptionUnknownPatterns_MatchesSnapshot()
    {
        var zonedDateTimeType = new ZonedDateTimeType(
            ZonedDateTimePattern.Create(
                "MM",
                CultureInfo.InvariantCulture,
                null,
                null,
                new ZonedDateTime()));

        zonedDateTimeType.Description.MatchInlineSnapshot(
            """
            A LocalDateTime in a specific time zone and with a particular offset to distinguish between otherwise-ambiguous instants.
            A ZonedDateTime is global, in that it maps to a single Instant.
            """);
    }
}
