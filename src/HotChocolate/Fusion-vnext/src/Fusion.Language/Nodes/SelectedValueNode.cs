namespace HotChocolate.Fusion;

/// <summary>
/// <para>
/// A <c>SelectedValue</c> is defined as either a <c>Path</c> or a <c>SelectedObjectValue</c>.
/// </para>
/// <para>
/// The <c>|</c> operator can be used to match multiple possible <c>SelectedValue</c>s.
/// </para>
/// </summary>
internal sealed class SelectedValueNode : IFieldSelectionMapSyntaxNode
{
    public SelectedValueNode(
        PathNode path,
        SelectedValueNode? selectedValue = null)
    {
        ArgumentNullException.ThrowIfNull(path);

        Path = path;
        SelectedValue = selectedValue;
    }

    public SelectedValueNode(
        SelectedObjectValueNode selectedObjectValue,
        SelectedValueNode? selectedValue = null)
    {
        ArgumentNullException.ThrowIfNull(selectedObjectValue);

        SelectedObjectValue = selectedObjectValue;
        SelectedValue = selectedValue;
    }

    public SelectedValueNode(
        Location? location,
        PathNode path,
        SelectedValueNode? selectedValue)
        : this(path, selectedValue)
    {
        Location = location;
    }

    public SelectedValueNode(
        Location? location,
        SelectedObjectValueNode selectedObjectValue,
        SelectedValueNode? selectedValue)
        : this(selectedObjectValue, selectedValue)
    {
        Location = location;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.SelectedValue;

    public Location? Location { get; }

    public PathNode? Path { get; }

    public SelectedObjectValueNode? SelectedObjectValue { get; }

    public SelectedValueNode? SelectedValue { get; }

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

        if (SelectedValue is not null)
        {
            yield return SelectedValue;
        }
    }

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
