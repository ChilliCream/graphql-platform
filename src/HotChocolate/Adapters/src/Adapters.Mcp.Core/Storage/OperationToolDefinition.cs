using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CaseConverter;
using HotChocolate.Adapters.Mcp.Serialization;
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
    public ImmutableArray<IconDefinition>? Icons { get; init; }

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
    /// Gets the optional view configuration for this tool.
    /// </summary>
    public McpAppView? View
    {
        get;
        init
        {
            field = value;

            if (value is null)
            {
                ViewResourceUri = null;
            }
            else
            {
                var name = Name.ToKebabCase();
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Html)));
                ViewResourceUri = $"ui://views/{name}-{hash}.html";
            }
        }
    }

    public string? ViewResourceUri { get; private set; }

    /// <summary>
    /// <para>
    /// Who can access this tool.
    /// </para>
    /// <list type="table">
    ///     <listheader>
    ///         <term>Value</term>
    ///         <description>Description</description>
    ///     </listheader>
    ///     <item>
    ///         <term>Model</term>
    ///         <description>Tool visible to and callable by the agent.</description>
    ///     </item>
    ///     <item>
    ///         <term>App</term>
    ///         <description>Tool callable by the app from this server only.</description>
    ///     </item>
    /// </list>
    /// </summary>
    public ImmutableArray<McpAppViewVisibility>? Visibility { get; init; }

    public static OperationToolDefinition From(
        DocumentNode document,
        string name,
        McpToolSettingsDto? settings,
        string? viewHtml)
    {
        return new OperationToolDefinition(document)
        {
            Name = name,
            Title = settings?.Title,
            Icons =
                settings?.Icons?.Select(
                    i => new IconDefinition(i.Source)
                    {
                        MimeType = i.MimeType,
                        Sizes = i.Sizes,
                        Theme = i.Theme
                    }).ToImmutableArray(),
            DestructiveHint = settings?.Annotations?.DestructiveHint,
            IdempotentHint = settings?.Annotations?.IdempotentHint,
            OpenWorldHint = settings?.Annotations?.OpenWorldHint,
            View = viewHtml is null ? null : new McpAppView(viewHtml)
            {
                Csp = settings?.View?.Csp is { } csp
                    ? new McpAppViewCsp
                    {
                        BaseUriDomains = csp.BaseUriDomains?.ToImmutableArray(),
                        ConnectDomains = csp.ConnectDomains?.ToImmutableArray(),
                        FrameDomains = csp.FrameDomains?.ToImmutableArray(),
                        ResourceDomains = csp.ResourceDomains?.ToImmutableArray()
                    }
                    : null,
                Domain = settings?.View?.Domain,
                Permissions = settings?.View?.Permissions is { } permissions
                    ? new McpAppViewPermissions
                    {
                        Camera = permissions.Camera,
                        ClipboardWrite = permissions.ClipboardWrite,
                        Geolocation = permissions.Geolocation,
                        Microphone = permissions.Microphone
                    }
                    : null,
                PrefersBorder = settings?.View?.PrefersBorder
            },
            Visibility = settings?.Visibility is { } visibility
                ? visibility.ToImmutableArray()
                : null
        };
    }

    /// <summary>Regex that validates tool names.</summary>
    [GeneratedRegex(@"^[A-Za-z0-9_.-]{1,128}\z")]
    private static partial Regex ValidateToolNameRegex();
}
