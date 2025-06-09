namespace HotChocolate.Execution;

/// <summary>
/// Represents a configuration for a request middleware.
/// </summary>
/// <param name="Middleware">
/// The middleware to be executed.
/// </param>
/// <param name="Key">
/// The key of the middleware.
/// </param>
public sealed record RequestMiddlewareConfiguration(
    RequestMiddleware Middleware,
    string? Key = null);
