namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// The cancellation of one publish: its token is cancelled when the publish timeout elapses or when the
/// caller cancels. Instances are reused, so a publish allocates none.
/// </summary>
internal sealed class PublishDeadline : IDisposable
{
    private readonly CancellationTokenSource _source = new();
    private CancellationTokenRegistration _caller;

    /// <summary>
    /// Gets the token that the operations of the publish observe.
    /// </summary>
    public CancellationToken Token => _source.Token;

    /// <summary>
    /// Gets whether the token was cancelled, by the timeout or by the caller.
    /// </summary>
    public bool IsCancellationRequested => _source.IsCancellationRequested;

    /// <summary>
    /// Starts the timeout and follows the caller's token.
    /// </summary>
    public void Start(TimeSpan timeout, CancellationToken callerToken)
    {
        _source.CancelAfter(timeout);
        if (callerToken.CanBeCanceled)
        {
            _caller = callerToken.UnsafeRegister(static state => ((CancellationTokenSource)state!).Cancel(), _source);
        }
    }

    /// <summary>
    /// Stops the timeout and stops following the caller's token. Returns <c>false</c> when the token was
    /// cancelled, after which the instance cannot be used again.
    /// </summary>
    public bool TryReset()
    {
        _caller.Dispose();
        _caller = default;
        return _source.TryReset();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _caller.Dispose();
        _source.Dispose();
    }
}
