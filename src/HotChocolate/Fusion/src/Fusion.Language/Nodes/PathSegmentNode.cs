using System.Collections.Immutable;

namespace HotChocolate.Fusion.Language;

/// <summary>
/// Each segment specifies a field in the context of the parent, with the root segment referencing a
/// field in the return type of the query.
/// </summary>
public sealed class PathSegmentNode : IFieldSelectionMapSyntaxNode
{
    public PathSegmentNode(NameNode fieldName)
        : this(null, fieldName, [], null, null)
    {
    }

    public PathSegmentNode(
        NameNode fieldName,
        PathSegmentNode? pathSegment)
        : this(null, fieldName, [], null, pathSegment)
    {
    }

    public PathSegmentNode(
        NameNode fieldName,
        NameNode? typeName,
        PathSegmentNode? pathSegment)
        : this(null, fieldName, [], typeName, pathSegment)
    {
    }

    public PathSegmentNode(
        Location? location,
        NameNode fieldName,
        NameNode? typeName,
        PathSegmentNode? pathSegment)
        : this(location, fieldName, [], typeName, pathSegment)
    {
    }

    public PathSegmentNode(
        Location? location,
        NameNode fieldName,
        ImmutableArray<ArgumentNode> arguments,
        NameNode? typeName,
        PathSegmentNode? pathSegment)
    {
        ArgumentNullException.ThrowIfNull(fieldName);

        FieldName = fieldName;
        Arguments = arguments.IsDefault ? [] : arguments;
        TypeName = typeName;
        PathSegment = pathSegment;
        Location = location;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.PathSegment;

    public Location? Location { get; }

    public NameNode FieldName { get; }

    /// <summary>
    /// Gets the arguments applied to the field of this path segment.
    /// </summary>
    public ImmutableArray<ArgumentNode> Arguments { get; }

    public NameNode? TypeName { get; }

    public PathSegmentNode? PathSegment { get; }

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes()
    {
        yield return FieldName;

        foreach (var argument in Arguments)
        {
            yield return argument;
        }

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
