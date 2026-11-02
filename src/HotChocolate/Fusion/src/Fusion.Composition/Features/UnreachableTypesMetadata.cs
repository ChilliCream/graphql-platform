using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Features;

/// <summary>
/// The object and interface types that were removed from a source schema because they are not
/// reachable from its root types.
/// </summary>
internal sealed class UnreachableTypesMetadata(IReadOnlyList<MutableComplexTypeDefinition> types)
{
    /// <summary>
    /// Gets the removed types, in schema order.
    /// </summary>
    public IReadOnlyList<MutableComplexTypeDefinition> Types { get; } = types;
}
