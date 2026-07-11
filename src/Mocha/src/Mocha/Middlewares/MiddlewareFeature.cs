namespace Mocha;

/// <summary>
/// An immutable record containing the compiled middleware pipeline configurations and modifiers for dispatch, receive, and consumer pipelines.
/// </summary>
/// <param name="DispatchMiddlewares">The registered dispatch middleware configurations.</param>
/// <param name="DispatchPipelineModifiers">The modifiers for the dispatch middleware pipeline.</param>
/// <param name="ReceiveMiddlewares">The registered receive middleware configurations.</param>
/// <param name="ReceivePipelineModifiers">The modifiers for the receive middleware pipeline.</param>
/// <param name="HandlerMiddlewares">The registered consumer middleware configurations.</param>
/// <param name="HandlerPipelineModifiers">The modifiers for the consumer middleware pipeline.</param>
public sealed record MiddlewareFeature(
    IReadOnlyList<DispatchMiddlewareConfiguration> DispatchMiddlewares,
    IReadOnlyList<Action<List<DispatchMiddlewareConfiguration>>> DispatchPipelineModifiers,
    IReadOnlyList<ReceiveMiddlewareConfiguration> ReceiveMiddlewares,
    IReadOnlyList<Action<List<ReceiveMiddlewareConfiguration>>> ReceivePipelineModifiers,
    IReadOnlyList<ConsumerMiddlewareConfiguration> HandlerMiddlewares,
    IReadOnlyList<Action<List<ConsumerMiddlewareConfiguration>>> HandlerPipelineModifiers);
