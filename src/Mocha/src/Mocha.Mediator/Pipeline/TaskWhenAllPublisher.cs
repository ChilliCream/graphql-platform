namespace Mocha.Mediator;

/// <summary>
/// Represents a notification publishing strategy that dispatches to all handlers concurrently
/// using <see cref="Task.WhenAll(Task[])"/>.
/// </summary>
public sealed class TaskWhenAllPublisher : INotificationStrategy
{
    /// <summary>
    /// Publishes a notification to all handlers concurrently, awaiting all completions.
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

        return PublishConcurrently(handlers, notification, cancellationToken, count);
    }

    private static async ValueTask PublishConcurrently<TNotification>(
        IReadOnlyList<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken,
        int count)
        where TNotification : INotification
    {
        var tasks = new Task[count];

        for (var i = 0; i < count; i++)
        {
            tasks[i] = handlers[i].HandleAsync(notification, cancellationToken).AsTask();
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
