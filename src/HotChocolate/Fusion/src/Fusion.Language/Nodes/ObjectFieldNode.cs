namespace HotChocolate.Fusion.Language;

/// <summary>
/// Represents a field of an <see cref="ObjectValueNode"/>, consisting of a name and a constant
/// value.
/// </summary>
public sealed class ObjectFieldNode : IFieldSelectionMapSyntaxNode
{
    public ObjectFieldNode(NameNode name, IValueNode value)
        : this(null, name, value)
    {
    }

    public ObjectFieldNode(Location? location, NameNode name, IValueNode value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        Location = location;
        Name = name;
        Value = value;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.ObjectField;

    public Location? Location { get; }

    /// <summary>
    /// Gets the name of the field.
    /// </summary>
    public NameNode Name { get; }

    /// <summary>
    /// Gets the value of the field.
    /// </summary>
    public IValueNode Value { get; }

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes()
    {
        yield return Name;
        yield return Value;
    }

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
