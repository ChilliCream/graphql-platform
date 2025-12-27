using HotChocolate.Execution;
using NodaTime;

namespace HotChocolate.Types.NodaTime.Tests;

public class DateTimeZoneTypeIntegrationTests
{
    public static class Schema
    {
        public class Query
        {
            public DateTimeZone Utc => DateTimeZone.Utc;
            public DateTimeZone Rome => DateTimeZoneProviders.Tzdb["Europe/Rome"];
            public DateTimeZone Chihuahua => DateTimeZoneProviders.Tzdb["America/Chihuahua"];
        }

        public class Mutation
        {
            public string Test(DateTimeZone arg)
            {
                return arg.Id;
            }
        }
    }

    private readonly IRequestExecutor _testExecutor = SchemaBuilder.New()
        .AddQueryType<Schema.Query>()
        .AddMutationType<Schema.Mutation>()
        .AddNodaTime()
        .Create()
        .MakeExecutable();

    [Fact]
    public void QueryReturnsUtc()
    {
        var result = _testExecutor.Execute("query { test: utc }");
        Assert.Equal("UTC", result.ExpectOperationResult().UnwrapData().GetProperty("test").GetString());
    }

    [Fact]
    public void QueryReturnsRome()
    {
        var result = _testExecutor.Execute("query { test: rome }");
        Assert.Equal("Europe/Rome", result.ExpectOperationResult().UnwrapData().GetProperty("test").GetString());
    }

    [Fact]
    public void QueryReturnsChihuahua()
    {
        var result = _testExecutor.Execute("query { test: chihuahua }");
        Assert.Equal("America/Chihuahua", result.ExpectOperationResult().UnwrapData().GetProperty("test").GetString());
    }

    [Fact]
    public void ParsesVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: DateTimeZone!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { { "arg", "Europe/Amsterdam" } })
                .Build());
        Assert.Equal("Europe/Amsterdam", result.ExpectOperationResult().UnwrapData().GetProperty("test").GetString());
    }

    [Fact]
    public void DoesNotParseIncorrectVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: DateTimeZone!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { { "arg", "Europe/Hamster" } })
                .Build());
        Assert.True(result.ExpectOperationResult().IsDataNull);
        Assert.Single(result.ExpectOperationResult().Errors!);
    }

    [Fact]
    public void ParsesLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"Europe/Amsterdam\") }")
                .Build());
        Assert.Equal("Europe/Amsterdam", result.ExpectOperationResult().UnwrapData().GetProperty("test").GetString());
    }

    [Fact]
    public void DoesNotParseIncorrectLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"Europe/Hamster\") }")
                .Build());
        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
        Assert.Null(result.ExpectOperationResult().Errors![0].Code);
        Assert.Equal(
            "Unable to deserialize string to DateTimeZone",
            result.ExpectOperationResult().Errors![0].Message);
    }
}
