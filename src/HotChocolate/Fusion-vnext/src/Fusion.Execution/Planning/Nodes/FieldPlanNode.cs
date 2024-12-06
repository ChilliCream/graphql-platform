using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

public sealed class FieldPlanNode : SelectionPlanNode
{
    private List<ArgumentAssignment>? _arguments;

    public FieldPlanNode(
        FieldNode fieldNode,
        OutputFieldInfo field)
        : base(field.Type.NamedType(), fieldNode.SelectionSet?.Selections, fieldNode.Directives)
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
        => _arguments ?? [];

    public void AddArgument(ArgumentAssignment argument)
    {
        ArgumentNullException.ThrowIfNull(argument);
        _arguments ??= [];
        _arguments.Add(argument);
    }

    public FieldNode ToSyntaxNode()
    {
        var directives = new List<DirectiveNode>(Directives.ToSyntaxNode());

        foreach (var condition in Conditions)
        {
            var directiveName = condition.PassingValue ? "include" : "skip";
            directives.Add(new DirectiveNode(directiveName,
                new ArgumentNode("if", new VariableNode(condition.VariableName))));
        }

        return new FieldNode(
            new NameNode(Field.Name),
            Field.Name.Equals(ResponseName) ? null : new NameNode(ResponseName),
            directives,
            Arguments.ToSyntaxNode(),
            Selections.Count == 0 ? null : Selections.ToSyntaxNode());
    }
}
