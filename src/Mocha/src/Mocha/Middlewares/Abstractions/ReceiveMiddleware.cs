namespace Mocha;

/// <summary>
/// A factory delegate that creates a receive middleware step by wrapping the next delegate in the pipeline.
/// </summary>
/// <param name="context">The factory context providing access to services, endpoint, and transport.</param>
/// <param name="next">The next delegate in the receive pipeline.</param>
/// <returns>A delegate that executes this middleware step.</returns>
public delegate ReceiveDelegate ReceiveMiddleware(ReceiveMiddlewareFactoryContext context, ReceiveDelegate next);
