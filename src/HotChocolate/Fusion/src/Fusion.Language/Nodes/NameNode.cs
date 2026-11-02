namespace HotChocolate.Fusion.Language;

/// <summary>
/// Equivalent to the <c>Name</c> defined in the GraphQL specification.
/// </summary>
public sealed class NameNode : IFieldSelectionMapSyntaxNode
{
    public NameNode(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);

        Value = value;
    }

    public NameNode(Location? location, string value) : this(value)
    {
        Location = location;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.Name;

    public Location? Location { get; }

    public string Value { get; }

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes() => [];

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
