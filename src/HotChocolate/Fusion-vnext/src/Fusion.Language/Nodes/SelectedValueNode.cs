namespace HotChocolate.Fusion;

/// <summary>
/// <para>
/// A <c>SelectedValue</c> is defined as either a <c>Path</c> or a <c>SelectedObjectValue</c>.
/// </para>
/// <para>
/// The <c>|</c> operator can be used to match multiple possible <c>SelectedValue</c>s.
/// </para>
/// </summary>
internal sealed class SelectedValueNode(
    Location? location = null,
    PathNode? path = null,
    SelectedObjectValueNode? selectedObjectValue = null,
    SelectedListValueNode? selectedListValue = null,
    SelectedValueNode? selectedValue = null)
    : IFieldSelectionMapSyntaxNode
{
    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.SelectedValue;

    public Location? Location { get; } = location;

    public PathNode? Path { get; } = path;

    public SelectedObjectValueNode? SelectedObjectValue { get; } = selectedObjectValue;

    public SelectedListValueNode? SelectedListValue { get; } = selectedListValue;

    public SelectedValueNode? SelectedValue { get; } = selectedValue;

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes()
    {
        if (Path is not null)
        {
            yield return Path;
        }

        if (SelectedObjectValue is not null)
        {
            yield return SelectedObjectValue;
        }

        if (SelectedListValue is not null)
        {
            yield return SelectedListValue;
        }

        if (SelectedValue is not null)
        {
            yield return SelectedValue;
        }
    }

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
