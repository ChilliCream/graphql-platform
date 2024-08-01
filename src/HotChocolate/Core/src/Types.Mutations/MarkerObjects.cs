namespace HotChocolate.Types;

/// <summary>
/// The null success marker is to signal to the execution engine
/// that the result is actually an success result although
/// the result is null.
/// </summary>
internal sealed class NullMarker
{
    private NullMarker()
    {
        // intentionally left empty
    }

    /// <summary>
    /// Gets the null success result marker.
    /// </summary>
    public static NullMarker Instance { get; } = new();
}
