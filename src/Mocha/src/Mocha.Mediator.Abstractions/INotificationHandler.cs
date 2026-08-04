namespace Mocha.Mediator;

/// <summary>
/// Defines a handler for a notification. Multiple handlers may be registered
/// for the same notification type.
/// </summary>
/// <typeparam name="TNotification">The type of notification to handle.</typeparam>
public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    /// <summary>
    /// Handles the specified notification.
    /// </summary>
    /// <param name="notification">The notification to handle.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken);
}
