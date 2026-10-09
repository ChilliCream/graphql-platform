namespace HotChocolate.Fusion.Language;

/// <summary>
/// Represents a floating-point value literal that includes either a decimal point (such as
/// <c>1.0</c>), an exponent (such as <c>1e50</c>), or both (such as <c>6.0221413e23</c>).
/// </summary>
public sealed class FloatValueNode : IValueNode
{
    public FloatValueNode(string value)
        : this(null, value)
    {
    }

    public FloatValueNode(Location? location, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);

        Location = location;
        Value = value;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.FloatValue;

    public Location? Location { get; }

    /// <summary>
    /// Gets the raw string representation of the floating-point value.
    /// </summary>
    public string Value { get; }

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes() => [];

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
