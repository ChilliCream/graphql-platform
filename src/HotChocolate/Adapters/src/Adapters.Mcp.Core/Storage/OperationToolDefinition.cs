using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CaseConverter;
using HotChocolate.Language;
using static HotChocolate.Adapters.Mcp.Properties.McpAdapterResources;

namespace HotChocolate.Adapters.Mcp.Storage;

/// <summary>
/// Represents a GraphQL-operation-based MCP tool definition which is used by
/// Hot Chocolate to create the actual MCP tool.
/// </summary>
public sealed partial class OperationToolDefinition
{
    /// <summary>
    /// Initializes a new MCP tool definition from a GraphQL operation document.
    /// </summary>
    /// <param name="document">
    /// GraphQL document containing exactly one operation definition.
    /// </param>
    /// <param name="name">
    /// The name of the MCP tool.
    /// </param>
    /// <param name="title">
    /// Optional tool title.
    /// </param>
    /// <param name="destructiveHint">
    /// Optional destructive operation hint.
    /// </param>
    /// <param name="idempotentHint">
    /// Optional idempotent operation hint.
    /// </param>
    /// <param name="openWorldHint">
    /// Optional open-world assumption hint.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when document doesn't contain exactly one operation.
    /// </exception>
    public OperationToolDefinition(
        DocumentNode document,
        string? name = null,
        string? title = null,
        bool? destructiveHint = null,
        bool? idempotentHint = null,
        bool? openWorldHint = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        OperationDefinitionNode? operation = null;

        foreach (var current in document.Definitions.OfType<OperationDefinitionNode>())
        {
            if (operation is not null)
            {
                throw new ArgumentException(
                    OperationToolDefinition_DocumentMustContainSingleOperation,
                    nameof(document));
            }

            operation = current;
        }

        if (operation is null)
        {
            throw new ArgumentException(
                OperationToolDefinition_DocumentMustContainSingleOperation,
                nameof(document));
        }

        if (name is not null && !ValidateToolNameRegex().IsMatch(name))
        {
            throw new ArgumentException(
                string.Format(OperationToolDefinition_InvalidToolName, name, ValidateToolNameRegex()));
        }

        Name = name ?? operation.Name?.Value.ToSnakeCase()!;
        Document = document;
        Title = title;
        DestructiveHint = destructiveHint;
        IdempotentHint = idempotentHint;
        OpenWorldHint = openWorldHint;
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

    /// <summary>
    /// Gets the optional OpenAI component configuration for this tool.
    /// </summary>
    public OpenAiComponent? OpenAiComponent
    {
        get;
        init
        {
            field = value;

            if (value is null)
            {
                OpenAiComponentOutputTemplate = null;
            }
            else
            {
                var name = Name.ToKebabCase();
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.HtmlTemplateText)));
                OpenAiComponentOutputTemplate = $"ui://components/{name}-{hash}.html";
            }
        }
    }

    public string? OpenAiComponentOutputTemplate { get; private set; }

    /// <summary>Regex that validates tool names.</summary>
    [GeneratedRegex(@"^[A-Za-z0-9_.-]{1,128}\z")]
    private static partial Regex ValidateToolNameRegex();
}
