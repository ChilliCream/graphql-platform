namespace Mocha;

/// <summary>
/// A factory delegate that creates a consumer middleware step by wrapping the next delegate in the pipeline.
/// </summary>
/// <param name="context">The factory context providing access to services and the consumer.</param>
/// <param name="next">The next delegate in the consumer pipeline.</param>
/// <returns>A delegate that executes this middleware step.</returns>
public delegate ConsumerDelegate ConsumerMiddleware(ConsumerMiddlewareFactoryContext context, ConsumerDelegate next);
