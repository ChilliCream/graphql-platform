using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class DurationTypeJsonRoundtripIntegrationTests
{
    public static class Schema
    {
        public class Query
        {
            public Duration PositiveWithDecimals =>
                Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 10, 19));

            public Duration NegativeWithDecimals =>
                -Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 10, 19));

            public Duration PositiveWithoutDecimals =>
                Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 10));

            public Duration PositiveWithoutSeconds =>
                Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 0));

            public Duration PositiveWithoutMinutes =>
                Duration.FromTimeSpan(new TimeSpan(123, 7, 0, 0));

            public Duration PositiveWithRoundtrip =>
                 Duration.FromTimeSpan(new TimeSpan(123, 26, 0, 70));
        }

        public class Mutation
        {
            public Duration Test(Duration arg)
                => arg + Duration.FromMinutes(10);
        }
    }

    private readonly IRequestExecutor _testExecutor = SchemaBuilder.New()
        .AddQueryType<Schema.Query>()
        .AddMutationType<Schema.Mutation>()
        .AddNodaTime(excludeTypes: typeof(DurationType))
        .AddType(new DurationType(DurationPattern.JsonRoundtrip))
        .Create()
        .MakeExecutable();

    [Fact]
    public void QueryReturnsSerializedDataWithDecimals()
    {
        var result = _testExecutor.Execute("query { test: positiveWithDecimals }");
        Assert.Equal("2959:53:10.019", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void QueryReturnsSerializedDataWithNegativeValue()
    {
        var result = _testExecutor.Execute("query { test: negativeWithDecimals }");
        Assert.Equal("-2959:53:10.019", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void QueryReturnsSerializedDataWithoutDecimals()
    {
        var result = _testExecutor.Execute("query { test: positiveWithoutDecimals }");
        Assert.Equal("2959:53:10", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void QueryReturnsSerializedDataWithoutSeconds()
    {
        var result = _testExecutor.Execute("query { test: positiveWithoutSeconds }");
        Assert.Equal("2959:53:00", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void QueryReturnsSerializedDataWithoutMinutes()
    {
        var result = _testExecutor.Execute("query { test: positiveWithoutMinutes }");
        Assert.Equal("2959:00:00", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void QueryReturnsSerializedDataWithRoundtrip()
    {
        var result = _testExecutor.Execute("query { test: positiveWithRoundtrip }");
        Assert.Equal("2978:01:10", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void MutationParsesInputWithDecimals()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Duration!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "238:01:00.019" }, })
                .Build());
        Assert.Equal("238:11:00.019", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void MutationParsesInputWithoutDecimals()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Duration!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "238:01:00" }, })
                .Build());
        Assert.Equal("238:11:00", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void MutationParsesInputWithoutLeadingZero()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Duration!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "238:01:00" }, })
                .Build());
        Assert.Equal("238:11:00", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void MutationParsesInputWithNegativeValue()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Duration!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "-238:01:00" }, })
                .Build());
        Assert.Equal("-237:51:00", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void MutationDoesntParseInputWithPlusSign()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Duration!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "+09:22:01:00" }, })
                .Build());
        Assert.Null(Assert.IsType<OperationResult>(result).Data);
        Assert.Single(Assert.IsType<OperationResult>(result).Errors!);
    }

    [Fact]
    public void MutationParsesLiteralWithDecimals()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"238:01:00.019\") }")
                .Build());
        Assert.Equal("238:11:00.019", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void MutationParsesLiteralWithoutDecimals()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"238:01:00\") }")
                .Build());
        Assert.Equal("238:11:00", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void MutationParsesLiteralWithoutLeadingZero()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"238:01:00\") }")
                .Build());
        Assert.Equal("238:11:00", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void MutationParsesLiteralWithNegativeValue()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"-238:01:00\") }")
                .Build());
        Assert.Equal("-237:51:00", Assert.IsType<OperationResult>(result).Data!["test"]);
    }

    [Fact]
    public void MutationDoesntParseLiteralWithPlusSign()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"+238:01:00\") }")
                .Build());
        Assert.Null(Assert.IsType<OperationResult>(result).Data);
        Assert.Single(Assert.IsType<OperationResult>(result).Errors!);
    }
}
