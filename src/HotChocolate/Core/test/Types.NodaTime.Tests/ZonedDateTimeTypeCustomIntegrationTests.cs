using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class ZonedDateTimeTypeCustomIntegrationTests
{
    private readonly IRequestExecutor _testExecutor;

    public ZonedDateTimeTypeCustomIntegrationTests()
    {
        var pattern = ZonedDateTimePattern.CreateWithInvariantCulture(
            "uuuu'-'MM'-'dd'T'HH':'mm':'ss' 'z' '(o<g>)",
            DateTimeZoneProviders.Tzdb);

        _testExecutor = SchemaBuilder.New()
            .AddQueryType<ZonedDateTimeTypeIntegrationTests.Schema.Query>()
            .AddMutationType<ZonedDateTimeTypeIntegrationTests.Schema.Mutation>()
            .AddNodaTime(typeof(ZonedDateTimeType))
            .AddType(new ZonedDateTimeType(pattern))
            .Create()
            .MakeExecutable();
    }

    [Fact]
    public void QueryReturns()
    {
        IExecutionResult result = _testExecutor.Execute("query { test: rome }");
        Assert.Equal(
            "2020-12-31T18:30:13 Asia/Kathmandu (+05:45)", 
            result.ExpectQueryResult().Data!["test"]);
    }

    [Fact]
    public void QueryReturnsUtc()
    {
        IExecutionResult result = _testExecutor.Execute("query { test: utc }");
        Assert.Equal("2020-12-31T18:30:13 UTC (+00)", result.ExpectQueryResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariable()
    {
        IExecutionResult result = _testExecutor
            .Execute(QueryRequestBuilder.New()
                .SetQuery("mutation($arg: ZonedDateTime!) { test(arg: $arg) }")
                .SetVariableValue("arg", "2020-12-31T19:30:13 Asia/Kathmandu (+05:45)")
                .Create());

        Assert.Equal(
            "2020-12-31T19:40:13 Asia/Kathmandu (+05:45)",
            result.ExpectQueryResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariableWithUTC()
    {
        IExecutionResult result = _testExecutor
            .Execute(QueryRequestBuilder.New()
                .SetQuery("mutation($arg: ZonedDateTime!) { test(arg: $arg) }")
                .SetVariableValue("arg", "2020-12-31T19:30:13 UTC (+00)")
                .Create());

        Assert.Equal("2020-12-31T19:40:13 UTC (+00)", result.ExpectQueryResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseAnIncorrectVariable()
    {
        IExecutionResult result = _testExecutor
            .Execute(QueryRequestBuilder.New()
                .SetQuery("mutation($arg: ZonedDateTime!) { test(arg: $arg) }")
                .SetVariableValue("arg", "2020-12-31T19:30:13 (UTC)")
                .Create());

        Assert.Null(result.ExpectQueryResult().Data);
        Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
    }

    [Fact]
    public void ParsesLiteral()
    {
        IExecutionResult result = _testExecutor
            .Execute(QueryRequestBuilder.New()
                .SetQuery(
                    @"mutation
                    {
                        test(arg: ""2020-12-31T19:30:13 Asia/Kathmandu (+05:45)"") 
                    }")
                .Create());

        Assert.Equal(
            "2020-12-31T19:40:13 Asia/Kathmandu (+05:45)",
            result.ExpectQueryResult().Data!["test"]);
    }

    [Fact]
    public void ParsesLiteralWithUTC()
    {
        IExecutionResult result = _testExecutor
            .Execute(QueryRequestBuilder.New()
                .SetQuery("mutation { test(arg: \"2020-12-31T19:30:13 UTC (+00)\") }")
                .Create());

        Assert.Equal("2020-12-31T19:40:13 UTC (+00)", result.ExpectQueryResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseIncorrectLiteral()
    {
        IExecutionResult result = _testExecutor
            .Execute(QueryRequestBuilder.New()
                .SetQuery("mutation { test(arg: \"2020-12-31T19:30:13 (UTC)\") }")
                .Create());

        Assert.Null(result.ExpectQueryResult().Data);
        Assert.Equal(1, result.ExpectQueryResult().Errors!.Count);
        Assert.Null(result.ExpectQueryResult().Errors![0].Code);
        Assert.Equal(
            "Unable to deserialize string to ZonedDateTime",
            result.ExpectQueryResult().Errors![0].Message);
    }
}
