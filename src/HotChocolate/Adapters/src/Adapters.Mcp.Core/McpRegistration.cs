using HotChocolate.Adapters.Mcp.Proxies;

namespace HotChocolate.Adapters.Mcp;

internal sealed record McpRegistration(
    McpRequestExecutorProxy ExecutorProxy,
    StreamableHttpHandlerProxy HandlerProxy);
