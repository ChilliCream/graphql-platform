namespace HotChocolate.Fusion.Language;

/// <summary>
/// Represents the <c>null</c> value literal.
/// </summary>
public sealed class NullValueNode : IValueNode
{
    public NullValueNode()
        : this(null)
    {
    }

    public NullValueNode(Location? location)
    {
        Location = location;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.NullValue;

    public Location? Location { get; }

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes() => [];

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
