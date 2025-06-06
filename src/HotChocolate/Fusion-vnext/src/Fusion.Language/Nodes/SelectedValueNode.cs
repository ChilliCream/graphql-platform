namespace HotChocolate.Fusion.Language;

/// <summary>
/// A <c>SelectedValue</c> consists of one or more <c>SelectedValueEntry</c> components, which may
/// be joined by a pipe (<c>|</c>) operator to indicate alternative selections based on type.
/// </summary>
public sealed class SelectedValueNode(
    SelectedValueEntryNode selectedValueEntry,
    SelectedValueNode? selectedValue = null)
    : IFieldSelectionMapSyntaxNode
{
    public SelectedValueNode(
        Location? location,
        SelectedValueEntryNode selectedValueEntry,
        SelectedValueNode? selectedValue) : this(selectedValueEntry, selectedValue)
    {
        Location = location;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.SelectedValue;

    public Location? Location { get; }

    public SelectedValueEntryNode SelectedValueEntry { get; } = selectedValueEntry;

    public SelectedValueNode? SelectedValue { get; } = selectedValue;

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes()
    {
        yield return SelectedValueEntry;

        if (SelectedValue is not null)
        {
            yield return SelectedValue;
        }
    }

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
