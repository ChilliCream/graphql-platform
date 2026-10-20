using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Provides the built-in consumer middleware configurations that form the default consumer pipeline.
/// </summary>
public static class ConsumerMiddlewares
{
    /// <summary>
    /// The retry middleware configuration that retries failed handler invocations with configurable backoff.
    /// </summary>
    public static readonly ConsumerMiddlewareConfiguration Retry = ConsumerRetryMiddleware.Create();

    /// <summary>
    /// The deferred dispatch middleware configuration that holds the messages a consumer dispatches until the
    /// consumer attempt completes.
    /// </summary>
    public static readonly ConsumerMiddlewareConfiguration DeferredDispatch =
        DeferredDispatchConsumeMiddleware.Create();

    /// <summary>
    /// The instrumentation middleware configuration that emits telemetry for consumer operations.
    /// </summary>
    public static readonly ConsumerMiddlewareConfiguration Instrumentation = ConsumerInstrumentationMiddleware.Create();
}
