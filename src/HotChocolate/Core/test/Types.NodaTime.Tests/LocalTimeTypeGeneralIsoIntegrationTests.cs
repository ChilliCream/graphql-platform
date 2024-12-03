using HotChocolate.Execution;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class LocalTimeTypeGeneralIsoIntegrationTests
{
    private readonly IRequestExecutor _testExecutor =
        SchemaBuilder.New()
            .AddQueryType<LocalTimeTypeIntegrationTests.Schema.Query>()
            .AddMutationType<LocalTimeTypeIntegrationTests.Schema.Mutation>()
            .AddNodaTime(typeof(LocalTimeType))
            .AddType(new LocalTimeType(LocalTimePattern.GeneralIso))
            .Create()
            .MakeExecutable();

    [Fact]
    public void QueryReturns()
    {
        var result = _testExecutor.Execute("query { test: one }");

        Assert.Equal("12:42:13", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariable()
    {
        IExecutionResult? result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: LocalTime!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "12:42:13" }, })
                .Build());

        Assert.Equal("12:52:13", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseAnIncorrectVariable()
    {
        IExecutionResult? result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: LocalTime!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "12:42" }, })
                .Build());

        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
    }

    [Fact]
    public void ParsesLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"12:42:13\") }")
                .Build());

        Assert.Equal("12:52:13", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseIncorrectLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"12:42\") }")
                .Build());

        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
        Assert.Null(result.ExpectOperationResult().Errors![0].Code);
        Assert.Equal(
            "Unable to deserialize string to LocalTime",
            result.ExpectOperationResult().Errors![0].Message);
    }
}
