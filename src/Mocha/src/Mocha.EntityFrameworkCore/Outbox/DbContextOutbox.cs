using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Prepares messages for the next save of a <see cref="DbContext"/>.
/// Messages are saved in the same database transaction as the other entity changes.
/// </summary>
public sealed class DbContextOutbox
{
    private readonly DbContextOutboxScope _scope;

    internal DbContextOutbox(DbContext context, DbContextOutboxScope scope)
    {
        Context = context;
        _scope = scope;
    }

    internal DbContext Context { get; }

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
    /// <exception cref="ObjectDisposedException">The DbContext has been disposed.</exception>
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

        _ = Context.GetInfrastructure();
        var bus = _scope.Bus;
        using var publish = OutboxPublishContext.Begin(this);
        await bus.PublishAsync(message, options, cancellationToken);
    }
}
