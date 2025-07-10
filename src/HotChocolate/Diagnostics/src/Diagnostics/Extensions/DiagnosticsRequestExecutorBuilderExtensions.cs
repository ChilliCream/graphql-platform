using System.Text;
using HotChocolate.Diagnostics;
using HotChocolate.Diagnostics.Listeners;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides instrumentation extensions to the <see cref="IRequestExecutorBuilder"/>.
/// </summary>
public static class DiagnosticsRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds instrumentation to a schema that can be used for open-telemetry.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="options">
    /// A delegate to modify the instrumentation options.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    public static IRequestExecutorBuilder AddInstrumentation(
        this IRequestExecutorBuilder builder,
        Action<InstrumentationOptions>? options = null)
        => AddInstrumentation(builder, (_, opt) => options?.Invoke(opt));

    /// <summary>
    /// Adds instrumentation to a schema that can be used for open-telemetry.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="options">
    /// A delegate to modify the instrumentation options.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    public static IRequestExecutorBuilder AddInstrumentation(
        this IRequestExecutorBuilder builder,
        Action<IServiceProvider, InstrumentationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        builder.Services.TryAddSingleton(
            sp =>
            {
                var optionInst = new InstrumentationOptions();
                options(sp, optionInst);
                return optionInst;
            });

        builder.Services.TryAddSingleton<InternalActivityEnricher>();

        builder.AddDiagnosticEventListener(
            sp => new ActivityExecutionDiagnosticListener(
                sp.GetService<ActivityEnricher>() ??
                    sp.GetRequiredService<InternalActivityEnricher>(),
                sp.GetRequiredService<InstrumentationOptions>()));

        builder.AddDiagnosticEventListener(
            sp => new ActivityServerDiagnosticListener(
                sp.GetService<ActivityEnricher>() ??
                    sp.GetRequiredService<InternalActivityEnricher>(),
                sp.GetRequiredService<InstrumentationOptions>()));

        builder.AddDiagnosticEventListener(
            sp => new ActivityDataLoaderDiagnosticListener(
                sp.GetService<ActivityEnricher>() ??
                    sp.GetRequiredService<InternalActivityEnricher>(),
                sp.GetRequiredService<InstrumentationOptions>()));

        return builder;
    }

    private sealed class InternalActivityEnricher : ActivityEnricher
    {
        public InternalActivityEnricher(
            ObjectPool<StringBuilder> stringBuilderPoolPool,
            InstrumentationOptions options)
            : base(stringBuilderPoolPool, options)
        {
        }
    }
}
