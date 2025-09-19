using System.Collections.Immutable;

namespace HotChocolate.Fusion.Language;

/// <summary>
/// <c>SelectedObjectValue</c> are unordered lists of keyed input values wrapped in curly-braces
/// <c>{}</c>. This structure is similar to the <c>ObjectValue</c> defined in the GraphQL
/// specification, but it differs by allowing the inclusion of <c>Path</c> values within a
/// <c>SelectedValue</c>, thus extending the traditional <c>ObjectValue</c> capabilities to support
/// direct path selections.
/// </summary>
public sealed class ObjectValueSelectionNode : IValueSelectionNode
{
    public ObjectValueSelectionNode(ImmutableArray<ObjectFieldSelectionNode> fields)
        : this(null, fields)
    {
    }

    public ObjectValueSelectionNode(Location? location, ImmutableArray<ObjectFieldSelectionNode> fields)
    {
        Location = location;
        Fields = fields;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.ObjectValueSelection;

    public Location? Location { get; }

    public readonly ImmutableArray<ObjectFieldSelectionNode> Fields;

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes() => Fields;

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
