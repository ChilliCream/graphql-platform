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
public sealed class ListValueSelectionNode : IFieldSelectionMapSyntaxNode
{
    public ListValueSelectionNode(SelectedValueNode selectedValue)
    {
        SelectedValue = selectedValue;
    }

    public ListValueSelectionNode(ListValueSelectionNode listValueSelection)
    {
        ListValueSelection = listValueSelection;
    }

    public ListValueSelectionNode(Location? location, SelectedValueNode selectedValue)
        : this(selectedValue)
    {
        Location = location;
    }

    public ListValueSelectionNode(Location? location, ListValueSelectionNode listValueSelection)
        : this(listValueSelection)
    {
        Location = location;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.ListValueSelection;

    public Location? Location { get; }

    public SelectedValueNode? SelectedValue { get; }

    public ListValueSelectionNode? ListValueSelection { get; }

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes()
    {
        if (SelectedValue is not null)
        {
            yield return SelectedValue;
        }

        if (ListValueSelection is not null)
        {
            yield return ListValueSelection;
        }
    }

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
