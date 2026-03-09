using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers;

public class SelectionSet(
    ITypeDefinition type,
    SelectionSetNode syntaxNode,
    IReadOnlyList<FieldSelection> fields,
    IReadOnlyList<FragmentNode> fragmentNodes)
{
    public ITypeDefinition Type { get; } = type;

    public SelectionSetNode SyntaxNode { get; } = syntaxNode;

    public IReadOnlyList<FieldSelection> Fields { get; } = fields;

    public IReadOnlyList<FragmentNode> FragmentNodes { get; } = fragmentNodes;
}
