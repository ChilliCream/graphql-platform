using ModelContextProtocol.AspNetCore;

namespace HotChocolate.ModelContextProtocol.Proxies;

internal sealed record McpExecutorSession(
    StreamableHttpHandler StreamableHttpHandler,
    SseHandler SseHandler);
