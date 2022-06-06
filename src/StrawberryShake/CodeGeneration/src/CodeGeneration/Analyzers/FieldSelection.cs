using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers;

internal sealed class FieldSelection
{
    public FieldSelection(IOutputField field, FieldNode syntaxNode, Path path)
    {
        ResponseName = syntaxNode.Alias?.Value ?? syntaxNode.Name.Value;
        Field = field;
        SyntaxNode = syntaxNode;
        Path = path;
    }

    public string ResponseName { get; }

    public IOutputField Field { get; }

    public FieldNode SyntaxNode { get; }

    public Path Path { get; }
}
