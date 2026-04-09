using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Extension methods for registering the Event Hub transport health check.
/// </summary>
public static class EventHubHealthCheckExtensions
{
    /// <summary>
    /// Adds a health check that monitors Event Hub receive endpoint processors.
    /// Returns <see cref="HealthStatus.Healthy"/> when all processors are running,
    /// <see cref="HealthStatus.Degraded"/> when some are stopped, and
    /// <see cref="HealthStatus.Unhealthy"/> when none are running.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="transport">The Event Hub transport instance to monitor.</param>
    /// <param name="tags">Optional tags for filtering health checks.</param>
    /// <returns>The health checks builder for method chaining.</returns>
    public static IHealthChecksBuilder AddEventHub(
        this IHealthChecksBuilder builder,
        EventHubMessagingTransport transport,
        params string[] tags)
    {
        var effectiveTags = tags.Length > 0 ? tags : ["ready"];

        builder.Add(new HealthCheckRegistration(
            "EventHub",
            _ => new EventHubHealthCheck(transport),
            failureStatus: HealthStatus.Unhealthy,
            effectiveTags));

        return builder;
    }
}
