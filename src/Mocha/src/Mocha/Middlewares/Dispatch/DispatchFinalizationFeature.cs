namespace Mocha;

/// <summary>
/// Configures message persistence between scheduling and transport dispatch.
/// </summary>
internal sealed record DispatchFinalizationFeature(DispatchMiddleware Middleware);
