using Microsoft.AspNetCore.Http;

namespace HotChocolate.ModelContextProtocol.Proxies;

internal sealed class SseHandlerProxy
{
    private readonly McpRequestExecutorProxy _mcpRequestExecutor;

    public SseHandlerProxy(McpRequestExecutorProxy mcpRequestExecutor)
    {
        ArgumentNullException.ThrowIfNull(mcpRequestExecutor);
        _mcpRequestExecutor = mcpRequestExecutor;
    }

    public async Task HandleSseRequestAsync(HttpContext context)
    {
        var session = await _mcpRequestExecutor.GetOrCreateSessionAsync(context.RequestAborted);
        await session.SseHandler.HandleSseRequestAsync(context);
    }

    public async Task HandleMessageRequestAsync(HttpContext context)
    {
        var session = await _mcpRequestExecutor.GetOrCreateSessionAsync(context.RequestAborted);
        await session.SseHandler.HandleMessageRequestAsync(context);
    }
}
