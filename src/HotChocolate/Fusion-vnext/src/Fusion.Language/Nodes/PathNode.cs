namespace HotChocolate.Fusion.Language;

/// <summary>
/// The <c>Path</c> literal is a string used to select a single output value from the return type by
/// specifying a path to that value. This path is defined as a sequence of field names, each
/// separated by a period (<c>.</c>) to create segments.
/// </summary>
public sealed class PathNode : IValueSelectionNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathNode"/> class.
    /// </summary>
    /// <param name="location">The location of the path node.</param>
    /// <param name="pathSegment">The path segment.</param>
    /// <param name="typeName">The type name.</param>
    public PathNode(Location? location, PathSegmentNode pathSegment, NameNode? typeName)
        : this(pathSegment, typeName)
    {
        Location = location;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PathNode"/> class.
    /// </summary>
    /// <param name="pathSegment">The path segment.</param>
    /// <param name="typeName">The type name.</param>
    public PathNode(PathSegmentNode pathSegment, NameNode? typeName = null)
    {
        PathSegment = pathSegment
            ?? throw new ArgumentNullException(nameof(pathSegment));
        TypeName = typeName;
    }

    /// <summary>
    /// Gets the syntax node kind of the path node.
    /// </summary>
    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.Path;

    /// <summary>
    /// Gets the location of the path node.
    /// </summary>
    public Location? Location { get; }

    /// <summary>
    /// Gets the root path segment of this path.
    /// </summary>
    public PathSegmentNode PathSegment { get; }

    /// <summary>
    /// Gets the generic type condition of this path.
    /// </summary>
    public NameNode? TypeName { get; }

    /// <summary>
    /// Gets the nodes of the path node.
    /// </summary>
    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes()
    {
        yield return PathSegment;

        if (TypeName is not null)
        {
            yield return TypeName;
        }
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => this.Print();

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <param name="indented">
    /// A value indicating whether the string should be indented.
    /// </param>
    /// <returns>A string that represents the current object.</returns>
    public string ToString(bool indented) => this.Print(indented);

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <param name="options">The options to use for the string writer.</param>
    /// <returns>A string that represents the current object.</returns>
    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
