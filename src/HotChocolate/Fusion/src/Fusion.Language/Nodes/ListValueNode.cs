using System.Collections.Immutable;

namespace HotChocolate.Fusion.Language;

/// <summary>
/// Represents a list value literal. Lists are ordered sequences of values wrapped in square
/// brackets <c>[]</c> (such as <c>[1, 2, 3]</c>).
/// </summary>
public sealed class ListValueNode : IValueNode
{
    public ListValueNode(ImmutableArray<IValueNode> items)
        : this(null, items)
    {
    }

    public ListValueNode(Location? location, ImmutableArray<IValueNode> items)
    {
        Location = location;
        Items = items.IsDefault ? [] : items;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.ListValue;

    public Location? Location { get; }

    /// <summary>
    /// Gets the items of this list.
    /// </summary>
    public ImmutableArray<IValueNode> Items { get; }

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes() => Items;

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
