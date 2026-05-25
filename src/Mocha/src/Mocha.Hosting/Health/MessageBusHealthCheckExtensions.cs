using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mocha.Hosting;

/// <summary>
/// Provides extension methods for registering message bus health check infrastructure.
/// </summary>
public static class MessageBusHealthCheckExtensions
{
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
    /// Adds a message bus health check to the ASP.NET Core health checks system, tagged with "ready" and "live".
    /// </summary>
    /// <remarks>
    /// The check sends a <see cref="HealthRequest"/> through the message bus and verifies the response.
    /// When an <paramref name="endpoint"/> is provided, the request is routed to that specific endpoint
    /// rather than using the default routing.
    /// </remarks>
    /// <param name="builder">The health checks builder to extend.</param>
    /// <param name="endpoint">An optional endpoint URI to target with the health check request.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IHealthChecksBuilder AddMessageBus(this IHealthChecksBuilder builder, Uri? endpoint = null)
    {
        if (endpoint is not null)
        {
            builder.Services.Configure<MessageBusHealthCheckOptions>(o => o.Endpoint = endpoint);
        }

        builder.AddCheck<MessageBusHealthCheck>("MessageBus", HealthStatus.Unhealthy, ["ready", "live"]);

        return builder;
    }
}
