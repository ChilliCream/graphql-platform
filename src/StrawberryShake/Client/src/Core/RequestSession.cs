namespace StrawberryShake;

internal class RequestSession : IDisposable
{
    private readonly CancellationTokenSource _cts;
    private bool _disposed;

    public RequestSession()
    {
        _cts = new CancellationTokenSource();
    }

    public CancellationToken Abort => _cts.Token;

    public void Cancel()
    {
        try
        {
            if (!_disposed)
            {
                _cts.Cancel();
            }
        }
        catch (ObjectDisposedException)
        {
            // we do not care if this happens.
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Cancel();
            _cts.Dispose();
            _disposed = true;
        }
    }
}
