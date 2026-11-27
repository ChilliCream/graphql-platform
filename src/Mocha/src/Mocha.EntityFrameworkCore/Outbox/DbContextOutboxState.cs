namespace Mocha.EntityFrameworkCore;

/// <summary>
/// The outbox state of a single DbContext instance.
/// </summary>
internal sealed class DbContextOutboxState
{
    /// <summary>
    /// Gets or sets whether the context wrote outbox messages in a transaction that has not committed yet.
    /// </summary>
    public bool HasUncommittedMessages { get; set; }
}
