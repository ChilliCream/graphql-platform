using System.Globalization;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class DurationTypeIntegrationTests
{
    public static class Schema
    {
        public class Query
        {
            public Duration PositiveWithDecimals
                => Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 10, 19));
            public Duration NegativeWithDecimals
                => -Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 10, 19));
            public Duration PositiveWithoutDecimals
                => Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 10));
            public Duration PositiveWithoutSeconds
                => Duration.FromTimeSpan(new TimeSpan(123, 7, 53, 0));
            public Duration PositiveWithoutMinutes
                => Duration.FromTimeSpan(new TimeSpan(123, 7, 0, 0));
            public Duration PositiveWithRoundtrip
                => Duration.FromTimeSpan(new TimeSpan(123, 26, 0, 70));
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
        .AddNodaTime()
        .Create()
        .MakeExecutable();

    [Fact]
    public void QueryReturnsSerializedDataWithDecimals()
    {
        var result = _testExecutor.Execute("query { test: positiveWithDecimals }");
        Assert.Equal("123:07:53:10.019", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void QueryReturnsSerializedDataWithNegativeValue()
    {
        var result = _testExecutor.Execute("query{test: negativeWithDecimals}");
        Assert.Equal("-123:07:53:10.019", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void QueryReturnsSerializedDataWithoutDecimals()
    {
        var result = _testExecutor.Execute("query{test: positiveWithoutDecimals}");
        Assert.Equal("123:07:53:10", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void QueryReturnsSerializedDataWithoutSeconds()
    {
        var result = _testExecutor.Execute("query{test:positiveWithoutSeconds}");
        Assert.Equal("123:07:53:00", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void QueryReturnsSerializedDataWithoutMinutes()
    {
        var result = _testExecutor.Execute("query{test:positiveWithoutMinutes}");
        Assert.Equal("123:07:00:00", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void QueryReturnsSerializedDataWithRoundtrip()
    {
        var result = _testExecutor.Execute("query{test:positiveWithRoundtrip}");
        Assert.Equal("124:02:01:10", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void MutationParsesInputWithDecimals()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Duration!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "09:22:01:00.019" }, })
                .Build());
        Assert.Equal("9:22:11:00.019", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void MutationParsesInputWithoutDecimals()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Duration!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "09:22:01:00" }, })
                .Build());
        Assert.Equal("9:22:11:00", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void MutationParsesInputWithoutLeadingZero()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Duration!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "9:22:01:00" }, })
                .Build());
        Assert.Equal("9:22:11:00", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void MutationParsesInputWithNegativeValue()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Duration!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "-9:22:01:00" }, })
                .Build());
        Assert.Equal("-9:21:51:00", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void MutationDoesntParseInputWithPlusSign()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Duration!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "+09:22:01:00" }, })
                .Build());
        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
    }

    [Fact]
    public void MutationDoesntParseInputWithOverflownHours()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation($arg: Duration!) { test(arg: $arg) }")
                .SetVariableValues(new Dictionary<string, object?> { {"arg", "9:26:01:00" }, })
                .Build());
        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
    }

    [Fact]
    public void MutationParsesLiteralWithDecimals()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"09:22:01:00.019\") }")
                .Build());

        Assert.Equal("9:22:11:00.019", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void MutationParsesLiteralWithoutDecimals()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"09:22:01:00\") }")
                .Build());

        Assert.Equal("9:22:11:00", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void MutationParsesLiteralWithoutLeadingZero()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"09:22:01:00\") }")
                .Build());

        Assert.Equal("9:22:11:00", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void MutationParsesLiteralWithNegativeValue()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"-9:22:01:00\") }")
                .Build());

        Assert.Equal("-9:21:51:00", result.ExpectOperationResult().Data!["test"]);
    }

    [Fact]
    public void MutationDoesntParseLiteralWithPlusSign()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"+09:22:01:00\") }")
                .Build());

        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
    }

    [Fact]
    public void MutationDoesntParseLiteralWithOverflownHours()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"9:26:01:00\") }")
                .Build());

        Assert.Null(result.ExpectOperationResult().Data);
        Assert.Single(result.ExpectOperationResult().Errors!);
    }

    [Fact]
    public void PatternEmpty_ThrowSchemaException()
    {
        static object Call() => new DurationType([]);
        Assert.Throws<SchemaException>(Call);
    }

    [Fact]
    public void DurationType_DescriptionKnownPatterns_MatchesSnapshot()
    {
        var durationType = new DurationType(
            DurationPattern.Roundtrip,
            DurationPattern.JsonRoundtrip);

        durationType.Description.MatchInlineSnapshot(
            """
            Represents a fixed (and calendar-independent) length of time.

            Allowed patterns:
            - `-D:hh:mm:ss.sssssssss`
            - `-hh:mm:ss.sssssssss`

            Examples:
            - `-1:20:00:00.999999999`
            - `-44:00:00.999999999`
            """);
    }

    [Fact]
    public void DurationType_DescriptionUnknownPatterns_MatchesSnapshot()
    {
        var durationType = new DurationType(
            DurationPattern.Create("mm", CultureInfo.InvariantCulture));

        durationType.Description.MatchInlineSnapshot(
            "Represents a fixed (and calendar-independent) length of time.");
    }
}
