using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public class FieldPlanNode : SelectionPlanNode
{
    public FieldPlanNode(
        string responseName,
        CompositeOutputField field,
        SelectionSetNode? selectionSet)
        : base(field.Type.NamedType(), selectionSet?.Selections)
    {
        ResponseName = responseName;
        Field = field;
    }

    public string ResponseName { get; }

    public CompositeOutputField Field { get; }
}
