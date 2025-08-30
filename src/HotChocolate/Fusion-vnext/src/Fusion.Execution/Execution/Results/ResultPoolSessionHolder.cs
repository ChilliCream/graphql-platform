using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

internal sealed class ResultPoolSessionHolder(ObjectPool<ResultPoolSession> pool) : IDisposable
{
    private ResultPoolSession? _session = pool.Get();

    public ResultPoolSession Session
    {
        get
        {
            var session = _session;
            return session ?? throw new ObjectDisposedException(nameof(ResultPoolSessionHolder));
        }
    }

    public void Dispose()
    {
        var session = Interlocked.Exchange(ref _session, null);
        if (session is not null)
        {
            pool.Return(session);
        }
    }

    public static implicit operator ResultPoolSession(ResultPoolSessionHolder holder)
        => holder.Session;
}
