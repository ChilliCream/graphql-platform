using System.Collections.Immutable;

namespace HotChocolate.Fusion.Language;

/// <summary>
/// <see cref="ObjectFieldSelectionNode"/> represents a field within a
/// <see cref="ObjectValueSelectionNode"/>.
/// </summary>
public sealed class ObjectFieldSelectionNode : IFieldSelectionMapSyntaxNode
{
    public ObjectFieldSelectionNode(NameNode name)
        : this(null, name, [], null)
    {
    }

    public ObjectFieldSelectionNode(NameNode name, IValueSelectionNode? valueSelection)
        : this(null, name, [], valueSelection)
    {
    }

    public ObjectFieldSelectionNode(Location? location, NameNode name, IValueSelectionNode? valueSelection)
        : this(location, name, [], valueSelection)
    {
    }

    public ObjectFieldSelectionNode(
        Location? location,
        NameNode name,
        ImmutableArray<ArgumentNode> arguments,
        IValueSelectionNode? valueSelection)
    {
        ArgumentNullException.ThrowIfNull(name);

        Location = location;
        Name = name;
        Arguments = arguments.IsDefault ? [] : arguments;
        ValueSelection = valueSelection;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.ObjectFieldSelection;

    public Location? Location { get; }

    public NameNode Name { get; }

    /// <summary>
    /// Gets the arguments applied to this field.
    /// </summary>
    public ImmutableArray<ArgumentNode> Arguments { get; }

    public IValueSelectionNode? ValueSelection { get; }

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes()
    {
        yield return Name;

        foreach (var argument in Arguments)
        {
            yield return argument;
        }

        if (ValueSelection is not null)
        {
            yield return ValueSelection;
        }
    }

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
