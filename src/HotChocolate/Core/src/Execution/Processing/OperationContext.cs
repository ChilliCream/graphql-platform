namespace HotChocolate.Execution.Processing;

/// <summary>
/// The internal context of the execution engine.
/// </summary>
internal sealed partial class OperationContext
{
    public IDictionary<string, object?> ContextData
    {
        get
        {
            AssertInitialized();
            return _contextData;
        }
    }

    /// <summary>
    /// Gets a cancellation token is used to signal
    /// if the request has be aborted.
    /// </summary>
    public CancellationToken RequestAborted
    {
        get
        {
            AssertInitialized();
            return _requestAborted;
        }
    }
}
