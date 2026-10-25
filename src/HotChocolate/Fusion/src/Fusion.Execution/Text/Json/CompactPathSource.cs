namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// Creates the paths of a <see cref="CompactPathSegment"/> when they are first accessed.
/// </summary>
internal abstract class CompactPathSource
{
    /// <summary>
    /// Gets the paths of every segment that shares this source, in segment order.
    /// </summary>
    public abstract CompactPath[] GetPaths();
}
