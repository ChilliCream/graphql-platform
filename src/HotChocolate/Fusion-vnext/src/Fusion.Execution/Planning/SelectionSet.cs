using System.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

[DebuggerDisplay("Path = {Path}, Node = {Node}")]
public record SelectionSet(
    uint Id,
    SelectionSetNode Node,
    ITypeDefinition Type,
    SelectionPath Path)
{
    public IReadOnlyList<ISelectionNode> Selections => Node.Selections;
}
