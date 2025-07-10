namespace HotChocolate.Fusion.Language;

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
public sealed class SelectedListValueNode : IFieldSelectionMapSyntaxNode
{
    public SelectedListValueNode(SelectedValueNode selectedValue)
    {
        SelectedValue = selectedValue;
    }

    public SelectedListValueNode(SelectedListValueNode selectedListValue)
    {
        SelectedListValue = selectedListValue;
    }

    public SelectedListValueNode(Location? location, SelectedValueNode selectedValue)
        : this(selectedValue)
    {
        Location = location;
    }

    public SelectedListValueNode(Location? location, SelectedListValueNode selectedListValue)
        : this(selectedListValue)
    {
        Location = location;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.SelectedListValue;

    public Location? Location { get; }

    public SelectedValueNode? SelectedValue { get; }

    public SelectedListValueNode? SelectedListValue { get; }

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes()
    {
        if (SelectedValue is not null)
        {
            yield return SelectedValue;
        }

        if (SelectedListValue is not null)
        {
            yield return SelectedListValue;
        }
    }

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
