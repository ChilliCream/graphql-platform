using Microsoft.Extensions.DependencyInjection;

namespace Mocha;

/// <summary>
/// Extension methods for registering OpenTelemetry instrumentation with the message bus host.
/// </summary>
public static class InstrumentationBusExtensions
{
    /// <summary>
    /// Registers the <see cref="OpenTelemetryDiagnosticObserver"/> to emit traces and metrics
    /// for all dispatch, receive, and consume operations on the bus.
    /// </summary>
    /// <param name="builder">The host builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IMessageBusHostBuilder AddInstrumentation(this IMessageBusHostBuilder builder)
    {
        builder.Services.AddSingleton<IBusDiagnosticObserver, OpenTelemetryDiagnosticObserver>();
        return builder;
    }
}
