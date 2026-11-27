using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// The outbox state of a single DbContext instance.
/// </summary>
internal sealed class DbContextOutboxState : IResettableService, IDisposable
{
    /// <summary>
    /// Gets or sets whether the context holds outbox messages that are staged, or written in a transaction,
    /// and not committed yet.
    /// </summary>
    public bool HasUncommittedMessages { get; set; }

    public int Version { get; private set; }

    public void ResetState() => Version++;

    public Task ResetStateAsync(CancellationToken cancellationToken)
    {
        ResetState();
        return Task.CompletedTask;
    }

    public void Dispose() => ResetState();
}
