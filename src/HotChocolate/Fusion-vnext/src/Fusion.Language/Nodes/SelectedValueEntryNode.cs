namespace HotChocolate.Fusion.Language;

/// <summary>
/// Each <c>SelectedValueEntry</c> may take one of the following forms:
///
/// <list type="bullet">
///     <item>
///         <description>
///         A <c>Path</c> (when not immediately followed by a dot) that is designed to point to a
///         single value, although it may reference multiple fields depending on its return type.
///         </description>
///     </item>
///     <item>
///         <description>
///         A <c>Path</c> immediately followed by a dot and a <c>SelectedObjectValue</c> to denote
///         a nested object selection.
///         </description>
///     </item>
///     <item>
///         <description>
///         A <c>Path</c> immediately followed by a <c>SelectedListValue</c> to denote selection
///         from a list.
///         </description>
///     </item>
///     <item>
///         <description>
///         A standalone <c>SelectedObjectValue</c>.
///         </description>
///     </item>
/// </list>
/// </summary>
public sealed class SelectedValueEntryNode(
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
