using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class Selection : ISelection
{
    public Selection(
        string responseName,
        CompositeOutputField field,
        SelectionSet? selectionSet,
        DirectiveCollection directives,
        ArgumentAssignmentCollection arguments)
    {
        ResponseName = responseName;
        Field = field;
        SelectionSet = selectionSet;
        Directives = directives;
        Arguments = arguments;
    }

    public string ResponseName { get; }

    public CompositeOutputField Field { get; }

    public SelectionSet? SelectionSet { get; }

    public DirectiveCollection Directives { get; }

    public ArgumentAssignmentCollection Arguments { get; }

    public FieldNode ToSyntaxNode()
    {
        return new FieldNode(
            null,
            new NameNode(ResponseName),
            ResponseName.Equals(Field.Name, StringComparison.Ordinal)
                ? null
                : new NameNode(ResponseName),
            Directives.ToSyntaxNodes(),
            Arguments.ToSyntaxNodes(),
            SelectionSet?.ToSyntaxNode());
    }

    ISelectionNode ISelection.ToSyntaxNode()
        => ToSyntaxNode();

    ISyntaxNode IOperationNode.ToSyntaxNode()
        => ToSyntaxNode();
}
