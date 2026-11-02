using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Policies;

/// <summary>
/// A denied selection with the reason and the first descriptor that denied it.
/// </summary>
/// <param name="Selection">
/// The denied selection.
/// </param>
/// <param name="Kind">
/// The reason the selection is denied.
/// </param>
/// <param name="Descriptor">
/// The first descriptor in evaluation order that did not allow the selection.
/// </param>
internal readonly record struct SelectionDenial(
    Selection Selection,
    AuthorizationDenialKind Kind,
    PolicyDescriptor Descriptor);
