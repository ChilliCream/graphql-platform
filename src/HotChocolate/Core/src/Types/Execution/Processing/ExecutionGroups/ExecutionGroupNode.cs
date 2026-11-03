using System.Collections.Immutable;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// The position of a selection in a resolved execution group.
/// </summary>
internal readonly struct ExecutionGroupNode
{
    internal ExecutionGroupNode(int slotIndex, ImmutableArray<int> slotsBelow)
    {
        SlotIndex = slotIndex;
        SlotsBelow = slotsBelow;
    }

    /// <summary>
    /// Gets the index of the slot of this selection, or <c>-1</c> if the selection only leads to members.
    /// </summary>
    public int SlotIndex { get; }

    /// <summary>
    /// Gets the indexes of the slots below this selection that are released when it resolves to null or fails.
    /// </summary>
    public ImmutableArray<int> SlotsBelow { get; }
}
