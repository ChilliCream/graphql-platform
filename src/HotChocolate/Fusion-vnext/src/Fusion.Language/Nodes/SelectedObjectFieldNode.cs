namespace HotChocolate.Fusion.Language;

/// <summary>
/// <see cref="SelectedObjectFieldNode"/> represents a field within a
/// <see cref="SelectedObjectValueNode"/>.
/// </summary>
public sealed class SelectedObjectFieldNode(
    NameNode name,
    SelectedValueNode? selectedValue = null)
    : IFieldSelectionMapSyntaxNode
{
    public SelectedObjectFieldNode(
        Location? location,
        NameNode name,
        SelectedValueNode? selectedValue) : this(name, selectedValue)
    {
        Location = location;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.SelectedObjectField;

    public Location? Location { get; }

    public NameNode Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    public SelectedValueNode? SelectedValue { get; } = selectedValue;

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes()
    {
        yield return Name;

        if (SelectedValue is not null)
        {
            yield return SelectedValue;
        }
    }

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
