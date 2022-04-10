using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public sealed class OperationDefinitionNode
    : IExecutableDefinitionNode
    , IHasDirectives
    , IEquatable<OperationDefinitionNode>
{
    /// <summary>
    /// Initializes a new instance of <see cref="OperationDefinitionNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name that this syntax node holds.
    /// </param>
    /// <param name="operation">
    /// The GraphQL operation.
    /// </param>
    /// <param name="variableDefinitions">
    /// The variables that are declared for this operation definition.</param>
    /// <param name="directives">
    /// The directives that are annotated to this syntax node.
    /// </param>
    /// <param name="selectionSet">
    /// The operations selection set.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Throws if either <paramref name="variableDefinitions"/>, <paramref name="directives"/>,
    /// or <paramref name="selectionSet"/> is <c>null</c>.
    /// </exception>
    public OperationDefinitionNode(
        Location? location,
        NameNode? name,
        OperationType operation,
        IReadOnlyList<VariableDefinitionNode> variableDefinitions,
        IReadOnlyList<DirectiveNode> directives,
        SelectionSetNode selectionSet)
    {
        Location = location;
        Name = name;
        Operation = operation;
        VariableDefinitions = variableDefinitions
                              ?? throw new ArgumentNullException(nameof(variableDefinitions));
        Directives = directives
                     ?? throw new ArgumentNullException(nameof(directives));
        SelectionSet = selectionSet
                       ?? throw new ArgumentNullException(nameof(selectionSet));
    }

    /// <inheritdoc/>
    public SyntaxKind Kind => SyntaxKind.OperationDefinition;

    /// <inheritdoc/>
    public Location? Location { get; }

    /// <summary>
    /// Gets the name of the operation.
    /// </summary>
    public NameNode? Name { get; }

    /// <summary>
    /// Gets the GraphQL operation.
    /// </summary>
    public OperationType Operation { get; }

    /// <summary>
    /// Gets the variables that are declared for this operation definition.
    /// </summary>
    public IReadOnlyList<VariableDefinitionNode> VariableDefinitions { get; }

    /// <inheritdoc/>
    public IReadOnlyList<DirectiveNode> Directives { get; }

    /// <summary>
    /// Gets the operation selection set.
    /// </summary>
    public SelectionSetNode SelectionSet { get; }

    /// <inheritdoc/>
    public IEnumerable<ISyntaxNode> GetNodes()
    {
        if (Name is { })
        {
            yield return Name;
        }

        foreach (VariableDefinitionNode variable in VariableDefinitions)
        {
            yield return variable;
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
    /// <see cref="Location"/> with <paramref name="location"/>.
    /// </summary>
    /// <param name="location">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="location"/>
    /// </returns>
    public OperationDefinitionNode WithLocation(Location? location) =>
        new(location, Name, Operation, VariableDefinitions, Directives, SelectionSet);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="NameNode"/> with <paramref name="name"/>
    /// </summary>
    /// <param name="name">
    /// The name that shall be used to replace the current name.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="name"/>
    /// </returns>
    public OperationDefinitionNode WithName(NameNode? name) =>
        new(Location, name, Operation, VariableDefinitions, Directives, SelectionSet);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Operation" /> with <paramref name="operation" />.
    /// </summary>
    /// <param name="operation">
    /// The operation that shall be used to replace the current operation.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="operation" />.
    /// </returns>
    public OperationDefinitionNode WithOperation(OperationType operation) =>
        new(Location, Name, operation, VariableDefinitions, Directives, SelectionSet);

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
    public OperationDefinitionNode WithVariableDefinitions(
        IReadOnlyList<VariableDefinitionNode> variableDefinitions) =>
        new(Location, Name, Operation, variableDefinitions, Directives, SelectionSet);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="IReadOnlyList&lt;DirectiveNode&gt;"/> with <paramref name="directives"/>
    /// </summary>
    /// <param name="directives">
    /// The directives that shall be used to replace the current directives.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="directives"/>
    /// </returns>
    public OperationDefinitionNode WithDirectives(IReadOnlyList<DirectiveNode> directives) =>
        new(Location, Name, Operation, VariableDefinitions, directives, SelectionSet);

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
    public OperationDefinitionNode WithSelectionSet(SelectionSetNode selectionSet) =>
        new(Location, Name, Operation, VariableDefinitions, Directives, selectionSet);

    /// <inheritdoc/>
    public bool Equals(OperationDefinitionNode? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Equals(Location, other.Location) && Name.IsEqualTo(other.Name) &&
               Operation == other.Operation &&
               EqualityHelper.Equals(VariableDefinitions, other.VariableDefinitions) &&
               EqualityHelper.Equals(Directives, Directives) &&
               SelectionSet.Equals(other.SelectionSet);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) ||
                                                obj is OperationDefinitionNode other &&
                                                Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Location);
        hashCode.Add(Name);
        hashCode.AddNodes(VariableDefinitions);
        hashCode.AddNodes(Directives);
        hashCode.Add(SelectionSet);

        return hashCode.ToHashCode();
    }
}
