namespace Mocha;

/// <summary>
/// Holds a consumer middleware factory delegate and an optional key used for identification in pipeline modification.
/// </summary>
/// <param name="Middleware">The consumer middleware factory delegate.</param>
/// <param name="Key">An optional key for identifying this middleware in the pipeline.</param>
public sealed record ConsumerMiddlewareConfiguration(ConsumerMiddleware Middleware, string? Key = null);
