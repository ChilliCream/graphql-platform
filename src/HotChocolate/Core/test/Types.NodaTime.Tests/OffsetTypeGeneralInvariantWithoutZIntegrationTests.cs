using HotChocolate.Execution;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class OffsetTypeGeneralInvariantWithoutZIntegrationTests
{
    private readonly IRequestExecutor _testExecutor =
        SchemaBuilder.New()
            .AddQueryType<OffsetTypeIntegrationTests.Schema.Query>()
            .AddMutationType<OffsetTypeIntegrationTests.Schema.Mutation>()
            .AddNodaTime(typeof(OffsetType))
            .AddType(new OffsetType(OffsetPattern.GeneralInvariant))
            .Create()
            .MakeExecutable();

    [Fact]
    public void QueryReturns()
    {
        var result = _testExecutor.Execute("query { test: hours }");
        Assert.Equal("+02", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void QueryReturnsWithMinutes()
    {
        var result = _testExecutor.Execute("query { test: hoursAndMinutes }");
        Assert.Equal("+02:35", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void QueryReturnsWithZ()
    {
        var result = _testExecutor.Execute("query { test: zOffset }");
        Assert.Equal("+00", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Offset!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "+02" }, })
                .Build());
        Assert.Equal("+03:05", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariableWithMinutes()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Offset!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "+02:35" }, })
                .Build());
        Assert.Equal("+03:40", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseAnIncorrectVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Offset!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "18:30:13+02" }, })
                .Build());
        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
    }

    [Fact]
    public void ParsesLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"+02\") }")
                .Build());

        Assert.Equal("+03:05", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesLiteralWithMinutes()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"+02:35\") }")
                .Build());
        Assert.Equal("+03:40", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesLiteralWithZero()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"+00\") }")
                .Build());
        Assert.Equal("+01:05", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseLiteralWithZ()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"Z\") }")
                .Build());
        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
        Assert.Null(result.ExpectOperationResult().Errors![0].Code);
        Assert.Equal(
            "Unable to deserialize string to Offset",
            result.ExpectOperationResult().Errors![0].Message);
    }

    [Fact]
    public void DoesntParseIncorrectLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"18:30:13+02\") }")
                .Build());

        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
        Assert.Null(result.ExpectOperationResult().Errors![0].Code);
        Assert.Equal(
            "Unable to deserialize string to Offset",
            result.ExpectOperationResult().Errors![0].Message);
    }
}
