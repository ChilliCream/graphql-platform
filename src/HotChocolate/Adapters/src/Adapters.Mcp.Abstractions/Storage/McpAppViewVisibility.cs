namespace HotChocolate.Adapters.Mcp.Storage;

/// <summary>
/// Tool visibility scope – who can access the tool.
/// </summary>
public enum McpAppViewVisibility
{
    /// <summary>
    /// Tool visible to and callable by the agent.
    /// </summary>
    Model,
    /// <summary>
    /// Tool callable by the app from this server only.
    /// </summary>
    App
}
