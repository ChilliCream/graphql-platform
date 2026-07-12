namespace HotChocolate.Fusion.Language;

/// <summary>
/// Represents a boolean value literal. The two keywords <c>true</c> and <c>false</c> represent the
/// two boolean values.
/// </summary>
public sealed class BooleanValueNode : IValueNode
{
    public BooleanValueNode(bool value)
        : this(null, value)
    {
    }

    public BooleanValueNode(Location? location, bool value)
    {
        Location = location;
        Value = value;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.BooleanValue;

    public Location? Location { get; }

    /// <summary>
    /// Gets the boolean value.
    /// </summary>
    public bool Value { get; }

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes() => [];

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
