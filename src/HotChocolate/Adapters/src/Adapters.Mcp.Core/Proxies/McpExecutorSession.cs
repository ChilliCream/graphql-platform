using ModelContextProtocol.AspNetCore;

namespace HotChocolate.Adapters.Mcp.Proxies;

internal sealed record McpExecutorSession(
    StreamableHttpHandler StreamableHttpHandler,
    SseHandler SseHandler);
