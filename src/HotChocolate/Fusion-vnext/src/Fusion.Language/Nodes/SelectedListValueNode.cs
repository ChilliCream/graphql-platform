namespace HotChocolate.Fusion;

/// <summary>
/// <para>
/// A <c>SelectedListValue</c> is an ordered list of <c>SelectedValue</c> wrapped in square brackets
/// <c>[]</c>. It is used to express semantic equivalence between an argument expecting a list of
/// values and the values of a list field within the output object.
/// </para>
/// <para>
/// The <c>SelectedListValue</c> differs from the <c>ListValue</c> defined in the GraphQL
/// specification by only allowing one <c>SelectedValue</c> as an element.
/// </para>
/// </summary>
internal sealed class SelectedListValueNode(SelectedValueNode selectedValue)
    : IFieldSelectionMapSyntaxNode
{
    public SelectedListValueNode(
        Location? location,
        SelectedValueNode selectedValue) : this(selectedValue)
    {
        Location = location;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.SelectedListValue;

    public Location? Location { get; }

    public readonly SelectedValueNode SelectedValue = selectedValue
        ?? throw new ArgumentNullException(nameof(selectedValue));

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes()
    {
        yield return SelectedValue;
    }

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
