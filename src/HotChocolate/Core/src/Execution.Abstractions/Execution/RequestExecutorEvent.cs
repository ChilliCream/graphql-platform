namespace HotChocolate.Execution;

public sealed record RequestExecutorEvent(
    RequestExecutorEventType Type,
    string Name,
    IRequestExecutor Executor)
{
    public static RequestExecutorEvent Created(IRequestExecutor executor)
        => new(RequestExecutorEventType.Created, executor.Schema.Name, executor);

    public static RequestExecutorEvent Evicted(IRequestExecutor executor)
        => new(RequestExecutorEventType.Evicted, executor.Schema.Name, executor);
}
