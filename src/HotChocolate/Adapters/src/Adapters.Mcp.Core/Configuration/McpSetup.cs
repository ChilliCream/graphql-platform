using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace HotChocolate.Adapters.Mcp.Configuration;

public sealed class McpSetup
{
    public List<Action<McpServerOptions>> ServerOptionsModifiers { get; } = [];

    public List<Action<IMcpServerBuilder>> ServerModifiers { get; } = [];
}
