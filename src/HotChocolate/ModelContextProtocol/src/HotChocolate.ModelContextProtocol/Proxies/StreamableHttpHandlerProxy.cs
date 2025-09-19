using Microsoft.AspNetCore.Http;
using ModelContextProtocol.AspNetCore;

namespace HotChocolate.ModelContextProtocol.Proxies;

internal sealed class StreamableHttpHandlerProxy
{
    private readonly McpRequestExecutorProxy _mcpRequestExecutor;

    public StreamableHttpHandlerProxy(McpRequestExecutorProxy mcpRequestExecutor)
    {
        ArgumentNullException.ThrowIfNull(mcpRequestExecutor);
        _mcpRequestExecutor = mcpRequestExecutor;
    }

    public HttpServerTransportOptions HttpServerTransportOptions
        => _mcpRequestExecutor.GetOrCreateSession().StreamableHttpHandler.HttpServerTransportOptions;

    public async Task HandlePostRequestAsync(HttpContext context)
    {
        var session = await _mcpRequestExecutor.GetOrCreateSessionAsync(context.RequestAborted);
        await session.StreamableHttpHandler.HandlePostRequestAsync(context);
    }

    public async Task HandleGetRequestAsync(HttpContext context)
    {
        var session = await _mcpRequestExecutor.GetOrCreateSessionAsync(context.RequestAborted);
        await session.StreamableHttpHandler.HandleGetRequestAsync(context);
    }

    public async Task HandleDeleteRequestAsync(HttpContext context)
    {
        var session = await _mcpRequestExecutor.GetOrCreateSessionAsync(context.RequestAborted);
        await session.StreamableHttpHandler.HandleDeleteRequestAsync(context);
    }
}
