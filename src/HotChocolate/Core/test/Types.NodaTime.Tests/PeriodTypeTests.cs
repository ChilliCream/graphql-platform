using HotChocolate.Execution;
using NodaTime;

namespace HotChocolate.Types.NodaTime.Tests;

public class PeriodTypeIntegrationTests
{
    public static class Schema
    {
        public class Query
        {
            public Period One =>
                Period.FromWeeks(-3) + Period.FromDays(3) + Period.FromTicks(139);
        }

        public class Mutation
        {
            public Period Test(Period arg)
                => arg + Period.FromMinutes(-10);
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
        Assert.Equal("P-3W3DT139t", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Period!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "P-3W15DT139t" }, })
                .Build());
        Assert.Equal("P-3W15DT-10M139t", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseAnIncorrectVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Period!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "-3W3DT-10M139t" }, })
                .Build());
        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
    }

    [Fact]
    public void ParsesLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"P-3W15DT139t\") }")
                .Build());
        Assert.Equal("P-3W15DT-10M139t", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseIncorrectLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"-3W3DT-10M139t\") }")
                .Build());
        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
        Assert.Null(result.ExpectOperationResult().Errors![0].Code);
        Assert.Equal(
            "Unable to deserialize string to Period",
            result.ExpectOperationResult().Errors![0].Message);
    }

    [Fact]
    public void PatternEmptyThrowSchemaException()
    {
        static object Call() => new PeriodType([]);
        Assert.Throws<SchemaException>(Call);
    }
}
