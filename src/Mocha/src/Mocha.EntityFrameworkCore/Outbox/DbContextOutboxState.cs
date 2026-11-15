using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mocha.EntityFrameworkCore;

internal sealed class DbContextOutboxState : IResettableService, IDisposable
{
    public int Version { get; private set; }

    public void ResetState() => Version++;

    public Task ResetStateAsync(CancellationToken cancellationToken)
    {
        ResetState();
        return Task.CompletedTask;
    }

    public void Dispose() => ResetState();
}
