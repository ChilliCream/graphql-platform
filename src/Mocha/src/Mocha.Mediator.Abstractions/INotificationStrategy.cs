namespace Mocha.Mediator;

/// <summary>
/// Defines the strategy for dispatching notifications to multiple handlers.
/// </summary>
/// <remarks>
/// Implementations control how handlers are invoked (e.g., sequentially, in parallel, or with specific error handling).
/// </remarks>
public interface INotificationStrategy
{
    /// <summary>
    /// Publishes a notification to all of the provided handlers using this strategy.
    /// </summary>
    /// <typeparam name="TNotification">The type of notification to publish.</typeparam>
    /// <param name="handlers">The collection of handlers to dispatch the notification to.</param>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask PublishAsync<TNotification>(
        IReadOnlyList<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification;
}
