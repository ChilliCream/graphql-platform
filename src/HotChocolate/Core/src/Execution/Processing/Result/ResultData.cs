namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a result data object like an object or list.
/// </summary>
public abstract class ResultData
{
    /// <summary>
    /// Gets the parent result data object.
    /// </summary>
    protected internal ResultData? Parent { get; protected set; }

    /// <summary>
    /// Gets the index under which this data is stored in the parent result.
    /// </summary>
    protected internal int ParentIndex { get; protected set; }

    /// <summary>
    /// Defines that this result was invalidated by one task and can be discarded.
    /// </summary>
    protected internal bool IsInvalidated { get; set; }

    /// <summary>
    /// Gets an internal ID that tracks result objects.
    /// In most cases this id is 0. But if this result object has
    /// significance for deferred work it will get assigned a proper id which
    /// allows us to efficiently track if this result was deleted due to a
    /// non-null propagation.
    /// </summary>
    public uint PatchId { get; set; }

    /// <summary>
    /// Gets an internal patch path that specifies from where this result was branched of.
    /// </summary>
    protected internal Path? PatchPath { get; set; }

    /// <summary>
    /// Connects this result to the parent result.
    /// </summary>
    /// <param name="parent">
    /// The parent result.
    /// </param>
    /// <param name="index">
    /// The index under which this result is stored in the parent result.
    /// </param>
    public void SetParent(ResultData parent, int index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        ParentIndex = index;
    }
}
