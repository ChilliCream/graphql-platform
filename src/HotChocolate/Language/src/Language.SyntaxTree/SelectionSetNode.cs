using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// Represents the selection set syntax.
/// </summary>
public sealed class SelectionSetNode
    : ISyntaxNode
    , IEquatable<SelectionSetNode>
{
    /// <summary>
    /// Initializes a new instance of <see cref="SchemaDefinitionNode"/>
    /// </summary>
    /// <param name="selections">
    /// The selections.
    /// </param>
    public SelectionSetNode(IReadOnlyList<ISelectionNode> selections)
        : this(null, selections)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SchemaDefinitionNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="selections">
    /// The selections.
    /// </param>
    public SelectionSetNode(Location? location, IReadOnlyList<ISelectionNode> selections)
    {
        Location = location;
        Selections = selections ?? throw new ArgumentNullException(nameof(selections));
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.SelectionSet;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <summary>
    /// Gets the selections.
    /// </summary>
    public IReadOnlyList<ISelectionNode> Selections { get; }

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes() => Selections;

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

    public SelectionSetNode WithLocation(Location? location)
        => new(location, Selections);

    public SelectionSetNode WithSelections(
        IReadOnlyList<ISelectionNode> selections)
        => new(Location, selections);

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
    public bool Equals(SelectionSetNode? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Selections.IsEqualTo(other.Selections);
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
            (obj is SelectionSetNode other && Equals(other));
    }

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
        hashCode.AddNodes(Selections);
        return hashCode.ToHashCode();
    }

    /// <summary>
    /// The equal operator.
    /// </summary>
    /// <param name="left">The left parameter</param>
    /// <param name="right">The right parameter</param>
    /// <returns>
    /// <c>true</c> if <paramref name="left"/> and <paramref name="right"/> are equal.
    /// </returns>
    public static bool operator ==(
        SelectionSetNode? left,
        SelectionSetNode? right)
        => Equals(left, right);

    /// <summary>
    /// The not equal operator.
    /// </summary>
    /// <param name="left">The left parameter</param>
    /// <param name="right">The right parameter</param>
    /// <returns>
    /// <c>true</c> if <paramref name="left"/> and <paramref name="right"/> are not equal.
    /// </returns>
    public static bool operator !=(
        SelectionSetNode? left,
        SelectionSetNode? right)
        => !Equals(left, right);
}
