using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// A factory delegate that creates a dispatch middleware step by wrapping the next delegate in the pipeline.
/// </summary>
/// <param name="context">The factory context providing access to services, endpoint, and transport.</param>
/// <param name="next">The next delegate in the dispatch pipeline.</param>
/// <returns>A delegate that executes this middleware step.</returns>
public delegate DispatchDelegate DispatchMiddleware(DispatchMiddlewareFactoryContext context, DispatchDelegate next);
