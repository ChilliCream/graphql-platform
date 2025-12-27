using HotChocolate.Language;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a field selection node with its path include flags.
/// </summary>
/// <param name="Node">
/// The syntax node that represents the field selection.
/// </param>
/// <param name="PathIncludeFlags">
/// The flags that must be all set for this selection to be included.
/// </param>
public sealed record FieldSelectionNode(FieldNode Node, ulong PathIncludeFlags);
