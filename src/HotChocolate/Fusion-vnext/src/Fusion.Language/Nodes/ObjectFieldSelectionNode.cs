namespace HotChocolate.Fusion.Language;

/// <summary>
/// <see cref="ObjectFieldSelectionNode"/> represents a field within a
/// <see cref="ObjectValueSelectionNode"/>.
/// </summary>
public sealed class ObjectFieldSelectionNode : IFieldSelectionMapSyntaxNode
{
    public ObjectFieldSelectionNode(NameNode name)
        : this(null, name, null)
    {
    }

    public ObjectFieldSelectionNode(NameNode name, IValueSelectionNode? selectedValue)
        : this(null, name, selectedValue)
    {
    }

    public ObjectFieldSelectionNode(Location? location, NameNode name, IValueSelectionNode? selectedValue)
    {
        ArgumentNullException.ThrowIfNull(name);

        Location = location;
        Name = name;
        SelectedValue = selectedValue;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.ObjectFieldSelection;

    public Location? Location { get; }

    public NameNode Name { get; }

    public IValueSelectionNode? SelectedValue { get; }

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
