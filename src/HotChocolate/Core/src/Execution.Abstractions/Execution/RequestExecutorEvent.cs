namespace HotChocolate.Execution;

public sealed record RequestExecutorEvent(
    RequestExecutorEventType Type,
    string Name,
    IRequestExecutor Executor);
