using System.Collections.Immutable;

namespace HotChocolate.Adapters.Mcp.Storage;

/// <summary>
/// Content Security Policy configuration for MCP App Views.
/// </summary>
public sealed record McpAppViewCsp
{
    /// <summary>Allowed base URIs for the document (base-uri directive).</summary>
    public ImmutableArray<string>? BaseUriDomains { get; init; }

    /// <summary>Origins for network requests (fetch/XHR/WebSocket).</summary>
    public ImmutableArray<string>? ConnectDomains { get; init; }

    /// <summary>Origins for nested iframes (frame-src directive).</summary>
    public ImmutableArray<string>? FrameDomains { get; init; }

    /// <summary>Origins for static resources (scripts, images, styles, fonts).</summary>
    public ImmutableArray<string>? ResourceDomains { get; init; }
}
