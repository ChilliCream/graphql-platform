using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Adapters.Mcp.Storage;

public sealed class McpAppView([StringSyntax("Html")] string html)
{
    /// <summary>
    /// HTML content.
    /// </summary>
    public string Html { get; } = html;

    /// <summary>
    /// Content Security Policy configuration.
    /// </summary>
    public McpAppViewCsp? Csp { get; init; }

    /// <summary>
    /// Dedicated origin for view sandbox.
    /// </summary>
    public string? Domain { get; init; }

    /// <summary>
    /// Sandbox permissions requested by the view.
    /// </summary>
    public McpAppViewPermissions? Permissions { get; init; }

    /// <summary>
    /// Visual boundary preference – <c>true</c> if UI prefers a visible border.
    /// </summary>
    public bool? PrefersBorder { get; init; }
}
