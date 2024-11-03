using HotChocolate.Execution;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class OffsetDateTypeFullRoundtripIntegrationTests
{
    private readonly IRequestExecutor _testExecutor =
        SchemaBuilder.New()
            .AddQueryType<OffsetDateTypeIntegrationTests.Schema.Query>()
            .AddMutationType<OffsetDateTypeIntegrationTests.Schema.Mutation>()
            .AddNodaTime(typeof(OffsetDateType))
            .AddType(new OffsetDateType(OffsetDatePattern.FullRoundtrip))
            .Create()
            .MakeExecutable();

    [Fact]
    public void QueryReturns()
    {
        var result = _testExecutor.Execute("query { test: hours }");

        Assert.Equal("2020-12-31+02 (Gregorian)", result.ExpectOperationResult()!.Data!["test"]);
    }

    [Fact]
    public void QueryReturnsWithMinutes()
    {
        var result = _testExecutor.Execute("query { test: hoursAndMinutes }");

        Assert.Equal("2020-12-31+02:35 (Gregorian)", result.ExpectOperationResult()!.Data!["test"]);
    }

    [Fact]
    public void ParsesVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: OffsetDate!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-12-31+02 (Gregorian)" }, })
                .Build());

        Assert.Equal("2020-12-31+02 (Gregorian)", result.ExpectOperationResult()!.Data!["test"]);
    }

    [Fact]
    public void ParsesVariableWithMinutes()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: OffsetDate!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-12-31+02:35 (Gregorian)" }, })
                .Build());

        Assert.Equal("2020-12-31+02:35 (Gregorian)", result.ExpectOperationResult()!.Data!["test"]);
    }

    [Fact]
    public void DoesntParseAnIncorrectVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: OffsetDate!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-12-31 (Gregorian)" }, })
                .Build());

        Assert.Null(result.ExpectOperationResult()!.Data);
        Assert.Single(result.ExpectOperationResult()!.Errors!);
    }

    [Fact]
    public void ParsesLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"2020-12-31+02 (Gregorian)\") }")
                .Build());

        Assert.Equal("2020-12-31+02 (Gregorian)", result.ExpectOperationResult()!.Data!["test"]);
    }

    [Fact]
    public void ParsesLiteralWithMinutes()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"2020-12-31+02:35 (Gregorian)\") }")
                .Build());

        Assert.Equal("2020-12-31+02:35 (Gregorian)", result.ExpectOperationResult()!.Data!["test"]);
    }

    [Fact]
    public void DoesntParseIncorrectLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"2020-12-31 (Gregorian)\") }")
                .Build());

        Assert.Null(result.ExpectOperationResult()!.Data);
        Assert.Single(result.ExpectOperationResult()!.Errors!);
        Assert.Null(result.ExpectOperationResult().Errors![0].Code);
        Assert.Equal(
            "Unable to deserialize string to OffsetDate",
            result.ExpectOperationResult().Errors![0].Message);
    }
}
