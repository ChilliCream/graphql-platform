namespace Mocha.Mediator;

/// <summary>
/// Provides access to mediator configuration state during descriptor construction.
/// </summary>
public interface IMediatorConfigurationContext : IFeatureProvider
{
    /// <summary>
    /// Gets the service provider used for resolving configuration-time dependencies.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Gets the mediator options.
    /// </summary>
    MediatorOptions Options { get; }
}
