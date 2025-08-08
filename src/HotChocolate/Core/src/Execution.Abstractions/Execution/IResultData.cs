namespace HotChocolate.Execution;

public interface IResultData : IResultDataJsonFormatter
{
    /// <summary>
    /// Gets the parent result data object.
    /// </summary>
    public IResultData? Parent { get; protected set; }

    /// <summary>
    /// Gets the index under which this data is stored in the parent result.
    /// </summary>
    public int ParentIndex { get; protected set; }

    /// <summary>
    /// Defines that this result was invalidated by one task and can be discarded.
    /// </summary>
    public bool IsInvalidated { get; set; }

    /// <summary>
    /// Gets an internal ID that tracks result objects.
    /// In most cases, this id is 0. But if this result object has
    /// significance for deferred work, it will get assigned a proper id which
    /// allows us to efficiently track if this result was deleted due to
    /// non-null propagation.
    /// </summary>
    public uint PatchId { get; set; }

    /// <summary>
    /// Gets an internal patch path that specifies from where this result was branched of.
    /// </summary>
    public Path? PatchPath { get; set; }

    /// <summary>
    /// Connects this result to the parent result.
    /// </summary>
    /// <param name="parent">
    /// The parent result.
    /// </param>
    /// <param name="index">
    /// The index under which this result is stored in the parent result.
    /// </param>
    public void SetParent(IResultData parent, int index);
}
