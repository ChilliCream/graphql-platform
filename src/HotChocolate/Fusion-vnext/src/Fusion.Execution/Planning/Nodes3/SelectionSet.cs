using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes3;

public record SelectionSet(
    uint Id,
    SelectionSetNode Node,
    ICompositeNamedType Type,
    SelectionPath Path)
{
    public IReadOnlyList<ISelectionNode> Selections => Node.Selections;
}

public record FieldSelection(
    uint SelectionSetId,
    FieldNode Node,
    CompositeOutputField Field,
    SelectionPath Path);
