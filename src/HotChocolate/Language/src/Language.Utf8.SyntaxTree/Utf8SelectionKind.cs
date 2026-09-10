namespace HotChocolate.Language;

/// <summary>
/// Specifies the kind of executable selection represented by a <see cref="Utf8SelectionNode"/>.
/// </summary>
public enum Utf8SelectionKind
{
    /// <summary>
    /// The selection is not initialized.
    /// </summary>
    None = 0,

    /// <summary>
    /// The selection is a field.
    /// </summary>
    Field = 1,

    /// <summary>
    /// The selection is a fragment spread.
    /// </summary>
    FragmentSpread = 2,

    /// <summary>
    /// The selection is an inline fragment.
    /// </summary>
    InlineFragment = 3
}
