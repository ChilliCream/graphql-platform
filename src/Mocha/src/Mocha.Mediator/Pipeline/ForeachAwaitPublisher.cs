namespace Mocha.Mediator;

/// <summary>
/// Represents a notification publishing strategy that dispatches to each handler sequentially,
/// awaiting completion before proceeding to the next.
/// </summary>
public sealed class ForeachAwaitPublisher : INotificationStrategy
{
    /// <summary>
    /// Publishes a notification to each handler in sequence, awaiting each one before proceeding.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification to publish.</typeparam>
    /// <param name="handlers">The collection of handlers to notify.</param>
    /// <param name="notification">The notification instance to publish.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public ValueTask PublishAsync<TNotification>(
        IReadOnlyList<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        var count = handlers.Count;

        if (count == 0)
        {
            return default;
        }

        if (count == 1)
        {
            return handlers[0].HandleAsync(notification, cancellationToken);
        }

        return PublishSequentially(handlers, notification, cancellationToken, count);
    }

    private static async ValueTask PublishSequentially<TNotification>(
        IReadOnlyList<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken,
        int count)
        where TNotification : INotification
    {
        for (var i = 0; i < count; i++)
        {
            await handlers[i].HandleAsync(notification, cancellationToken).ConfigureAwait(false);
        }
    }
}
