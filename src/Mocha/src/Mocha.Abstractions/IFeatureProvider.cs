namespace Mocha;

/// <summary>
/// An object that has features.
/// </summary>
public interface IFeatureProvider
{
    /// <summary>
    /// Gets the feature collection.
    /// </summary>
    IFeatureCollection Features { get; }
}
