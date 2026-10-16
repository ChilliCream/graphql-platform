namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Identifies the outbox that receives messages prepared by <see cref="DbContextOutbox.PublishAsync{T}(T, CancellationToken)"/>.
/// </summary>
internal static class OutboxPublishContext
{
    private static readonly AsyncLocal<DbContextOutbox?> s_current = new();

    /// <summary>
    /// Gets the target outbox, or <c>null</c> for a regular bus publish.
    /// </summary>
    public static DbContextOutbox? Current => s_current.Value;

    /// <summary>
    /// Selects <paramref name="outbox"/> until the returned scope is disposed.
    /// </summary>
    public static Scope Begin(DbContextOutbox outbox)
    {
        var previous = s_current.Value;
        s_current.Value = outbox;
        return new Scope(previous);
    }

    public readonly struct Scope(DbContextOutbox? previous) : IDisposable
    {
        public void Dispose() => s_current.Value = previous;
    }
}
