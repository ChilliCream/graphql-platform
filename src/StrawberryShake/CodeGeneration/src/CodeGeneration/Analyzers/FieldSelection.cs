using HotChocolate.Language;
using HotChocolate.Types;
using Path = HotChocolate.Path;

namespace StrawberryShake.CodeGeneration.Analyzers;

public class FieldSelection
{
    public FieldSelection(
        IOutputField field,
        FieldNode syntaxNode,
        Path path,
        bool isConditional = true)
    {
        ResponseName = syntaxNode.Alias?.Value ?? syntaxNode.Name.Value;
        Field = field;
        SyntaxNode = syntaxNode;
        IsConditional = isConditional;
        Path = path;
    }

    public string ResponseName { get; }

    public IOutputField Field { get; }

    public FieldNode SyntaxNode { get; }

    public Path Path { get; }

    public bool IsConditional { get; }

    public FieldSelection WithPath(Path path) =>
        new(Field, SyntaxNode, path, IsConditional);
}
