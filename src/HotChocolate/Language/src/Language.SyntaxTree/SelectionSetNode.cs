using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// Represents the selection set syntax.
/// </summary>
public sealed class SelectionSetNode : ISyntaxNode
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

    public SelectionSetNode WithSelections(IReadOnlyList<ISelectionNode> selections)
        => new(Location, selections);
}
