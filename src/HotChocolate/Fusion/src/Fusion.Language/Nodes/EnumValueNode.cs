namespace HotChocolate.Fusion.Language;

/// <summary>
/// Represents an enum value literal, such as <c>ENUM_VALUE</c>. An enum value is a name that is not
/// <c>true</c>, <c>false</c>, or <c>null</c>.
/// </summary>
public sealed class EnumValueNode : IValueNode
{
    public EnumValueNode(string value)
        : this(null, value)
    {
    }

    public EnumValueNode(Location? location, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);

        Location = location;
        Value = value;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.EnumValue;

    public Location? Location { get; }

    /// <summary>
    /// Gets the enum value.
    /// </summary>
    public string Value { get; }

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes() => [];

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
