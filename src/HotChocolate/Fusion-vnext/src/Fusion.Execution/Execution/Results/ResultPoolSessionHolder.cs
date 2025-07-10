using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

internal sealed class ResultPoolSessionHolder(ObjectPool<ResultPoolSession> pool) : IDisposable
{
    public ResultPoolSession Session { get; } = pool.Get();

    public void Dispose() => pool.Return(Session);
}
