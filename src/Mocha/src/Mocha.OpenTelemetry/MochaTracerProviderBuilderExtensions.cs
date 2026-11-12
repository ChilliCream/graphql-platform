namespace OpenTelemetry.Trace;

/// <summary>
/// Provides extension methods for configuring Mocha tracing on a <see cref="TracerProviderBuilder"/>.
/// </summary>
public static class MochaTracerProviderBuilderExtensions
{
    /// <summary>
    /// Subscribes the tracer provider to the spans of the Mocha message bus.
    /// </summary>
    /// <param name="builder">The tracer provider builder.</param>
    /// <returns>The tracer provider builder for configuration chaining.</returns>
    public static TracerProviderBuilder AddMochaInstrumentation(this TracerProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddSource(Mocha.OpenTelemetry.Source.Name);
    }
}
