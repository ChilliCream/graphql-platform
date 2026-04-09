using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Mocha.Testing;

/// <summary>
/// Extension methods for registering message tracking services.
/// </summary>
public static class MessageTrackingExtensions
{
    /// <summary>
    /// Registers the message tracking services including <see cref="IMessageTracker"/>
    /// and a <see cref="FakeTimeProvider"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageTracking(this IServiceCollection services)
    {
        services.TryAddSingleton<MessageTracker>();
        services.AddSingleton<IMessagingDiagnosticEventListener>(sp =>
        {
            var tracker = sp.GetRequiredService<MessageTracker>();
            tracker.SetServiceProvider(sp);
            return tracker;
        });
        services.TryAddSingleton<IMessageTracker>(sp => sp.GetRequiredService<MessageTracker>());

        var timeProvider = new FakeTimeProvider();
        services.TryAddSingleton(timeProvider);
        services.TryAddSingleton<TimeProvider>(timeProvider);

        return services;
    }

    /// <summary>
    /// Registers the message tracking services with a shared <see cref="MessageTracker"/>
    /// that spans multiple independent bus hosts. Pass the same <paramref name="tracker"/>
    /// instance to every host that should participate in unified cross-host tracking.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="tracker">
    /// The shared tracker instance. Pass the same instance to every host that should
    /// participate in unified cross-host tracking.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageTracking(
        this IServiceCollection services,
        MessageTracker tracker)
    {
        services.AddSingleton<IMessagingDiagnosticEventListener>(tracker);
        services.TryAddSingleton<IMessageTracker>(tracker);

        var timeProvider = new FakeTimeProvider();
        services.TryAddSingleton(timeProvider);
        services.TryAddSingleton<TimeProvider>(timeProvider);

        return services;
    }
}
