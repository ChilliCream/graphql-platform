using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mocha;

/// <summary>
/// Provides extension methods for adding diagnostics and instrumentation
/// to the messaging pipeline via <see cref="IMessageBusHostBuilder"/>.
/// </summary>
public static class InstrumentationBusExtensions
{
    /// <summary>
    /// Adds the default OpenTelemetry-compatible diagnostic event listener to the messaging pipeline.
    /// </summary>
    public static IMessageBusHostBuilder AddInstrumentation(this IMessageBusHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddDiagnosticEventListener<ActivityMessagingDiagnosticListener>();

        return builder;
    }

    /// <summary>
    /// Registers a custom <see cref="IMessagingDiagnosticEventListener"/> implementation.
    /// </summary>
    public static IMessageBusHostBuilder AddDiagnosticEventListener<T>(this IMessageBusHostBuilder builder)
        where T : class, IMessagingDiagnosticEventListener
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<T>();
        builder.Services.AddSingleton<IMessagingDiagnosticEventListener>(sp => sp.GetRequiredService<T>());

        return builder;
    }

    /// <summary>
    /// Registers a diagnostic event listener instance.
    /// </summary>
    public static IMessageBusHostBuilder AddDiagnosticEventListener(
        this IMessageBusHostBuilder builder,
        IMessagingDiagnosticEventListener listener)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(listener);

        builder.Services.AddSingleton(listener);

        return builder;
    }
}
