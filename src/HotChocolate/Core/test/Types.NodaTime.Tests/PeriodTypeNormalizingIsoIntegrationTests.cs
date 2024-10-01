using HotChocolate.Execution;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class PeriodTypeNormalizingIsoIntegrationTests
{
    private readonly IRequestExecutor _testExecutor = SchemaBuilder.New()
        .AddQueryType<PeriodTypeIntegrationTests.Schema.Query>()
        .AddMutationType<PeriodTypeIntegrationTests.Schema.Mutation>()
        .AddNodaTime(typeof(PeriodType))
        .AddType(new PeriodType(PeriodPattern.NormalizingIso))
        .Create()
        .MakeExecutable();

    [Fact]
    public void QueryReturns()
    {
        var result = _testExecutor.Execute("query { test: one }");
        Assert.Equal("P-17DT-23H-59M-59.9999861S", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Period!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "P-17DT-23H-59M-59.9999861S" }, })
                .Build());
        Assert.Equal("P-18DT-9M-59.9999861S", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseAnIncorrectVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Period!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "-P-17DT-23H-59M-59.9999861S" }, })
                .Build());
        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
    }

    [Fact]
    public void ParsesLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"P-17DT-23H-59M-59.9999861S\") }")
                .Build());
        Assert.Equal("P-18DT-9M-59.9999861S", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseIncorrectLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"-P-17DT-23H-59M-59.9999861S\") }")
                .Build());
        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
        Assert.Null(result.ExpectOperationResult().Errors![0].Code);
        Assert.Equal(
            "Unable to deserialize string to Period",
            result.ExpectOperationResult().Errors![0].Message);
    }
}
