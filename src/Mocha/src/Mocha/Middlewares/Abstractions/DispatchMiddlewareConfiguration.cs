namespace Mocha;

/// <summary>
/// Holds a dispatch middleware factory delegate and an optional key used for identification in pipeline modification.
/// </summary>
/// <param name="Middleware">The dispatch middleware factory delegate.</param>
/// <param name="Key">An optional key for identifying this middleware in the pipeline.</param>
public sealed record DispatchMiddlewareConfiguration(DispatchMiddleware Middleware, string? Key = null);
