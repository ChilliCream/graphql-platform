namespace HotChocolate.Execution;

/// <summary>
/// Represents a configuration for a request middleware.
/// </summary>
/// <param name="Middleware">The middleware to be executed.</param>
/// <param name="Key">The unique key of the middleware.</param>
/// <returns>A new instance of <see cref="RequestCoreMiddlewareConfiguration"/>.</returns>
public sealed record RequestCoreMiddlewareConfiguration(
    RequestCoreMiddleware Middleware,
    string? Key = null);
