namespace Mocha;

/// <summary>
/// Provides context for constructing a consumer middleware pipeline, including the service provider and the consumer being configured.
/// </summary>
public class ConsumerMiddlewareFactoryContext
{
    /// <summary>
    /// Gets the service provider for resolving dependencies.
    /// </summary>
    public required IServiceProvider Services { get; init; }

    /// <summary>
    /// Gets the consumer that this middleware pipeline is being built for.
    /// </summary>
    public required Consumer Consumer { get; init; }
}
