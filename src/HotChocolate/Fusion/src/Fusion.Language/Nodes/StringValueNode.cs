namespace HotChocolate.Fusion.Language;

/// <summary>
/// Represents a string value literal. A string value can either be a regular double-quoted string
/// or a triple-quoted block string.
/// </summary>
public sealed class StringValueNode : IValueNode
{
    public StringValueNode(string value)
        : this(null, value, false)
    {
    }

    public StringValueNode(string value, bool block)
        : this(null, value, block)
    {
    }

    public StringValueNode(Location? location, string value, bool block)
    {
        ArgumentNullException.ThrowIfNull(value);

        Location = location;
        Value = value;
        Block = block;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.StringValue;

    public Location? Location { get; }

    /// <summary>
    /// Gets the string value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets a value indicating whether this string value is a block string.
    /// </summary>
    public bool Block { get; }

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes() => [];

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
