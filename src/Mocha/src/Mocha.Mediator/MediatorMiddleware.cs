namespace Mocha.Mediator;

/// <summary>
/// A factory delegate that wraps a <see cref="MediatorDelegate"/> with middleware logic.
/// The factory receives context for resolving services and the next delegate in the pipeline.
/// </summary>
public delegate MediatorDelegate MediatorMiddleware(MediatorMiddlewareFactoryContext context, MediatorDelegate next);
