namespace HotChocolate.Execution.Processing;

/// <summary>
/// A member of a resolved execution group.
/// </summary>
/// <param name="Selection">The selection that is sequenced.</param>
/// <param name="Priority">The priority of the entry that matched the selection.</param>
/// <param name="IsStreamable">Defines whether the matching entry is marked as streamable.</param>
internal readonly record struct ExecutionGroupSlot(Selection Selection, int Priority, bool IsStreamable);
