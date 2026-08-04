namespace Mocha.Mediator;

/// <summary>
/// Holds a middleware factory and an optional key for ordering or identification.
/// </summary>
public sealed record MediatorMiddlewareConfiguration(MediatorMiddleware Middleware, string? Key = null);
