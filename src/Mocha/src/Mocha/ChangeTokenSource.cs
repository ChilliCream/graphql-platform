using Microsoft.Extensions.Primitives;

namespace Mocha;

internal sealed class ChangeTokenSource
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    private CancellationTokenSource _source = new();
    private IChangeToken _current;

    public ChangeTokenSource()
    {
        _current = new CancellationChangeToken(_source.Token);
    }

    public IChangeToken Current
    {
        get
        {
            lock (_lock)
            {
                return _current;
            }
        }
    }

    public void Rotate()
    {
        CancellationTokenSource previous;

        lock (_lock)
        {
            previous = _source;
            _source = new CancellationTokenSource();
            _current = new CancellationChangeToken(_source.Token);
        }

        try
        {
            previous.Cancel();
        }
        finally
        {
            previous.Dispose();
        }
    }
}
