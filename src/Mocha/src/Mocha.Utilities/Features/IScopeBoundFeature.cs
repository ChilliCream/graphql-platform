namespace Mocha.Features;

/// <summary>
/// A feature that caches state resolved from the current service scope and must drop it when the
/// scope it was resolved from is replaced.
/// </summary>
public interface IScopeBoundFeature
{
    /// <summary>
    /// Drops state resolved from the previous service scope so it is resolved again from the current one.
    /// </summary>
    void ResetScope();
}
