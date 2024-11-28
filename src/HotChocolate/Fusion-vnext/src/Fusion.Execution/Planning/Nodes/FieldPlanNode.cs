using System.Diagnostics;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

public sealed class FieldPlanNode : SelectionPlanNode
{
    private List<ArgumentAssignment>? _arguments;

    public FieldPlanNode(
        FieldNode fieldNode,
        OutputFieldInfo field)
        : base(field.Type.NamedType(), fieldNode.SelectionSet?.Selections)
    {
        FieldNode = fieldNode;
        Field = field;
        ResponseName = FieldNode.Alias?.Value ?? field.Name;

        foreach (var argument in fieldNode.Arguments)
        {
            AddArgument(new ArgumentAssignment(argument.Name.Value, argument.Value));
        }
    }

    public FieldPlanNode(
        FieldNode fieldNode,
        CompositeOutputField field)
        : this(fieldNode, new OutputFieldInfo(field))
    {
    }

    public string ResponseName { get; }

    public FieldNode FieldNode { get; }

    public OutputFieldInfo Field { get; }

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
