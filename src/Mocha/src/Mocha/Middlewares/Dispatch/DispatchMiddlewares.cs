namespace Mocha;

/// <summary>
/// Provides the built-in dispatch middleware configurations that form the default dispatch pipeline.
/// </summary>
public static class DispatchMiddlewares
{
    /// <summary>
    /// The instrumentation middleware configuration that emits telemetry for dispatch operations.
    /// </summary>
    public static readonly DispatchMiddlewareConfiguration Instrumentation = DispatchInstrumentationMiddleware.Create();

    /// <summary>
    /// The serialization middleware configuration that serializes messages into transport envelopes.
    /// </summary>
    public static readonly DispatchMiddlewareConfiguration Serialization = DispatchSerializerMiddleware.Create();
}
