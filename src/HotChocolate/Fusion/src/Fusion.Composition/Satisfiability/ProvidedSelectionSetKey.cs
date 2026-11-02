using HotChocolate.Language;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// A provided selection set that equals every provided selection set with the same syntax.
/// </summary>
internal readonly struct ProvidedSelectionSetKey(SelectionSetNode selectionSet)
    : IEquatable<ProvidedSelectionSetKey>
{
    private readonly int _hashCode = SyntaxComparer.BySyntax.GetHashCode(selectionSet);

    /// <summary>
    /// Gets the provided selection set.
    /// </summary>
    public SelectionSetNode SelectionSet { get; } = selectionSet;

    public bool Equals(ProvidedSelectionSetKey other)
        => _hashCode == other._hashCode
            && SyntaxComparer.BySyntax.Equals(SelectionSet, other.SelectionSet);

    public override bool Equals(object? obj) => obj is ProvidedSelectionSetKey other && Equals(other);

    public override int GetHashCode() => _hashCode;
}
