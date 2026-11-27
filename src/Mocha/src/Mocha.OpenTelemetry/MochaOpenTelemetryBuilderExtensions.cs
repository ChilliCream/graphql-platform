using Mocha;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace OpenTelemetry;

/// <summary>
/// Provides extension methods for configuring Mocha telemetry on an <see cref="OpenTelemetryBuilder"/>.
/// </summary>
public static class MochaOpenTelemetryBuilderExtensions
{
    /// <summary>
    /// Subscribes to message bus and mediator spans and to message bus metrics.
    /// This does not register the bus or mediator, enable their instrumentation, or configure an exporter.
    /// </summary>
    /// <param name="builder">The OpenTelemetry builder.</param>
    /// <returns>The OpenTelemetry builder for configuration chaining.</returns>
    public static OpenTelemetryBuilder AddMochaInstrumentation(this OpenTelemetryBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .WithTracing(tracing => tracing.AddMochaInstrumentation().AddSource(MochaTelemetryNames.Mediator))
            .WithMetrics(metrics => metrics.AddMochaInstrumentation());
    }
}
