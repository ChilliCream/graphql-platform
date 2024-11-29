namespace StrawberryShake;

internal class ObserverSession : IDisposable
{
    private readonly object _sync = new();
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
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(ObserverSession).FullName);
            }

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
