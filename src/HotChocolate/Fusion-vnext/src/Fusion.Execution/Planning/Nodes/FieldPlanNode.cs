using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class FieldPlanNode : SelectionPlanNode
{
    public FieldPlanNode(
        FieldNode fieldNode,
        CompositeOutputField field)
        : base(field.Type.NamedType(), fieldNode.SelectionSet?.Selections)
    {
        FieldNode = fieldNode;
        Field = field;
        ResponseName = FieldNode.Alias?.Value ?? field.Name;
    }

    public string ResponseName { get; }

    public FieldNode FieldNode { get; }

    public CompositeOutputField Field { get; }
}
