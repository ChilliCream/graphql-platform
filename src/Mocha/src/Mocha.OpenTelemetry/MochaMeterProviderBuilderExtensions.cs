namespace OpenTelemetry.Metrics;

/// <summary>
/// Provides extension methods for configuring Mocha metrics on a <see cref="MeterProviderBuilder"/>.
/// </summary>
public static class MochaMeterProviderBuilderExtensions
{
    /// <summary>
    /// Subscribes the meter provider to the metrics of the Mocha message bus.
    /// </summary>
    /// <param name="builder">The meter provider builder.</param>
    /// <returns>The meter provider builder for configuration chaining.</returns>
    public static MeterProviderBuilder AddMochaInstrumentation(this MeterProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddMeter(Mocha.OpenTelemetry.Meter.Name);
    }
}
