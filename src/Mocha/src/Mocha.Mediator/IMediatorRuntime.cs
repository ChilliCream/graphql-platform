namespace Mocha.Mediator;

/// <summary>
/// Represents the fully initialized mediator runtime, providing access to
/// the feature collection and compiled pipelines.
/// </summary>
public interface IMediatorRuntime
{
    /// <summary>
    /// Gets the read-only feature collection for this mediator runtime.
    /// </summary>
    IFeatureCollection Features { get; }
}
