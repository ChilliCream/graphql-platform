namespace HotChocolate.Fusion;

/// <summary>
/// The <c>Path</c> literal is a string used to select a single output value from the return type by
/// specifying a path to that value. This path is defined as a sequence of field names, each
/// separated by a period (<c>.</c>) to create segments.
/// </summary>
internal sealed class PathNode(NameNode fieldName, NameNode? typeName = null, PathNode? path = null)
    : IFieldSelectionMapSyntaxNode
{
    public PathNode(Location? location, NameNode fieldName, NameNode? typeName, PathNode? path)
        : this(fieldName, typeName, path)
    {
        Location = location;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.Path;

    public Location? Location { get; }

    public NameNode FieldName { get; } = fieldName
        ?? throw new ArgumentNullException(nameof(fieldName));

    public NameNode? TypeName { get; } = typeName;

    public PathNode? Path { get; } = path;

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes()
    {
        yield return FieldName;

        if (TypeName is not null)
        {
            yield return TypeName;
        }

        if (Path is not null)
        {
            yield return Path;
        }
    }

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
