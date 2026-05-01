using HotChocolate.Adapters.Mcp.Proxies;
using HotChocolate.Adapters.Mcp.Storage;

namespace HotChocolate.Adapters.Mcp;

internal sealed record McpRegistration(
    IMcpStorage Storage,
    McpRequestExecutorProxy ExecutorProxy,
    StreamableHttpHandlerProxy HandlerProxy);
