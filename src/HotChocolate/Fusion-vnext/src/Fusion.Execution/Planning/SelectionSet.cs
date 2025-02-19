using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning.Nodes3;

public record SelectionSet(
    uint Id,
    SelectionSetNode Node,
    ITypeDefinition Type,
    SelectionPath Path)
{
    public IReadOnlyList<ISelectionNode> Selections => Node.Selections;
}

public record FieldSelection(
    uint SelectionSetId,
    FieldNode Node,
    FusionOutputFieldDefinition Field,
    SelectionPath Path);
