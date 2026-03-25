using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator;

/// <summary>
/// Represents the configuration options for the Mocha Mediator.
/// </summary>
public sealed class MediatorOptions
{
    /// <summary>
    /// Gets or sets the default service lifetime for handlers and behaviors.
    /// Default is <see cref="ServiceLifetime.Scoped"/>.
    /// </summary>
    public ServiceLifetime ServiceLifetime { get; set; } = ServiceLifetime.Scoped;

    /// <summary>
    /// Gets or sets how notification handler pipelines are dispatched.
    /// Default is <see cref="NotificationPublishMode.Sequential"/>.
    /// </summary>
    public NotificationPublishMode NotificationPublishMode { get; set; } = NotificationPublishMode.Sequential;
}

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
