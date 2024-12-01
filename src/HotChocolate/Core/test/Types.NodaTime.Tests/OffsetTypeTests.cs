using System.Globalization;
using HotChocolate.Execution;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime.Tests;

public class OffsetTypeIntegrationTests
{
    public static class Schema
    {
        public class Query
        {
            public Offset Hours => Offset.FromHours(2);
            public Offset HoursAndMinutes => Offset.FromHoursAndMinutes(2, 35);
            public Offset ZOffset => Offset.Zero;
        }

        public class Mutation
        {
            public Offset Test(Offset arg)
                => arg + Offset.FromHoursAndMinutes(1, 5);
        }
    }

    private readonly IRequestExecutor _testExecutor = SchemaBuilder.New()
        .AddQueryType<Schema.Query>()
        .AddMutationType<Schema.Mutation>()
        .AddNodaTime()
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
        Assert.Equal("Z", result.ExpectOperationResult().Data!["test"]);
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
    public void ParsesLiteralWithZ()
    {
        var result = _testExecutor
            .Execute(OperationRequestBuilder.New()
                .SetDocument("mutation { test(arg: \"Z\") }")
                .Build());
        Assert.Equal("+01:05", result.ExpectOperationResult().Data!["test"]);
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

    [Fact]
    public void PatternEmptyThrowSchemaException()
    {
        static object Call() => new OffsetType([]);
        Assert.Throws<SchemaException>(Call);
    }

    [Fact]
    public void OffsetType_DescriptionKnownPatterns_MatchesSnapshot()
    {
        var offsetType = new OffsetType(
            OffsetPattern.GeneralInvariant,
            OffsetPattern.GeneralInvariantWithZ);

        offsetType.Description.MatchInlineSnapshot(
            """
            An offset from UTC in seconds.
            A positive value means that the local time is ahead of UTC (e.g. for Europe); a negative value means that the local time is behind UTC (e.g. for America).

            Allowed patterns:
            - `±hh:mm:ss`
            - `Z`

            Examples:
            - `+02:30:00`
            - `Z`
            """);
    }

    [Fact]
    public void OffsetType_DescriptionUnknownPatterns_MatchesSnapshot()
    {
        var offsetType = new OffsetType(
            OffsetPattern.Create("mm", CultureInfo.InvariantCulture));

        offsetType.Description.MatchInlineSnapshot(
            """
            An offset from UTC in seconds.
            A positive value means that the local time is ahead of UTC (e.g. for Europe); a negative value means that the local time is behind UTC (e.g. for America).
            """);
    }
}
