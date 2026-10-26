using Microsoft.Extensions.DependencyInjection;
using Mocha.Scheduling;

namespace Mocha;

/// <summary>
/// Provides the built-in dispatch middleware configurations.
/// </summary>
public static class DispatchMiddlewares
{
    /// <summary>
    /// Instruments dispatch operations and propagates trace headers.
    /// </summary>
    public static readonly DispatchMiddlewareConfiguration Instrumentation = new(
        static (context, next) => DispatchInstrumentationMiddleware.Create(
            context.Services.GetRequiredService<IMessagingDiagnosticEvents>(), next),
        "Instrumentation");

    /// <summary>
    /// Persists scheduled messages before transport dispatch.
    /// </summary>
    public static readonly DispatchMiddlewareConfiguration Scheduling = new(
        static (_, next) => DispatchSchedulingMiddleware.Create(next),
        "Scheduling");

    /// <summary>
    /// Serializes messages before transport dispatch.
    /// </summary>
    public static readonly DispatchMiddlewareConfiguration Serialization = new(
        static (_, next) => DispatchSerializerMiddleware.Create(next),
        "Serialization");
}
