using HotChocolate.Execution;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class OffsetTimeTypeRfc3339IntegrationTests
{
    private readonly IRequestExecutor _testExecutor = SchemaBuilder.New()
        .AddQueryType<OffsetTimeTypeIntegrationTests.Schema.Query>()
        .AddMutationType<OffsetTimeTypeIntegrationTests.Schema.Mutation>()
        .AddNodaTime(typeof(OffsetTimeType))
        .AddType(new OffsetTimeType(OffsetTimePattern.Rfc3339))
        .Create()
        .MakeExecutable();

    [Fact]
    public void QueryReturns()
    {
        var result = _testExecutor.Execute("query { test: hours }");
        Assert.Equal("18:30:13.010011234+02:00", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void QueryReturnsWithMinutes()
    {
        var result = _testExecutor.Execute("query { test: hoursAndMinutes }");
        Assert.Equal("18:30:13.010011234+02:35", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: OffsetTime!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "18:30:13.010011234+02:00" }, })
                .Build());

        Assert.Equal("18:30:13.010011234+02:00", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariableWithMinutes()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: OffsetTime!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "18:30:13.010011234+02:35" }, })
                .Build());
        Assert.Equal("18:30:13.010011234+02:35", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseAnIncorrectVariable()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: OffsetTime!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "18:30:13.010011234+02" }, })
                .Build());
        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
    }

    [Fact]
    public void ParsesLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"18:30:13.010011234+02:00\") }")
                .Build());
        Assert.Equal("18:30:13.010011234+02:00", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesLiteralWithMinutes()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"18:30:13.010011234+02:35\") }")
                .Build());
        Assert.Equal("18:30:13.010011234+02:35", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseIncorrectLiteral()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"18:30:13.010011234+02\") }")
                .Build());
        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
        Assert.Null(result.ExpectOperationResult().Errors![0].Code);
        Assert.Equal(
            "Unable to deserialize string to OffsetTime",
            result.ExpectOperationResult().Errors![0].Message);
    }
}
