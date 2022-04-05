using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public sealed class InlineFragmentNode
    : ISelectionNode
    , IEquatable<InlineFragmentNode>
{
    public InlineFragmentNode(
        Location? location,
        NamedTypeNode? typeCondition,
        IReadOnlyList<DirectiveNode> directives,
        SelectionSetNode selectionSet)
    {
        Location = location;
        TypeCondition = typeCondition;
        Directives = directives
            ?? throw new ArgumentNullException(nameof(directives));
        SelectionSet = selectionSet
            ?? throw new ArgumentNullException(nameof(selectionSet));
    }

    public SyntaxKind Kind => SyntaxKind.InlineFragment;

    public Location? Location { get; }

    public NamedTypeNode? TypeCondition { get; }

    public IReadOnlyList<DirectiveNode> Directives { get; }

    public SelectionSetNode SelectionSet { get; }

    public IEnumerable<ISyntaxNode> GetNodes()
    {
        if (TypeCondition is { })
        {
            yield return TypeCondition;
        }

        foreach (DirectiveNode directive in Directives)
        {
            yield return directive;
        }

        yield return SelectionSet;
    }

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public override string ToString() => SyntaxPrinter.Print(this, true);

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <param name="indented">
    /// A value that indicates whether the GraphQL output should be formatted,
    /// which includes indenting nested GraphQL tokens, adding
    /// new lines, and adding white space between property names and values.
    /// </param>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

    public InlineFragmentNode WithLocation(Location? location)
    {
        return new InlineFragmentNode(
            location, TypeCondition,
            Directives, SelectionSet);
    }

    public InlineFragmentNode WithTypeCondition(
        NamedTypeNode? typeCondition)
    {
        return new InlineFragmentNode(
            Location, typeCondition,
            Directives, SelectionSet);
    }

    public InlineFragmentNode WithDirectives(
        IReadOnlyList<DirectiveNode> directives)
    {
        return new InlineFragmentNode(
            Location, TypeCondition,
            directives, SelectionSet);
    }

    public InlineFragmentNode WithSelectionSet(
        SelectionSetNode selectionSet)
    {
        return new InlineFragmentNode(
            Location, TypeCondition,
            Directives, selectionSet);
    }


    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">
    /// An object to compare with this object.
    /// </param>
    /// <returns>
    /// true if the current object is equal to the <paramref name="other" /> parameter;
    /// otherwise, false.
    /// </returns>
    public bool Equals(InlineFragmentNode? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Kind == other.Kind
               && TypeCondition.IsEqualTo(other.TypeCondition)
               && SelectionSet.IsEqualTo(other.SelectionSet)
               && Directives.IsEqualTo(other.Directives);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">
    /// The object to compare with the current object.
    /// </param>
    /// <returns>
    /// true if the specified object  is equal to the current object; otherwise, false.
    /// </returns>
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) ||
            (obj is InlineFragmentNode other && Equals(other));
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current object.
    /// </returns>
    public override int GetHashCode() 
        => HashCode.Combine((int)Kind, TypeCondition, Directives, SelectionSet);

    public static bool operator ==(
        InlineFragmentNode? left,
        InlineFragmentNode? right)
        => Equals(left, right);

    public static bool operator !=(
        InlineFragmentNode? left,
        InlineFragmentNode? right)
        => !Equals(left, right);
}
