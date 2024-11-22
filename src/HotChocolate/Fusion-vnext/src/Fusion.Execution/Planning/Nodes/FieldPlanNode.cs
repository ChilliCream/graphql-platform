using System.Collections.Immutable;
using System.Diagnostics;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

public sealed class FieldPlanNode : SelectionPlanNode
{
    private List<ArgumentAssignment>? _arguments;

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

    public IReadOnlyList<ArgumentAssignment> Arguments
        => _arguments ?? (IReadOnlyList<ArgumentAssignment>)Array.Empty<ArgumentAssignment>();

    public void AddArgument(ArgumentAssignment argument)
    {
        ArgumentNullException.ThrowIfNull(argument);
        _arguments ??= [];
        _arguments.Add(argument);
    }

    public FieldNode ToSyntaxNode()
    {
        return new FieldNode(
            new NameNode(Field.Name),
            Field.Name.Equals(ResponseName) ? null : new NameNode(ResponseName),
            Directives.ToSyntaxNode(),
            Arguments.ToSyntaxNode(),
            Selections.Count == 0 ? null : Selections.ToSyntaxNode());
    }
}
