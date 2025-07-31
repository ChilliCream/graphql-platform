using HotChocolate.Fusion.Execution.Nodes;

public sealed class SubscriptionResult
{
    private readonly IAsyncEnumerable<EventMessageResult>? _stream;

    private SubscriptionResult(
        ExecutionStatus status,
        IAsyncEnumerable<EventMessageResult>? stream,
        Exception? exception)
    {
        Status = status;
        _stream = stream;
        Exception = exception;
    }

    internal static SubscriptionResult Success(
        IAsyncEnumerable<EventMessageResult> stream)
        => new(ExecutionStatus.Success, stream, null);

    internal static SubscriptionResult Failed(
        Exception? exception = null)
        => new(ExecutionStatus.Failed, null, exception);

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
