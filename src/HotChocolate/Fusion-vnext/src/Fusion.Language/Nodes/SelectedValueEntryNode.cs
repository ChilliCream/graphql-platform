namespace HotChocolate.Fusion;

/// <summary>
/// TODO: Add summary.
/// </summary>
internal sealed class SelectedValueEntryNode(
    Location? location = null,
    PathNode? path = null,
    SelectedObjectValueNode? selectedObjectValue = null,
    SelectedListValueNode? selectedListValue = null)
    : IFieldSelectionMapSyntaxNode
{
    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.SelectedValueEntry;

    public Location? Location { get; } = location;

    public PathNode? Path { get; } = path;

    public SelectedObjectValueNode? SelectedObjectValue { get; } = selectedObjectValue;

    public SelectedListValueNode? SelectedListValue { get; } = selectedListValue;

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
    }

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
