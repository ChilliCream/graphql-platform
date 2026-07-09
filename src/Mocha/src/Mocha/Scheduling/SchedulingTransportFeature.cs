namespace Mocha.Scheduling;

/// <summary>
/// Feature that contains metadata about the transport's scheduling capabilities.
/// </summary>
public sealed class SchedulingTransportFeature
{
    /// <summary>
    /// Indicates that the transport supports native scheduling, and that the dispatch scheduling
    /// middleware should be skipped.
    /// </summary>
    public bool SupportsSchedulingNatively { get; set; }
}
