using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mocha.Hosting;

/// <summary>
/// Provides extension methods for registering message bus health check infrastructure.
/// </summary>
public static class MessageBusHealthCheckExtensions
{
    private static readonly string[] s_defaultTags = ["ready"];
    private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Registers the built-in health request handler so the message bus can respond to health check requests.
    /// </summary>
    /// <param name="builder">The message bus host builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IMessageBusHostBuilder AddHealthCheck(this IMessageBusHostBuilder builder)
    {
        builder.AddRequestHandler<HealthRequestHandler>();
        return builder;
    }

    /// <summary>
    /// Adds a message bus check to the ASP.NET Core health checks system.
    /// </summary>
    /// <remarks>
    /// The check requires every local transport to be healthy and a <see cref="HealthRequest"/> to be answered
    /// within <paramref name="timeout"/>. Each unhealthy transport is listed with its reason in the result data.
    /// </remarks>
    /// <param name="builder">The health checks builder to extend.</param>
    /// <param name="endpoint">
    /// The endpoint the health request is sent to, or <see langword="null"/> to use default routing.
    /// </param>
    /// <param name="name">The name of the health check.</param>
    /// <param name="failureStatus">
    /// The status reported when the check fails, or <see langword="null"/> for <see cref="HealthStatus.Unhealthy"/>.
    /// </param>
    /// <param name="tags">The tags of the health check, or <see langword="null"/> for the tag "ready".</param>
    /// <param name="timeout">
    /// The time within which the health request must be answered, or <see langword="null"/> for 10 seconds.
    /// The health request expires after this time.
    /// </param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is zero or negative.</exception>
    public static IHealthChecksBuilder AddMessageBus(
        this IHealthChecksBuilder builder,
        Uri? endpoint = null,
        string name = "MessageBus",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        if (timeout is { } value)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero, nameof(timeout));
        }

        var options = new MessageBusHealthCheckOptions { Endpoint = endpoint, Timeout = timeout ?? s_defaultTimeout };

        // The check applies the timeout itself, so the registration carries none.
        return builder.Add(
            new HealthCheckRegistration(
                name,
                services => new MessageBusHealthCheck(
                    services.GetRequiredService<IMessageBus>(),
                    services.GetRequiredService<IMessagingRuntime>(),
                    services.GetService<TimeProvider>() ?? TimeProvider.System,
                    options),
                failureStatus,
                tags ?? s_defaultTags));
    }
}
