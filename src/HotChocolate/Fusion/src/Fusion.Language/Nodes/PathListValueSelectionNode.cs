namespace HotChocolate.Fusion.Language;

public sealed class PathListValueSelectionNode : IValueSelectionNode
{
    public PathListValueSelectionNode(PathNode path, ListValueSelectionNode listValueSelection)
        : this(null, path, listValueSelection)
    {
    }

    public PathListValueSelectionNode(Location? location, PathNode path, ListValueSelectionNode listValueSelection)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(listValueSelection);

        Location = location;
        Path = path;
        ListValueSelection = listValueSelection;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.PathListValueSelection;

    public Location? Location { get; }

    public PathNode Path { get; }

    public ListValueSelectionNode ListValueSelection { get; }

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes()
    {
        yield return Path;
        yield return ListValueSelection;
    }

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
