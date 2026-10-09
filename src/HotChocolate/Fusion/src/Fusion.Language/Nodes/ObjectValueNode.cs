using System.Collections.Immutable;

namespace HotChocolate.Fusion.Language;

/// <summary>
/// Represents an object value literal. Object values are unordered lists of keyed input values
/// wrapped in curly braces <c>{}</c> (such as <c>{ name: "value", score: 1.0 }</c>).
/// </summary>
public sealed class ObjectValueNode : IValueNode
{
    public ObjectValueNode(ImmutableArray<ObjectFieldNode> fields)
        : this(null, fields)
    {
    }

    public ObjectValueNode(Location? location, ImmutableArray<ObjectFieldNode> fields)
    {
        Location = location;
        Fields = fields.IsDefault ? [] : fields;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.ObjectValue;

    public Location? Location { get; }

    /// <summary>
    /// Gets the fields of this object value.
    /// </summary>
    public ImmutableArray<ObjectFieldNode> Fields { get; }

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes() => Fields;

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
