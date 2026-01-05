using System.Text.Json;

namespace HotChocolate.Adapters.Mcp.Packaging;

/// <summary>
/// Represents an MCP prompt containing the settings.
/// </summary>
/// <param name="Settings">The settings document for this prompt.</param>
public sealed record McpPrompt(JsonDocument Settings) : IDisposable
{
    /// <summary>
    /// Releases the resources used by the prompt.
    /// </summary>
    public void Dispose()
    {
        Settings.Dispose();
    }
}
