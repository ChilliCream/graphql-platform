using Microsoft.Extensions.Primitives;

namespace Mocha;

internal sealed class ChangeTokenSource : IDisposable
{
    private TokenState _state = new(new CancellationTokenSource());

    public IChangeToken Current => Volatile.Read(ref _state).Token;

    public void Rotate()
    {
        var previous = Interlocked.Exchange(ref _state, new TokenState(new CancellationTokenSource()));

        try
        {
            previous.Source.Cancel();
        }
        finally
        {
            previous.Source.Dispose();
        }
    }

    public void Dispose() => _state.Source.Dispose();

    private sealed class TokenState(CancellationTokenSource source)
    {
        public CancellationTokenSource Source { get; } = source;
        public IChangeToken Token { get; } = new CancellationChangeToken(source.Token);
    }
}
