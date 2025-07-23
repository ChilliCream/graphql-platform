using System.Collections.Immutable;

namespace HotChocolate.Fusion.Language;

/// <summary>
/// A <c>SelectedValue</c> consists of one or more <c>SelectedValueEntry</c> components, which may
/// be joined by a pipe (<c>|</c>) operator to indicate alternative selections based on type.
/// </summary>
public sealed class SelectedValueNode : IFieldSelectionMapSyntaxNode
{
    public SelectedValueNode(SelectedValueEntryNode entry)
        : this(null, entry)
    {
    }

    public SelectedValueNode(
        Location? location,
        SelectedValueEntryNode entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Location = location;
        Entries = [entry];
    }

    public SelectedValueNode(ImmutableArray<SelectedValueEntryNode> entries)
        : this(null, entries)
    {
    }

    public SelectedValueNode(
        Location? location,
        ImmutableArray<SelectedValueEntryNode> entries)
    {
        if (entries.IsEmpty)
        {
            throw new ArgumentException(
                $"{nameof(entries)} is empty.",
                nameof(entries));
        }

        Location = location;
        Entries = entries;
    }

    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.SelectedValue;

    public Location? Location { get; }

    public ImmutableArray<SelectedValueEntryNode> Entries { get; }

    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes() => Entries;

    public override string ToString() => this.Print();

    public string ToString(bool indented) => this.Print(indented);

    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
