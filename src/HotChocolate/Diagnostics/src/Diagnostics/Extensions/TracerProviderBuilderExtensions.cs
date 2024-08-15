using HotChocolate.Diagnostics;

namespace OpenTelemetry.Trace;

/// <summary>
/// Provides configuration methods to open-telemetry.
/// </summary>
public static class TracerProviderBuilderExtensions
{
    /// <summary>
    /// Adds the Hot Chocolate instrumentation to open-telemetry.
    /// </summary>
    /// <param name="builder">
    /// The tracing builder.
    /// </param>
    /// <returns>
    /// Returns the tracing builder for configuration chaining.
    /// </returns>
    public static TracerProviderBuilder AddHotChocolateInstrumentation(
        this TracerProviderBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddSource(HotChocolateActivitySource.GetName());
        return builder;
    }
}
