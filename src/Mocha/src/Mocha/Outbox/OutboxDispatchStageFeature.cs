namespace Mocha.Outbox;

/// <summary>
/// Adds the outbox middleware to the dispatch pipeline of every dispatch endpoint of the bus, directly after
/// <see cref="DispatchMiddlewares.Scheduling"/>.
/// </summary>
/// <param name="Middleware">The outbox middleware configuration.</param>
internal sealed record OutboxDispatchStageFeature(DispatchMiddlewareConfiguration Middleware);
