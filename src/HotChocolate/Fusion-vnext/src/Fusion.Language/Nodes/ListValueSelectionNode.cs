using System.Collections.Immutable;

namespace HotChocolate.Fusion.Language;

/// <summary>
/// <para>
/// A <c>SelectedListValue</c> is an ordered list of <c>SelectedValue</c> wrapped in square brackets
/// <c>[]</c>. It is used to express semantic equivalence between an argument expecting a list of
/// values and the values of a list field within the output object.
/// </para>
/// <para>
/// The <c>SelectedListValue</c> differs from the <c>ListValue</c> defined in the GraphQL
/// specification by only allowing one <c>SelectedValue</c> as an element.
/// </para>
/// </summary>
public sealed class ListValueSelectionNode : IValueSelectionNode
{
    public ListValueSelectionNode(IValueSelectionNode elementSelection)
        : this(null, elementSelection)
    {
    }

    public ListValueSelectionNode(Location? location, IValueSelectionNode elementSelection)
    {
        Location = location;
        ElementSelection = elementSelection;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.ListValueSelection;

    public Location? Location { get; }

    public IValueSelectionNode ElementSelection { get; }

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes()
    {
        yield return ElementSelection;
    }

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
