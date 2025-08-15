using HotChocolate.Language;
using HotChocolate.ModelContextProtocol.Directives;
using HotChocolate.ModelContextProtocol.Extensions;
using static HotChocolate.ModelContextProtocol.Properties.ModelContextProtocolResources;
using static HotChocolate.ModelContextProtocol.WellKnownDirectiveNames;

namespace HotChocolate.ModelContextProtocol.Storage;

/// <summary>
/// Represents a GraphQL operation based MCP tool definition which is used by
/// Hot Chocolate to create the actual MCP tool..
/// </summary>
public sealed class OperationToolDefinition
{
    /// <summary>
    /// Initializes a new MCP tool definition from a GraphQL operation document.
    /// </summary>
    /// <param name="name">
    /// The name of the MCP tool.
    /// </param>
    /// <param name="document">
    /// GraphQL document containing exactly one operation definition.
    /// </param>
    /// <param name="title">
    /// Optional tool title. Overrides directive metadata if provided.
    /// </param>
    /// <param name="destructiveHint">
    /// Optional destructive operation hint. Overrides directive metadata if provided.
    /// </param>
    /// <param name="idempotentHint">
    /// Optional idempotent operation hint. Overrides directive metadata if provided.
    /// </param>
    /// <param name="openWorldHint">
    /// Optional open-world assumption hint. Overrides directive metadata if provided.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when document doesn't contain exactly one operation.
    /// </exception>
    public OperationToolDefinition(
        string name,
        DocumentNode document,
        string? title = null,
        bool? destructiveHint = null,
        bool? idempotentHint = null,
        bool? openWorldHint = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(document);

        OperationDefinitionNode? operation = null;

        foreach (var current in document.Definitions.OfType<OperationDefinitionNode>())
        {
            if (operation is not null)
            {
                throw new ArgumentException(
                    Document_Must_Have_Single_Op,
                    nameof(document));
            }

            operation = current;
        }

        if (operation is null)
        {
            throw new ArgumentException(
                Document_Must_Have_Single_Op,
                nameof(document));
        }

        // If we find a tool directive, parse it and remove it from the document.
        // The tool directive is metadata only and doesn't exist in the target schema.
        // Removing it prevents execution errors when the operation is executed.
        var toolDirective = operation.GetMcpToolDirective();
        if (toolDirective is not null)
        {
            var tempDirectives = operation.Directives.ToList();
            foreach (var directive in operation.Directives)
            {
                if (directive.Name.Value.Equals(McpTool))
                {
                    tempDirectives.Remove(directive);
                }
            }

            IReadOnlyList<DirectiveNode> cleanedDirectives;
            if (tempDirectives.Count == 0)
            {
                cleanedDirectives = [];
            }
            else
            {
                tempDirectives.Capacity = tempDirectives.Count;
                cleanedDirectives = tempDirectives;
            }

            var cleanedOperation = operation.WithDirectives(cleanedDirectives);
            var cleanedDefinitions = document.Definitions.ToList();
            cleanedDefinitions.Remove(operation);
            cleanedDefinitions.Add(cleanedOperation);
            cleanedDefinitions.Capacity = cleanedDefinitions.Count;
            document = document.WithDefinitions(cleanedDefinitions);
        }

        Name = name;
        Document = document;

        // Explicit parameters take precedence over directive metadata.
        Title = title ?? toolDirective?.Title;
        DestructiveHint = destructiveHint ?? toolDirective?.DestructiveHint;
        IdempotentHint = idempotentHint ?? toolDirective?.IdempotentHint;
        OpenWorldHint = openWorldHint ??  toolDirective?.OpenWorldHint;
    }

    /// <summary>
    /// Gets the name of the MCP tool.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the GraphQL document containing operation that represents the MCP tool.
    /// </summary>
    public DocumentNode Document { get; }

    /// <summary>
    /// Gets the optional human-readable title for the tool.
    /// </summary>
    public string? Title { get; }

    /// <summary>
    /// Gets a hint indicating whether this operation may cause destructive side effects.
    /// </summary>
    public bool? DestructiveHint { get; }

    /// <summary>
    /// Gets a hint indicating whether this operation is idempotent (safe to retry).
    /// </summary>
    public bool? IdempotentHint { get; }

    /// <summary>
    /// Gets a hint indicating whether this operation assumes an open-world model.
    /// </summary>
    public bool? OpenWorldHint { get; }
}
