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

    /// <summary>
    /// Gets a description of the mediator and its registered handlers for diagnostic and
    /// visualization purposes.
    /// </summary>
    /// <returns>A <see cref="MediatorDescription"/> describing the mediator.</returns>
    MediatorDescription Describe();
}
