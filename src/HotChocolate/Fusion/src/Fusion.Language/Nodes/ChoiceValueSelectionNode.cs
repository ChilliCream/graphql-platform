using System.Collections.Immutable;

namespace HotChocolate.Fusion.Language;

/// <summary>
/// Represents a value selection that evaluates a set of mutually exclusive branches,
/// selecting exactly one branch based on the context of the selection set it is applied to.
/// Each branch is an <see cref="IValueSelectionNode"/>, and a valid choice must contain
/// at least two branches.
/// </summary>
public sealed class ChoiceValueSelectionNode : IValueSelectionNode
{
    public ChoiceValueSelectionNode(ImmutableArray<IValueSelectionNode> branches)
        : this(null, branches)
    {
    }

    public ChoiceValueSelectionNode(Location? location, ImmutableArray<IValueSelectionNode> branches)
    {
        if (branches.Length < 2)
        {
            throw new ArgumentException(
                "A choice value selection must have at least two branches.",
                nameof(branches));
        }

        if (branches.Any(b => b.Kind == FieldSelectionMapSyntaxKind.ChoiceValueSelection))
        {
            throw new ArgumentException(
                "A choice value selection cannot contain another choice value selection.",
                nameof(branches));
        }

        Location = location;
        Branches = branches;
    }

    /// <inheritdoc />
    public FieldSelectionMapSyntaxKind Kind => FieldSelectionMapSyntaxKind.ChoiceValueSelection;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <summary>
    /// Gets the set of mutually exclusive branches from which exactly one will be selected
    /// based on the evaluation context. Each branch represents a distinct value selection.
    /// </summary>
    public ImmutableArray<IValueSelectionNode> Branches { get; }

    /// <inheritdoc />
    public IEnumerable<IFieldSelectionMapSyntaxNode> GetNodes() => Branches;

    /// <inheritdoc cref="IFieldSelectionMapSyntaxNode.ToString()" />
    public override string ToString() => this.Print();

    /// <inheritdoc />
    public string ToString(bool indented) => this.Print(indented);

    /// <inheritdoc />
    public string ToString(StringSyntaxWriterOptions options) => this.Print(true, options);
}
