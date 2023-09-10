using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>
/// Represents a named fragment definition.
/// </para>
/// <para>
/// Fragments are the primary unit of composition in GraphQL.
/// </para>
/// <para>
/// Fragments allow for the reuse of common repeated selections of fields,
/// reducing duplicated text in the document.
/// </para>
/// </summary>
public sealed class FragmentDefinitionNode : NamedSyntaxNode, IExecutableDefinitionNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="FragmentDefinitionNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name of the fragment definition.
    /// </param>
    /// <param name="variableDefinitions">
    /// The variables that are declared for this fragment definition.
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
    public FragmentDefinitionNode(
        Location? location,
        NameNode name,
        IReadOnlyList<VariableDefinitionNode> variableDefinitions,
        NamedTypeNode typeCondition,
        IReadOnlyList<DirectiveNode> directives,
        SelectionSetNode selectionSet)
        : base(location, name, directives)
    {
        VariableDefinitions = variableDefinitions
            ?? throw new ArgumentNullException(nameof(variableDefinitions));
        TypeCondition = typeCondition
            ?? throw new ArgumentNullException(nameof(typeCondition));
        SelectionSet = selectionSet
            ?? throw new ArgumentNullException(nameof(selectionSet));
    }

    /// <inheritdoc />
    public override SyntaxKind Kind => SyntaxKind.FragmentDefinition;

    /// <summary>
    /// Gets the variables that are declared for this fragment definition.
    /// </summary>
    public IReadOnlyList<VariableDefinitionNode> VariableDefinitions { get; }

    /// <summary>
    /// Gets the type condition.
    /// </summary>
    public NamedTypeNode TypeCondition { get; }

    /// <summary>
    /// Gets the fragments selection set.
    /// </summary>
    public SelectionSetNode SelectionSet { get; }

    /// <inheritdoc />
    public override IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Name;
        yield return TypeCondition;

        foreach (var directive in Directives)
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
    public FragmentDefinitionNode WithLocation(Location? location)
        => new(location, Name, VariableDefinitions, TypeCondition, Directives, SelectionSet);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="NamedSyntaxNode.Name" /> with <paramref name="name" />.
    /// </summary>
    /// <param name="name">
    /// The name that shall be used to replace the current <see cref="NamedSyntaxNode.Name" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="name" />.
    /// </returns>
    public FragmentDefinitionNode WithName(NameNode name)
        => new(Location, name, VariableDefinitions, TypeCondition, Directives, SelectionSet);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="VariableDefinitions" /> with <paramref name="variableDefinitions" />.
    /// </summary>
    /// <param name="variableDefinitions">
    /// The variable definitions that shall be used to replace the
    /// current <see cref="VariableDefinitions" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="variableDefinitions" />.
    /// </returns>
    public FragmentDefinitionNode WithVariableDefinitions(
        IReadOnlyList<VariableDefinitionNode> variableDefinitions)
        => new(Location, Name, variableDefinitions, TypeCondition, Directives, SelectionSet);

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
    public FragmentDefinitionNode WithTypeCondition(
        NamedTypeNode typeCondition)
        => new(Location, Name, VariableDefinitions, typeCondition, Directives, SelectionSet);

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
    public FragmentDefinitionNode WithDirectives(
        IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, VariableDefinitions, TypeCondition, directives, SelectionSet);

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
    public FragmentDefinitionNode WithSelectionSet(
        SelectionSetNode selectionSet)
        => new(Location, Name, VariableDefinitions, TypeCondition, Directives, selectionSet);
}
