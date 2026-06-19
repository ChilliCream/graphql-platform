namespace HotChocolate.Fusion.Language;

/// <summary>
/// Represents an argument of a <see cref="PathSegmentNode"/> or an
/// <see cref="ObjectFieldSelectionNode"/>, consisting of a name and a constant value.
/// </summary>
public sealed class ArgumentNode : IFieldSelectionMapSyntaxNode
{
    public ArgumentNode(NameNode name, IValueNode value)
        : this(null, name, value)
    {
    }

    public ArgumentNode(Location? location, NameNode name, IValueNode value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        Location = location;
        Name = name;
        Value = value;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.Argument;

    public Location? Location { get; }

    /// <summary>
    /// Gets the name of the argument.
    /// </summary>
    public NameNode Name { get; }

    /// <summary>
    /// Gets the value of the argument.
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
