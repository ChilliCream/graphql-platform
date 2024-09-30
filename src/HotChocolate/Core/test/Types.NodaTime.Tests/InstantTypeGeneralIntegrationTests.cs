using HotChocolate.Execution;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class InstantTypeGeneralIntegrationTests
{
    private readonly IRequestExecutor _testExecutor =
        SchemaBuilder.New()
            .AddQueryType<InstantTypeIntegrationTests.Schema.Query>()
            .AddMutationType<InstantTypeIntegrationTests.Schema.Mutation>()
            .AddNodaTime(typeof(InstantType))
            .AddType(new InstantType(InstantPattern.General))
            .Create()
            .MakeExecutable();

    [Fact]
    public void QueryReturnsUtc()
    {
        var result = _testExecutor.Execute("query { test: one }");

        Assert.Equal("2020-02-20T17:42:59Z", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Instant!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-02-21T17:42:59Z" }, })
                .Build());

        Assert.Equal("2020-02-21T17:52:59Z", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseAnIncorrectVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Instant!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-02-20T17:42:59" }, })
                .Build());

        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
    }

    [Fact]
    public void ParsesLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"2020-02-20T17:42:59Z\") }")
                .Build());

        Assert.Equal("2020-02-20T17:52:59Z", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseIncorrectLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"2020-02-20T17:42:59\") }")
                .Build());

        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
        Assert.Null(result.ExpectOperationResult().Errors!.First().Code);
        Assert.Equal(
            "Unable to deserialize string to Instant",
            result.ExpectOperationResult().Errors!.First().Message);
    }
}
