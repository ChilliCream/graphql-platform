using System;
using System.Collections.Generic;

namespace HotChocolate.Language;

/// <summary>
/// Represents the base class for <see cref="SchemaDefinitionNode"/> and
/// <see cref="SchemaExtensionNode"/>.
/// </summary>
public abstract class SchemaDefinitionNodeBase : IHasDirectives
{
    /// <summary>
    /// Initializes a new instance of <see cref="SchemaDefinitionNodeBase"/>.
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
    protected SchemaDefinitionNodeBase(
        Location? location,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<OperationTypeDefinitionNode> operationTypes)
    {
        Location = location;
        Directives = directives ?? throw new ArgumentNullException(nameof(directives));
        OperationTypes = operationTypes ?? throw new ArgumentNullException(nameof(operationTypes));
    }

    /// <inheritdoc cref="ISyntaxNode.Kind" />
    public abstract SyntaxKind Kind { get; }

    /// <inheritdoc cref="ISyntaxNode.Location" />
    public Location? Location { get; }

    /// <summary>
    /// Gets the applied directives.
    /// </summary>
    public IReadOnlyList<DirectiveNode> Directives { get; }

    /// <summary>
    /// Gets the schema operation types.
    /// </summary>
    public IReadOnlyList<OperationTypeDefinitionNode> OperationTypes { get; }
}
