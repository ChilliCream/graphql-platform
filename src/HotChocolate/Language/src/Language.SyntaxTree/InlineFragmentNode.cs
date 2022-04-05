using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>
/// Represents an inline fragment.
/// </para>
/// <para>
/// Inline Fragments can be used directly within
/// a selection to condition upon a type condition
/// when querying against an interface or union.
/// </para>
/// </summary>
public sealed class InlineFragmentNode
    : ISelectionNode
    , IEquatable<InlineFragmentNode>
{
    /// <summary>
    /// Initializes a new instance of <see cref="FragmentDefinitionNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="typeCondition">
    /// The type condition.
    /// </param>
    /// <param name="directives">
    /// The applied directives.
    /// </param>
    /// <param name="selectionSet">
    /// The fragments selection set.
    /// </param>
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

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Location" /> with <paramref name="location" />.
    /// </summary>
    /// <param name="location">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="location" />.
    /// </returns>
    public InlineFragmentNode WithLocation(Location? location)
        => new(location, TypeCondition, Directives, SelectionSet);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="TypeCondition" /> with <paramref name="typeCondition" />.
    /// </summary>
    /// <param name="typeCondition">
    /// The type condition that shall be used to replace the
    /// current <see cref="TypeCondition" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="typeCondition" />.
    /// </returns>
    public InlineFragmentNode WithTypeCondition(NamedTypeNode? typeCondition)
        => new(Location, typeCondition, Directives, SelectionSet);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="NamedSyntaxNode.Directives" /> with <paramref name="directives" />.
    /// </summary>
    /// <param name="directives">
    /// The directives that shall be used to replace the current
    /// <see cref="NamedSyntaxNode.Directives" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="directives" />.
    /// </returns>
    public InlineFragmentNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, TypeCondition, directives, SelectionSet);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="SelectionSet" /> with <paramref name="selectionSet" />.
    /// </summary>
    /// <param name="selectionSet">
    /// The selectionSet that shall be used to replace the current <see cref="SelectionSet" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="selectionSet" />.
    /// </returns>
    public InlineFragmentNode WithSelectionSet(
        SelectionSetNode selectionSet)
        => new(Location, TypeCondition, Directives, selectionSet);

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

        return TypeCondition.IsEqualTo(other.TypeCondition) &&
            SelectionSet.IsEqualTo(other.SelectionSet) &&
            Directives.IsEqualTo(other.Directives);
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
        => ReferenceEquals(this, obj) ||
            (obj is InlineFragmentNode other && Equals(other));

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current object.
    /// </returns>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Kind);
        hashCode.Add(TypeCondition);
        HashCodeExtensions.AddNodes(ref hashCode, Directives);
        hashCode.Add(SelectionSet);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(
        InlineFragmentNode? left,
        InlineFragmentNode? right)
        => Equals(left, right);

    public static bool operator !=(
        InlineFragmentNode? left,
        InlineFragmentNode? right)
        => !Equals(left, right);
}
