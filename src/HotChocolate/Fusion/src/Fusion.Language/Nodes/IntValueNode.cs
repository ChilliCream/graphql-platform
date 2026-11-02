namespace HotChocolate.Fusion.Language;

/// <summary>
/// Represents an integer value literal, such as <c>1</c> or <c>-123</c>.
/// </summary>
public sealed class IntValueNode : IValueNode
{
    public IntValueNode(string value)
        : this(null, value)
    {
    }

    public IntValueNode(Location? location, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);

        Location = location;
        Value = value;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.IntValue;

    public Location? Location { get; }

    /// <summary>
    /// Gets the raw string representation of the integer value.
    /// </summary>
    public string Value { get; }

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes() => [];

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
