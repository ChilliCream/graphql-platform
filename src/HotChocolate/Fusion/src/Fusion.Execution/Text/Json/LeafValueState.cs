namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// Classifies a result slot for a leaf value assignment.
/// </summary>
internal enum LeafValueState
{
    /// <summary>
    /// The slot holds a non-null leaf value that can be assigned to another slot.
    /// </summary>
    Assignable,

    /// <summary>
    /// The slot is unset, null, an object or a list.
    /// </summary>
    NotALeaf,

    /// <summary>
    /// The slot holds a value that cannot be assigned, such as a reference.
    /// </summary>
    Unsupported
}
