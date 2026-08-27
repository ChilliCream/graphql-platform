using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents a field selection node with its path include flags and delivery group.
/// </summary>
/// <param name="Node">
/// The syntax node that represents the field selection.
/// </param>
/// <param name="PathIncludeFlags">
/// The flags that must be all set for this selection to be included.
/// </param>
/// <param name="DeliveryGroup">
/// The delivery group context this field was collected under, or <c>null</c>
/// if the field is not inside a deferred fragment.
/// </param>
/// <param name="PathIncludeFlagsOverflow">
/// The overflow words of the path include flags for condition indexes 64 and above,
/// or <c>null</c> if all conditions on this path sit below 64.
/// </param>
public sealed record FieldSelectionNode(
    FieldNode Node,
    ulong PathIncludeFlags,
    DeliveryGroup? DeliveryGroup = null,
    ulong[]? PathIncludeFlagsOverflow = null);
