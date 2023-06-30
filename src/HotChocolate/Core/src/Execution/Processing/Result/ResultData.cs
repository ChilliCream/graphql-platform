#nullable enable

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a result data object like an object or list.
/// </summary>
public abstract class ResultData
{
    /// <summary>
    /// Gets the parent result data object.
    /// </summary>
    internal ResultData? Parent { get; set; }

    /// <summary>
    /// Gets the index under which this data is stored in the parent result.
    /// </summary>
    internal int ParentIndex { get; set; }

    /// <summary>
    /// Defines that this result was invalidated by one task and can be discarded.
    /// </summary>
    internal bool IsInvalidated { get; set; }

    /// <summary>
    /// Gets an internal ID that tracks result objects.
    /// In most cases this id is 0. But if this result object has
    /// significance for deferred work it will get assigned a proper id which
    /// allows us to efficiently track if this result was deleted due to a
    /// non-null propagation.
    /// </summary>
    public uint PatchId { get; set; }
}
