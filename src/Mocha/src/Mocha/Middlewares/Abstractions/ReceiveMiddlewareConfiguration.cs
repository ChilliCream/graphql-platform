namespace Mocha;

/// <summary>
/// Holds a receive middleware factory delegate and a key used for identification in pipeline modification.
/// </summary>
/// <param name="Middleware">The receive middleware factory delegate.</param>
/// <param name="Key">The key for identifying this middleware in the pipeline.</param>
public sealed record ReceiveMiddlewareConfiguration(ReceiveMiddleware Middleware, string Key);
