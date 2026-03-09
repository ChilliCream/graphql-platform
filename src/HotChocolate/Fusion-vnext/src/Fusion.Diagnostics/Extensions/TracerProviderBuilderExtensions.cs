using HotChocolate.Fusion.Diagnostics;

namespace OpenTelemetry.Trace;

/// <summary>
/// Provides configuration methods to open-telemetry.
/// </summary>
public static class TracerProviderBuilderExtensions
{
    /// <summary>
    /// Adds the Hot Chocolate Fusion instrumentation to open-telemetry.
    /// </summary>
    /// <param name="builder">
    /// The tracing builder.
    /// </param>
    /// <returns>
    /// Returns the tracing builder for configuration chaining.
    /// </returns>
    public static TracerProviderBuilder AddHotChocolateFusionInstrumentation(
        this TracerProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddSource(HotChocolateFusionActivitySource.GetName());
        return builder;
    }
}
