namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class SubscriptionResult
{
    private readonly IAsyncEnumerable<EventMessageResult>? _stream;

    private SubscriptionResult(
        ulong id,
        ExecutionStatus status,
        IAsyncEnumerable<EventMessageResult>? stream,
        Exception? exception)
    {
        Id = id;
        Status = status;
        _stream = stream;
        Exception = exception;
    }

    internal static SubscriptionResult Success(
        ulong id,
        IAsyncEnumerable<EventMessageResult> stream)
        => new(id, ExecutionStatus.Success, stream, null);

    internal static SubscriptionResult Failed(
        ulong id,
        Exception? exception = null)
        => new(id, ExecutionStatus.Failed, null, exception);

    public ulong Id { get; }

    public ExecutionStatus Status { get; }

    public Exception? Exception { get; }

    public IAsyncEnumerable<EventMessageResult> ReadStreamAsync()
    {
        if (_stream is null)
        {
            throw new InvalidOperationException();
        }

        return _stream;
    }
}
