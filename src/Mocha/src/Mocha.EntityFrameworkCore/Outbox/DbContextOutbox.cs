using Microsoft.EntityFrameworkCore;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Prepares messages for the next save of a <see cref="DbContext"/>.
/// Messages are saved in the same database transaction as the other entity changes.
/// </summary>
public sealed class DbContextOutbox
{
    private DbContextOutboxScope? _scope;
    private int _contextLease;

    internal DbContextOutbox(DbContext context)
    {
        Context = context;
    }

    internal DbContext Context { get; }

    internal void BindScope(DbContextOutboxScope scope)
    {
        _scope = scope;
        _contextLease = Context.ContextId.Lease;
    }

    /// <summary>
    /// Prepares a message for publication when the DbContext is saved.
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">A token to cancel preparation of the message.</param>
    public ValueTask PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : notnull
        => PublishAsync(message, PublishOptions.Default, cancellationToken);

    /// <summary>
    /// Prepares a message with the specified options for publication when the DbContext is saved.
    /// Scheduled messages are not supported.
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="options">The options of the publish operation.</param>
    /// <param name="cancellationToken">A token to cancel preparation of the message.</param>
    /// <exception cref="ArgumentException">The options specify a scheduled time.</exception>
    /// <exception cref="InvalidOperationException">The DbContext has not been bound to an application scope.</exception>
    public async ValueTask PublishAsync<T>(
        T message,
        PublishOptions options,
        CancellationToken cancellationToken)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(message);

        if (options.ScheduledTime is not null)
        {
            throw new ArgumentException(
                "DbContext.Outbox does not support scheduled messages.",
                nameof(options));
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (_scope is null || _contextLease != Context.ContextId.Lease)
        {
            throw new InvalidOperationException(
                $"The DbContext '{Context.GetType().FullName}' has no application scope for its outbox. "
                + "Register the DbContext before AddEntityFramework and resolve the DbContext itself "
                + "from an application scope. Contexts created directly by IDbContextFactory are not bound to a scope.");
        }

        var bus = _scope.Bus;
        using var publish = OutboxPublishContext.Begin(this);
        await bus.PublishAsync(message, options, cancellationToken);
    }
}
