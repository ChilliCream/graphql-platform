namespace HotChocolate.Fusion;

/// <summary>
/// Each segment specifies a field in the context of the parent, with the root segment referencing a
/// field in the return type of the query.
/// </summary>
internal sealed class PathSegmentNode(
    NameNode fieldName,
    NameNode? typeName = null,
    PathSegmentNode? pathSegment = null)
    : IFieldSelectionMapSyntaxNode
{
    public PathSegmentNode(
        Location? location,
        NameNode fieldName,
        NameNode? typeName,
        PathSegmentNode? pathSegment)
        : this(fieldName, typeName, pathSegment)
    {
        Location = location;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.PathSegment;

    public Location? Location { get; }

    public NameNode FieldName { get; } = fieldName
        ?? throw new ArgumentNullException(nameof(fieldName));

    public NameNode? TypeName { get; } = typeName;

    public PathSegmentNode? PathSegment { get; } = pathSegment;

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes()
    {
        yield return FieldName;

        if (TypeName is not null)
        {
            yield return TypeName;
        }

        if (PathSegment is not null)
        {
            yield return PathSegment;
        }
    }

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
