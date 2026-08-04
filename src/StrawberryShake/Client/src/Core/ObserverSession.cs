namespace StrawberryShake;

internal class ObserverSession : IDisposable
{
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private IDisposable? _storeSession;
    private bool _disposed;

    public RequestSession RequestSession { get; } = new();

    public bool HasStoreSession
    {
        get
        {
            if (_storeSession is not null)
            {
                return true;
            }

            lock (_sync)
            {
                return _storeSession is not null;
            }
        }
    }

    public void SetStoreSession(IDisposable storeSession)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _storeSession = storeSession;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (!_disposed)
            {
                RequestSession.Dispose();
                _storeSession?.Dispose();
                _disposed = true;
            }
        }
    }
}
