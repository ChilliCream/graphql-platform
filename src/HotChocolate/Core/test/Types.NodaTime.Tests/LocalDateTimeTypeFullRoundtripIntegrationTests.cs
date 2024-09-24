using HotChocolate.Execution;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class LocalDateTimeTypeFullRoundtripIntegrationTests
{
    private readonly IRequestExecutor _testExecutor =
        SchemaBuilder.New()
            .AddQueryType<LocalDateTimeTypeIntegrationTests.Schema.Query>()
            .AddMutationType<LocalDateTimeTypeIntegrationTests.Schema.Mutation>()
            .AddNodaTime(typeof(LocalDateTimeType))
            .AddType(new LocalDateTimeType(LocalDateTimePattern.FullRoundtrip))
            .Create()
            .MakeExecutable();

    [Fact]
    public void QueryReturns()
    {
        var result = _testExecutor.Execute("query { test: one }");

        Assert.Equal(
            "2020-02-07T17:42:59.000001234 (Julian)",
            result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: LocalDateTime!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-02-21T17:42:59.000001234 (Julian)" }, })
                .Build());

        Assert.Equal(
            "2020-02-21T17:52:59.000001234 (Julian)",
            result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseAnIncorrectVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: LocalDateTime!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "2020-02-20T17:42:59Z" }, })
                .Build());

        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
    }

    [Fact]
    public void ParsesLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"2020-02-20T17:42:59.000001234 (Julian)\") }")
                .Build());

        Assert.Equal(
            "2020-02-20T17:52:59.000001234 (Julian)",
            result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseIncorrectLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"2020-02-20T17:42:59Z\") }")
                .Build());

        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
        Assert.Null(result.ExpectOperationResult().Errors![0].Code);
        Assert.Equal(
            "Unable to deserialize string to LocalDateTime",
            result.ExpectOperationResult().Errors![0].Message);
    }
}
