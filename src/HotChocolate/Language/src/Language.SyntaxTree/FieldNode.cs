using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// A field describes one discrete piece of information available to 
/// request within a selection set.
/// 
/// Some fields describe complex data or relationships to other data. 
/// In order to further explore this data, a field may itself contain 
/// a selection set, allowing for deeply nested requests. 
/// 
/// All GraphQL operations must specify their selections down to fields 
/// which return scalar values to ensure an unambiguously shaped response.
/// 
/// Field : Alias? Name Arguments? Nullability? Directives? SelectionSet?
/// </summary>
public sealed class FieldNode
    : NamedSyntaxNode
    , ISelectionNode
{
    public FieldNode(
        Location? location,
        NameNode name,
        NameNode? alias,
        INullabilityNode? required,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<ArgumentNode> arguments,
        SelectionSetNode? selectionSet)
        : base(location, name, directives)
    {
        Alias = alias;
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        Required = required;
        SelectionSet = selectionSet;
    }

    /// <inheritdoc />
    public override SyntaxKind Kind => SyntaxKind.Field;

    public NameNode? Alias { get; }

    public IReadOnlyList<ArgumentNode> Arguments { get; }

    public INullabilityNode? Required { get; }

    public SelectionSetNode? SelectionSet { get; }

    /// <inheritdoc />
    public override IEnumerable<ISyntaxNode> GetNodes()
    {
        if (Alias is not null)
        {
            yield return Alias;
        }

        yield return Name;

        foreach (ArgumentNode argument in Arguments)
        {
            yield return argument;
        }

        if (Required is not null)
        {
            yield return Required;
        }

        foreach (DirectiveNode directive in Directives)
        {
            yield return directive;
        }

        if (SelectionSet is not null)
        {
            yield return SelectionSet;
        }
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
    public override string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

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
    public FieldNode WithLocation(Location? location)
        => new(location, Name, Alias, Required, Directives, Arguments, SelectionSet);

    /// <summary>
    /// Creates a new node from the current instance and replaces the 
    /// <see cref="Name" /> with <paramref name="name" />.
    /// </summary>
    /// <param name="name">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="name" />.
    /// </returns>
    public FieldNode WithName(NameNode name)
        => new(Location, name, Alias, Required, Directives, Arguments, SelectionSet);

    /// <summary>
    /// Creates a new node from the current instance and replaces the 
    /// <see cref="Alias" /> with <paramref name="alias" />.
    /// </summary>
    /// <param name="alias">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="alias" />.
    /// </returns>
    public FieldNode WithAlias(NameNode? alias)
        => new(Location, Name, alias, Required, Directives, Arguments, SelectionSet);

    /// <summary>
    /// Creates a new node from the current instance and replaces the 
    /// <see cref="Directives" /> with <paramref name="directives" />.
    /// </summary>
    /// <param name="directives">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="directives" />.
    /// </returns>
    public FieldNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, Alias, Required, directives, Arguments, SelectionSet);

    /// <summary>
    /// Creates a new node from the current instance and replaces the 
    /// <see cref="Arguments" /> with <paramref name="arguments" />.
    /// </summary>
    /// <param name="arguments">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="arguments" />.
    /// </returns>
    public FieldNode WithArguments(IReadOnlyList<ArgumentNode> arguments)
        => new(Location, Name, Alias, Required, Directives, arguments, SelectionSet);

    /// <summary>
    /// Creates a new node from the current instance and replaces the 
    /// <see cref="SelectionSet" /> with <paramref name="selectionSet" />.
    /// </summary>
    /// <param name="selectionSet">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="selectionSet" />.
    /// </returns>
    public FieldNode WithSelectionSet(SelectionSetNode? selectionSet)
        => new(Location, Name, Alias, Required, Directives, Arguments, selectionSet);

    /// <summary>
    /// Creates a new node from the current instance and replaces the 
    /// <see cref="Required" /> with <paramref name="required" />.
    /// </summary>
    /// <param name="required">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="required" />.
    /// </returns>
    public FieldNode WithRequired(INullabilityNode? required)
        => new(Location, Name, Alias, required, Directives, Arguments, SelectionSet);
}
