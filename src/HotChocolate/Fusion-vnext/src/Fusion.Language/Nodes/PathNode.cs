namespace HotChocolate.Fusion.Language;

/// <summary>
/// The <c>Path</c> literal is a string used to select a single output value from the return type by
/// specifying a path to that value. This path is defined as a sequence of field names, each
/// separated by a period (<c>.</c>) to create segments.
/// </summary>
public sealed class PathNode(PathSegmentNode pathSegment, NameNode? typeName = null)
    : IFieldSelectionMapSyntaxNode
{
    public PathNode(Location? location, PathSegmentNode pathSegment, NameNode? typeName)
        : this(pathSegment, typeName)
    {
        Location = location;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.Path;

    public Location? Location { get; }

    public PathSegmentNode PathSegment { get; } = pathSegment
        ?? throw new ArgumentNullException(nameof(pathSegment));

    public NameNode? TypeName { get; } = typeName;

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes()
    {
        yield return PathSegment;

        if (TypeName is not null)
        {
            yield return TypeName;
        }
    }

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
