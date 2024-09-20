namespace HotChocolate.Execution;

public sealed class RequestExecutorUpdatedEventArgs : EventArgs
{
    public RequestExecutorUpdatedEventArgs(IRequestExecutor executor)
    {
        Executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public IRequestExecutor Executor { get; }
}
