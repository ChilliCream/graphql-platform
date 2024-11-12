using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public class Operation : IOperationNode
{
    public Operation(
        string name,
        OperationType kind,
        CompositeObjectType type,
        SourceObjectType sourceType,
        SelectionSet selectionSet,
        DirectiveCollection directives)
    {
        Name = name;
        Kind = kind;
        Type = type;
        SourceType = sourceType;
        SelectionSet = selectionSet;
        Directives = directives;
    }

    public string Name { get; set; }

    public OperationType Kind { get; }

    public CompositeObjectType Type { get; }

    public SourceObjectType SourceType { get; }
    public SelectionSet SelectionSet { get; }

    public DirectiveCollection Directives { get; }

    public OperationDefinitionNode ToSyntaxNode()
    {
        return new OperationDefinitionNode(
            null,
            new NameNode(Name),
            Kind,
            Array.Empty<VariableDefinitionNode>(),
            Directives.ToSyntaxNodes(),
            SelectionSet.ToSyntaxNode());
    }

    ISyntaxNode IOperationNode.ToSyntaxNode()
    {
        throw new NotImplementedException();
    }
}
