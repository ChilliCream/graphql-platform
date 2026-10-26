using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mocha.EntityFrameworkCore;

internal sealed class DbContextOutboxState : IResettableService, IDisposable
{
    public int Preparing;

    public bool IsPreparing => Volatile.Read(ref Preparing) != 0;

    public int Version { get; private set; }

    public void ResetState()
    {
        Preparing = 0;
        Version++;
    }

    public Task ResetStateAsync(CancellationToken cancellationToken)
    {
        ResetState();
        return Task.CompletedTask;
    }

    public void Dispose() => ResetState();
}
