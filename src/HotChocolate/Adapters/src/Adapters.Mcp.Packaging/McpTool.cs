using System.Text.Json;

namespace HotChocolate.Adapters.Mcp.Packaging;

/// <summary>
/// Represents an MCP tool containing the GraphQL document and optional settings and view.
/// </summary>
/// <param name="Document">The GraphQL document as raw bytes.</param>
/// <param name="Settings">The optional settings document for this tool.</param>
/// <param name="ViewHtml">The optional view HTML as raw bytes.</param>
public sealed record McpTool(
    ReadOnlyMemory<byte> Document,
    JsonDocument? Settings,
    ReadOnlyMemory<byte>? ViewHtml) : IDisposable
{
    /// <summary>
    /// Releases the resources used by the tool.
    /// </summary>
    public void Dispose()
    {
        Settings?.Dispose();
    }
}
