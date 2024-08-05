using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public interface ISyntaxNodeProvider
{
    ISyntaxNode SyntaxNode { get; }
}

public interface ISelection
    : ISyntaxNodeProvider;

public interface ISelectionSet
    : ISyntaxNodeProvider
    , IReadOnlyList<ISelection>;

public sealed class OperationDefinition
{
    public ISyntaxNode SyntaxNode { get; }

    public string Name { get; }

    public CompositeObjectType OperationType { get; }

    public ISelectionSet SelectionSet { get; }
}

public sealed class FieldSelection(
    ISyntaxNode syntaxNode,
    string responseName,
    ICompositeOutputField field,
    IReadOnlyList<DirectiveNode> directives,
    IReadOnlyList<ArgumentAssignment> arguments,
    ISelectionSet? selectionSet)
    : ISelection
{
    public ISyntaxNode SyntaxNode { get; } = syntaxNode;

    public string ResponseName { get; } = responseName;

    public ICompositeOutputField Field { get; } = field;

    public IReadOnlyList<DirectiveNode> Directives { get; } = directives;

    public IReadOnlyList<ArgumentAssignment> Arguments { get; } = arguments;

    public ISelectionSet? SelectionSet { get; } = selectionSet;
}

public class FragmentSpread : ISelection
{
    public ISyntaxNode SyntaxNode { get; }

    public IReadOnlyList<DirectiveNode> Directives { get; }

    public FragmentDefinition Fragment { get; }

}

public class InlineDefinition : ISelection
{
    public ISyntaxNode SyntaxNode { get; }

    public ICompositeNamedType? TypeCondition { get; }

    public IReadOnlyList<DirectiveNode> Directives { get; }

    public ISelectionSet SelectionSet { get; }
}

public class FragmentDefinition : ISelection
{

    public ISyntaxNode SyntaxNode { get; }

    public string Name { get; }

    public ICompositeNamedType TypeCondition { get; }

    public IReadOnlyList<DirectiveNode> Directives { get; }

    public ISelectionSet SelectionSet { get; }
}


