using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;
using static System.StringComparison;

namespace HotChocolate.Fusion.Planning;

public interface IOperationNode
{
    ISyntaxNode ToSyntaxNode();
}

public sealed class SelectionSet : IOperationNode
{

    public SelectionSetNode ToSyntaxNode()
    {

    }

    ISyntaxNode IOperationNode.ToSyntaxNode()
        => ToSyntaxNode();
}

public interface ISelection : IOperationNode
{
    SelectionSet? SelectionSet { get; }

    DirectiveCollection Directives { get; }
}

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
            ResponseName.Equals(Field.Name, Ordinal)
                ? null
                : new NameNode(ResponseName),
            Directives.ToSyntaxNodes(),
            Arguments.ToSyntaxNodes(),
            SelectionSet?.ToSyntaxNode());
    }

    ISyntaxNode IOperationNode.ToSyntaxNode()
        => ToSyntaxNode();
}

public sealed class InlineFragment : ISelection
{
    public ICompositeNamedType Type { get; }

    public DirectiveCollection Directives { get; }

    public SelectionSet SelectionSet { get; }

    public ISyntaxNode ToSyntaxNode()
    {
        throw new NotImplementedException();
    }
}
