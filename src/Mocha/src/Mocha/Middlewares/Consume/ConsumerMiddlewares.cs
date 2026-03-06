using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Provides the built-in consumer middleware configurations that form the default consumer pipeline.
/// </summary>
public static class ConsumerMiddlewares
{
    /// <summary>
    /// The instrumentation middleware configuration that emits telemetry for consumer operations.
    /// </summary>
    public static readonly ConsumerMiddlewareConfiguration Instrumentation = ConsumerInstrumentationMiddleware.Create();
}
