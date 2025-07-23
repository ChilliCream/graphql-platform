namespace HotChocolate.Fusion.Language;

public sealed class PathObjectValueSelectionNode : IValueSelectionNode
{
    public PathObjectValueSelectionNode(Location? location, PathNode path, ObjectValueSelectionNode value)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(value);

        Location = location;
        Path = path;
        Value = value;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.PathObjectValueSelection;

    public Location? Location { get; }

    public PathNode Path { get; }

    public ObjectValueSelectionNode Value { get; }

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes()
    {
        yield return Path;
        yield return Value;
    }

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
