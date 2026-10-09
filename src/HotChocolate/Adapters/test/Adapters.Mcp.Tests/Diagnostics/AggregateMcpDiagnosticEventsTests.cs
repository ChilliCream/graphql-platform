namespace HotChocolate.Adapters.Mcp.Diagnostics;

public sealed class AggregateMcpDiagnosticEventsTests
{
    [Fact]
    public void InitializePrompts_Should_ForwardToAllListeners()
    {
        // arrange
        var (aggregate, first, second) = CreateAggregate();

        // act
        aggregate.InitializePrompts().Dispose();

        // assert
        string[] expected = ["InitializePrompts", "InitializePrompts.Disposed"];
        Assert.Equal(expected, first.Events);
        Assert.Equal(expected, second.Events);
    }

    [Fact]
    public void UpdatePrompts_Should_ForwardToAllListeners()
    {
        // arrange
        var (aggregate, first, second) = CreateAggregate();

        // act
        aggregate.UpdatePrompts().Dispose();

        // assert
        string[] expected = ["UpdatePrompts", "UpdatePrompts.Disposed"];
        Assert.Equal(expected, first.Events);
        Assert.Equal(expected, second.Events);
    }

    [Fact]
    public void InitializeTools_Should_ForwardToAllListeners()
    {
        // arrange
        var (aggregate, first, second) = CreateAggregate();

        // act
        aggregate.InitializeTools().Dispose();

        // assert
        string[] expected = ["InitializeTools", "InitializeTools.Disposed"];
        Assert.Equal(expected, first.Events);
        Assert.Equal(expected, second.Events);
    }

    [Fact]
    public void UpdateTools_Should_ForwardToAllListeners()
    {
        // arrange
        var (aggregate, first, second) = CreateAggregate();

        // act
        aggregate.UpdateTools().Dispose();

        // assert
        string[] expected = ["UpdateTools", "UpdateTools.Disposed"];
        Assert.Equal(expected, first.Events);
        Assert.Equal(expected, second.Events);
    }

    [Fact]
    public void ToolCreationFailed_Should_ForwardToAllListeners()
    {
        // arrange
        var (aggregate, first, second) = CreateAggregate();

        // act
        aggregate.ToolCreationFailed("failing_tool", new InvalidOperationException());

        // assert
        string[] expected = ["ToolCreationFailed(failing_tool)"];
        Assert.Equal(expected, first.Events);
        Assert.Equal(expected, second.Events);
    }

    [Fact]
    public void ValidationErrors_Should_ForwardToAllListeners()
    {
        // arrange
        var (aggregate, first, second) = CreateAggregate();

        // act
        aggregate.ValidationErrors([]);

        // assert
        string[] expected = ["ValidationErrors"];
        Assert.Equal(expected, first.Events);
        Assert.Equal(expected, second.Events);
    }

    private static AggregateSetup CreateAggregate()
    {
        var first = new RecordingListener();
        var second = new RecordingListener();

        return new AggregateSetup(new AggregateMcpDiagnosticEvents([first, second]), first, second);
    }

    private sealed record AggregateSetup(
        AggregateMcpDiagnosticEvents Aggregate,
        RecordingListener First,
        RecordingListener Second);

    private sealed class RecordingListener : McpDiagnosticEventListener
    {
        public List<string> Events { get; } = [];

        public override IDisposable InitializePrompts() => RecordScope("InitializePrompts");

        public override IDisposable UpdatePrompts() => RecordScope("UpdatePrompts");

        public override IDisposable InitializeTools() => RecordScope("InitializeTools");

        public override IDisposable UpdateTools() => RecordScope("UpdateTools");

        public override void ToolCreationFailed(string toolName, Exception exception)
            => Events.Add($"ToolCreationFailed({toolName})");

        public override void ValidationErrors(IReadOnlyList<IError> errors)
            => Events.Add("ValidationErrors");

        private RecordingScope RecordScope(string name)
        {
            Events.Add(name);

            return new RecordingScope(this, name);
        }

        private sealed class RecordingScope(RecordingListener listener, string name) : IDisposable
        {
            public void Dispose() => listener.Events.Add($"{name}.Disposed");
        }
    }
}
