namespace HotChocolate.Fusion.Language;

public sealed class PathListValueSelectionNode : IValueSelectionNode
{
    public PathListValueSelectionNode(PathNode path, ListValueSelectionNode listValueSelection)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(listValueSelection);

        Path = path;
        ListValueSelection = listValueSelection;
    }

    public PathNode Path { get; }

    public ListValueSelectionNode ListValueSelection { get; }
    public FieldSelectionMapSyntaxKind Kind { get; }
    public Location? Location { get; }
    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes()
    {
        throw new NotImplementedException();
    }

    public string ToString(bool indented)
    {
        throw new NotImplementedException();
    }

    public string ToString(StringSyntaxWriterOptions options)
    {
        throw new NotImplementedException();
    }
}
