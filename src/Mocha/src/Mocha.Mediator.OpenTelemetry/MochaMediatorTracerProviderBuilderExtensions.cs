using Mocha.Mediator;

namespace OpenTelemetry.Trace;

/// <summary>
/// Provides extension methods for configuring Mocha mediator tracing on a <see cref="TracerProviderBuilder"/>.
/// </summary>
public static class MochaMediatorTracerProviderBuilderExtensions
{
    /// <summary>
    /// Subscribes the tracer provider to the spans of the Mocha mediator.
    /// </summary>
    /// <param name="builder">The tracer provider builder.</param>
    /// <returns>The tracer provider builder for configuration chaining.</returns>
    public static TracerProviderBuilder AddMochaMediatorInstrumentation(this TracerProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddSource(MochaMediatorActivitySource.Source.Name);
    }
}
