namespace Mocha.Mediator;

/// <summary>
/// Specifies how notification handler pipelines are dispatched.
/// </summary>
public enum NotificationPublishMode
{
    /// <summary>
    /// Handlers are invoked sequentially, awaiting each before proceeding to the next.
    /// If a handler throws, subsequent handlers are not invoked.
    /// </summary>
    Sequential,

    /// <summary>
    /// Handlers are invoked concurrently using <see cref="Task.WhenAll(Task[])"/>.
    /// All handlers are started simultaneously and awaited together.
    /// <para>
    /// <b>Warning:</b> All concurrent handler pipelines share the same scoped
    /// <see cref="IServiceProvider"/>. Scoped services such as <c>DbContext</c>
    /// are not thread-safe and must not be used concurrently across handlers.
    /// </para>
    /// </summary>
    Concurrent
}
