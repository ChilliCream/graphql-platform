namespace Mocha.Mediator;

/// <summary>
/// Feature that carries the resolved <see cref="INotificationStrategy"/> on the mediator runtime.
/// </summary>
internal sealed class NotificationStrategyFeature(INotificationStrategy strategy)
{
    public INotificationStrategy Strategy { get; } = strategy;
}
