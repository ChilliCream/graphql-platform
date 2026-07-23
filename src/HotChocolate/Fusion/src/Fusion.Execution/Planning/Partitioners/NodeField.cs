using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Partitioners;

internal sealed class NodeField
{
    public required FieldNode Field { get; init; }

    public bool IsPlural { get; init; }

    public required InlineFragmentNode[]? ParentFragments { get; init; }
}
