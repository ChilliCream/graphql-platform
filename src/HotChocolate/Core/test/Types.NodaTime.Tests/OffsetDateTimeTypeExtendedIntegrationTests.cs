using HotChocolate.Execution;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class OffsetDateTimeTypeExtendedIntegrationTests
{
    private readonly IRequestExecutor _testExecutor =
        SchemaBuilder.New()
            .AddQueryType<OffsetDateTimeTypeIntegrationTests.Schema.Query>()
            .AddMutationType<OffsetDateTimeTypeIntegrationTests.Schema.Mutation>()
            .AddNodaTime(typeof(OffsetDateTimeType))
            .AddType(new OffsetDateTimeType(OffsetDateTimePattern.ExtendedIso))
            .Create()
            .MakeExecutable();

    [Fact]
    public void QueryReturns()
    {
        var result = _testExecutor.Execute("query { test: hours }");

        Assert.Equal("2020-12-31T18:30:13.000001234+02", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void QueryReturnsWithMinutes()
    {
        var result = _testExecutor.Execute("query { test: hoursAndMinutes }");

        Assert.Equal("2020-12-31T18:30:13.000001234+02:30", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariable()
    {
        var result = _testExecutor
            .Execute(
                OperationRequestBuilder.New()
                    .SetDocument("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { { "arg", "2020-12-31T18:30:13+02" }, })
                    .Build());

        Assert.Equal("2020-12-31T18:40:13+02", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesVariableWithMinutes()
    {
        var result = _testExecutor
            .Execute(
                OperationRequestBuilder.New()
                    .SetDocument("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { { "arg", "2020-12-31T18:30:13+02:35" }, })
                    .Build());

        Assert.Equal("2020-12-31T18:40:13+02:35", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseAnIncorrectVariable()
    {
        var result = _testExecutor
            .Execute(
                OperationRequestBuilder.New()
                    .SetDocument("mutation($arg: OffsetDateTime!) { test(arg: $arg) }")
                    .SetVariableValues(new Dictionary<string, object?> { { "arg", "2020-12-31T18:30:13" }, })
                    .Build());

        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
    }

    [Fact]
    public void ParsesLiteral()
    {
        var result = _testExecutor
            .Execute(
                OperationRequestBuilder.New()
                    .SetDocument("mutation { test(arg: \"2020-12-31T18:30:13+02\") }")
                    .Build());

        Assert.Equal("2020-12-31T18:40:13+02", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void ParsesLiteralWithMinutes()
    {
        var result = _testExecutor
            .Execute(
                OperationRequestBuilder.New()
                    .SetDocument("mutation { test(arg: \"2020-12-31T18:30:13+02:35\") }")
                    .Build());

        Assert.Equal("2020-12-31T18:40:13+02:35", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void DoesntParseIncorrectLiteral()
    {
        var result = _testExecutor
            .Execute(
                OperationRequestBuilder.New()
                    .SetDocument("mutation { test(arg: \"2020-12-31T18:30:13\") }")
                    .Build());

        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
        Assert.Null(result.ExpectOperationResult().Errors![0].Code);
        Assert.Equal(
            "Unable to deserialize string to OffsetDateTime",
            result.ExpectOperationResult().Errors![0].Message);
    }
}
