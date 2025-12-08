using System.Collections.Immutable;
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
    private readonly OperationDefinitionNode _operation;

    /// <summary>
    /// Initializes a new MCP tool definition from a GraphQL operation document.
    /// </summary>
    /// <param name="document">
    /// GraphQL document containing exactly one operation definition.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when document doesn't contain exactly one operation.
    /// </exception>
    public OperationToolDefinition(DocumentNode document)
    {
        ArgumentNullException.ThrowIfNull(document);

        try
        {
            _operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        }
        catch (InvalidOperationException)
        {
            throw new ArgumentException(
                OperationToolDefinition_DocumentMustContainSingleOperation,
                nameof(document));
        }

        Document = document;
    }

    /// <summary>
    /// Gets the GraphQL document containing operation that represents the MCP tool.
    /// </summary>
    public DocumentNode Document { get; }

    /// <summary>
    /// Gets the name of the MCP tool.
    /// </summary>
    public string Name
    {
        get => field ??= _operation.Name!.Value.ToSnakeCase();
        init
        {
            if (!ValidateToolNameRegex().IsMatch(value))
            {
                throw new ArgumentException(
                    string.Format(OperationToolDefinition_InvalidToolName, value, ValidateToolNameRegex()));
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets the optional human-readable title for the tool.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the optional icons for the tool.
    /// </summary>
    public ImmutableArray<OperationToolIcon>? Icons { get; init; }

    /// <summary>
    /// Gets a hint indicating whether this operation may cause destructive side effects.
    /// </summary>
    public bool? DestructiveHint { get; init; }

    /// <summary>
    /// Gets a hint indicating whether this operation is idempotent (safe to retry).
    /// </summary>
    public bool? IdempotentHint { get; init; }

    /// <summary>
    /// Gets a hint indicating whether this operation assumes an open-world model.
    /// </summary>
    public bool? OpenWorldHint { get; init; }

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
                OpenAiComponentOutputTemplate = $"ui://open-ai-components/{name}-{hash}.html";
            }
        }
    }

    public string? OpenAiComponentOutputTemplate { get; private set; }

    /// <summary>Regex that validates tool names.</summary>
    [GeneratedRegex(@"^[A-Za-z0-9_.-]{1,128}\z")]
    private static partial Regex ValidateToolNameRegex();
}
