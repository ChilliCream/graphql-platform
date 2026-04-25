using System.Collections.Concurrent;
using HotChocolate.Adapters.Mcp.Proxies;
using HotChocolate.Execution;

namespace HotChocolate.Adapters.Mcp;

internal sealed class McpResolver
{
    private readonly IRequestExecutorProvider _executorProvider;
    private readonly IRequestExecutorEvents _executorEvents;
    private readonly ConcurrentDictionary<string, McpRequestExecutorProxy> _executorProxies = new();
    private readonly ConcurrentDictionary<string, StreamableHttpHandlerProxy> _handlerProxies = new();

    public McpResolver(
        IRequestExecutorProvider executorProvider,
        IRequestExecutorEvents executorEvents)
    {
        ArgumentNullException.ThrowIfNull(executorProvider);
        ArgumentNullException.ThrowIfNull(executorEvents);
        _executorProvider = executorProvider;
        _executorEvents = executorEvents;
    }

    public McpRequestExecutorProxy GetRequestExecutorProxy(string schemaName)
    {
        ArgumentNullException.ThrowIfNull(schemaName);
        return _executorProxies.GetOrAdd(
            schemaName,
            static (name, state) => new McpRequestExecutorProxy(
                state._executorProvider,
                state._executorEvents,
                name),
            this);
    }

    public StreamableHttpHandlerProxy GetStreamableHttpHandlerProxy(string schemaName)
    {
        ArgumentNullException.ThrowIfNull(schemaName);
        return _handlerProxies.GetOrAdd(
            schemaName,
            static (name, state) => new StreamableHttpHandlerProxy(
                state.GetRequestExecutorProxy(name)),
            this);
    }
}
