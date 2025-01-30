namespace HotChocolate.Fusion;

/// <summary>
/// <c>SelectedObjectValue</c> are unordered lists of keyed input values wrapped in curly-braces
/// <c>{}</c>. This structure is similar to the <c>ObjectValue</c> defined in the GraphQL
/// specification, but it differs by allowing the inclusion of <c>Path</c> values within a
/// <c>SelectedValue</c>, thus extending the traditional <c>ObjectValue</c> capabilities to support
/// direct path selections.
/// </summary>
public sealed class SelectedObjectValueNode(IReadOnlyList<SelectedObjectFieldNode> fields)
    : IFieldSelectionMapSyntaxNode
{
    public SelectedObjectValueNode(
        Location? location,
        IReadOnlyList<SelectedObjectFieldNode> fields) : this(fields)
    {
        Location = location;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.SelectedObjectValue;

    public Location? Location { get; }

    public readonly IReadOnlyList<SelectedObjectFieldNode> Fields = fields
        ?? throw new ArgumentNullException(nameof(fields));

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes()
    {
        return Fields;
    }

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
