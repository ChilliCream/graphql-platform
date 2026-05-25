using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mocha.Mediator;

/// <summary>
/// Provides extension methods for adding diagnostics and instrumentation
/// to the mediator pipeline via <see cref="IMediatorBuilder"/>.
/// </summary>
public static class MediatorBuilderInstrumentationExtensions
{
    /// <summary>
    /// Adds the default OpenTelemetry-compatible diagnostic event listener to the mediator pipeline.
    /// </summary>
    public static IMediatorBuilder AddInstrumentation(this IMediatorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddDiagnosticEventListener<ActivityMediatorDiagnosticListener>();

        return builder;
    }

    /// <summary>
    /// Registers a custom <see cref="IMediatorDiagnosticEventListener"/> implementation.
    /// </summary>
    public static IMediatorBuilder AddDiagnosticEventListener<T>(this IMediatorBuilder builder)
        where T : class, IMediatorDiagnosticEventListener
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureServices(static services =>
        {
            services.TryAddSingleton<T>();
            services.AddSingleton<IMediatorDiagnosticEventListener>(static sp => sp.GetRequiredService<T>());
        });

        return builder;
    }

    /// <summary>
    /// Registers a diagnostic event listener instance.
    /// </summary>
    public static IMediatorBuilder AddDiagnosticEventListener(
        this IMediatorBuilder builder,
        IMediatorDiagnosticEventListener listener)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(listener);

        builder.ConfigureServices(services => services.AddSingleton(listener));

        return builder;
    }
}
