using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// Represents the schema definition extension syntax.
/// </summary>
public sealed class SchemaExtensionNode : SchemaDefinitionNodeBase, ITypeSystemExtensionNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="SchemaExtensionNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="directives">
    /// The applied directives.
    /// </param>
    /// <param name="operationTypes">
    /// The operation types.
    /// </param>
    public SchemaExtensionNode(
        Location? location,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
        : base(location, directives, operationTypes)
    {
    }

    /// <inheritdoc cref="ISyntaxNode.Kind" />
    public override SyntaxKind Kind => SyntaxKind.SchemaExtension;

    /// <inheritdoc cref="ISyntaxNode.GetNodes()" />
    public IEnumerable<ISyntaxNode> GetNodes()
    {
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
    public SchemaExtensionNode WithLocation(Location? location)
        => new(location, Directives, OperationTypes);

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
    public SchemaExtensionNode WithDirectives(
        IReadOnlyList<DirectiveNode> directives)
        => new(Location, directives, OperationTypes);

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
    public SchemaExtensionNode WithOperationTypes(
        IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
        => new(Location, Directives, operationTypes);
}
