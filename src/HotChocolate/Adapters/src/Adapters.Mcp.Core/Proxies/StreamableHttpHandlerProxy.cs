using Microsoft.AspNetCore.Http;

namespace HotChocolate.Adapters.Mcp.Proxies;

internal sealed class StreamableHttpHandlerProxy
{
    private readonly McpRequestExecutorProxy _mcpRequestExecutor;

    public StreamableHttpHandlerProxy(McpRequestExecutorProxy mcpRequestExecutor)
    {
        ArgumentNullException.ThrowIfNull(mcpRequestExecutor);
        _mcpRequestExecutor = mcpRequestExecutor;
    }

    public async Task HandlePostRequestAsync(HttpContext context)
    {
        var handler =
            await _mcpRequestExecutor.GetStreamableHttpHandlerAsync(context.RequestAborted);
        await handler.HandlePostRequestAsync(context);
    }

    public async Task HandleGetRequestAsync(HttpContext context)
    {
        var handler =
            await _mcpRequestExecutor.GetStreamableHttpHandlerAsync(context.RequestAborted);

        if (handler.HttpServerTransportOptions.Stateless)
        {
            WriteMethodNotAllowed(context);

            return;
        }

        await handler.HandleGetRequestAsync(context);
    }

    public async Task HandleDeleteRequestAsync(HttpContext context)
    {
        var handler =
            await _mcpRequestExecutor.GetStreamableHttpHandlerAsync(context.RequestAborted);

        if (handler.HttpServerTransportOptions.Stateless)
        {
            WriteMethodNotAllowed(context);

            return;
        }

        await handler.HandleDeleteRequestAsync(context);
    }

    private static void WriteMethodNotAllowed(HttpContext context)
    {
        context.Response.Headers.Allow = "POST";
        context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
    }
}
