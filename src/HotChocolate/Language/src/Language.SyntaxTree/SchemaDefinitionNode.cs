using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>
/// Represents GraphQL schema definition syntax.
/// </para>
/// <para>
/// A GraphQL service’s collective type system capabilities
/// are referred to as that service’s “schema”.
/// </para>
/// <para>
/// A schema is defined in terms of the types and directives it supports as well
/// as the root operation types for each kind of operation: query, mutation, and subscription;
/// this determines the place in the type system where those operations begin.
/// </para>
/// </summary>
public sealed class SchemaDefinitionNode : SchemaDefinitionNodeBase, ITypeSystemDefinitionNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="SchemaDefinitionNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="description">
    /// The description of the schema.
    /// </param>
    /// <param name="directives">
    /// The applied directives.
    /// </param>
    /// <param name="operationTypes">
    /// The operation types.
    /// </param>
    public SchemaDefinitionNode(
        Location? location,
        StringValueNode? description,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
        : base(location, directives, operationTypes)
    {
        Description = description;
    }

    /// <inheritdoc cref="SchemaDefinitionNodeBase.Kind"/>
    public override SyntaxKind Kind => SyntaxKind.SchemaDefinition;

    /// <summary>
    /// Gets the schema description.
    /// </summary>
    public StringValueNode? Description { get; }

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes()
    {
        if (Description is not null)
        {
            yield return Description;
        }

        foreach (var directive in Directives)
        {
            yield return directive;
        }

        foreach (var operationType in OperationTypes)
        {
            yield return operationType;
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
    public SchemaDefinitionNode WithLocation(Location? location)
        => new(location, Description, Directives, OperationTypes);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Description" /> with <paramref name="description" />.
    /// </summary>
    /// <param name="description">
    /// The description that shall be used to replace the current description.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="description" />.
    /// </returns>
    public SchemaDefinitionNode WithDescription(
        StringValueNode? description)
        => new(Location, description, Directives, OperationTypes);

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
    public SchemaDefinitionNode WithDirectives(
        IReadOnlyList<DirectiveNode> directives)
        => new(Location, Description, directives, OperationTypes);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="SchemaDefinitionNodeBase.OperationTypes" /> with
    /// <paramref name="operationTypes" />.
    /// </summary>
    /// <param name="operationTypes">
    /// The operationTypes that shall be used to replace the current
    /// <see cref="SchemaDefinitionNodeBase.OperationTypes" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="operationTypes" />.
    /// </returns>
    public SchemaDefinitionNode WithOperationTypes(
        IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
        => new(Location, Description, Directives, operationTypes);
}
