namespace HotChocolate.Fusion.Execution.Results;

/// <summary>
/// Maps the key fields and the reused fields of a lookup onto the value rows of the objects
/// that one selection set describes.
/// </summary>
internal sealed class EntityReuseLayout(
    int selectionSetId,
    int[] keyRowOffsets,
    int[] fieldRowOffsets)
{
    /// <summary>
    /// Gets the id of the selection set whose objects this layout describes.
    /// </summary>
    public int SelectionSetId => selectionSetId;

    /// <summary>
    /// Gets the row offsets of the key fields in requirement order, from the first row of the object.
    /// </summary>
    public ReadOnlySpan<int> KeyRowOffsets => keyRowOffsets;

    /// <summary>
    /// Gets the row offsets of the reused fields in the order of
    /// <see cref="EntityReuseCandidateList.ResponseNames"/>, from the first row of the object.
    /// </summary>
    public ReadOnlySpan<int> FieldRowOffsets => fieldRowOffsets;

    /// <summary>
    /// Gets the largest row offset of <see cref="KeyRowOffsets"/> and <see cref="FieldRowOffsets"/>.
    /// </summary>
    public int MaxRowOffset { get; } = Math.Max(
        keyRowOffsets.Length == 0 ? 0 : keyRowOffsets.Max(),
        fieldRowOffsets.Length == 0 ? 0 : fieldRowOffsets.Max());
}
