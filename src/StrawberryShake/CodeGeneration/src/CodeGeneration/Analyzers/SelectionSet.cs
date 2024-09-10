using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers;

public class SelectionSet
{
    public SelectionSet(
        INamedType type,
        SelectionSetNode syntaxNode,
        IReadOnlyList<FieldSelection> fields,
        IReadOnlyList<FragmentNode> fragmentNodes)
    {
        Type = type;
        SyntaxNode = syntaxNode;
        Fields = fields;
        FragmentNodes = fragmentNodes;
    }

    public INamedType Type { get; }

    public SelectionSetNode SyntaxNode { get; }

    public IReadOnlyList<FieldSelection> Fields { get; }

    public IReadOnlyList<FragmentNode> FragmentNodes { get; }
}
