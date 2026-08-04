using HotChocolate.Adapters.Mcp.Storage;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace HotChocolate.Adapters.Mcp.Configuration;

/// <summary>
/// Holds the per-schema configuration that is applied when the MCP server is built.
/// </summary>
public sealed class McpSetup
{
    /// <summary>
    /// Gets the list of delegates that configure <see cref="McpServerOptions"/> before the
    /// MCP server is built. Modifiers are applied in registration order.
    /// </summary>
    public List<Action<McpServerOptions>> ServerOptionsModifiers { get; } = [];

    /// <summary>
    /// Gets the list of delegates that configure the <see cref="IMcpServerBuilder"/>, allowing
    /// services such as tools, prompts, or resources to be registered with the MCP server.
    /// Modifiers are applied in registration order.
    /// </summary>
    public List<Action<IMcpServerBuilder>> ServerModifiers { get; } = [];

    /// <summary>
    /// Gets or sets the factory that produces the <see cref="IMcpStorage"/> instance backing
    /// this schema. The storage is the source of truth for the MCP feature definitions
    /// (tools, prompts) exposed by the MCP server.
    /// </summary>
    public Func<IServiceProvider, IMcpStorage>? StorageFactory { get; set; }
}
