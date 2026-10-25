using System.Collections.Concurrent;
using HotChocolate.Execution;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using static ModelContextProtocol.Protocol.NotificationMethods;

namespace HotChocolate.Adapters.Mcp.Proxies;

internal sealed class McpRequestExecutorProxy(
    IRequestExecutorProvider executorProvider,
    IRequestExecutorEvents executorEvents,
    string schemaName)
    : RequestExecutorProxy(executorProvider, executorEvents, schemaName)
{
    private StreamableHttpHandler? _handler;

    public async ValueTask<StreamableHttpHandler> GetStreamableHttpHandlerAsync(
        CancellationToken cancellationToken)
    {
        if (_handler is not null)
        {
            return _handler;
        }

        var executor = await GetExecutorAsync(cancellationToken).ConfigureAwait(false);

        return executor.Schema.Services.GetRequiredService<StreamableHttpHandler>();
    }

    protected override void OnConfigureRequestExecutor(
        IRequestExecutor newExecutor,
        IRequestExecutor? oldExecutor)
    {
        _handler = newExecutor.Schema.Services.GetRequiredService<StreamableHttpHandler>();
    }

    protected override void OnAfterRequestExecutorSwapped(
        IRequestExecutor newExecutor,
        IRequestExecutor oldExecutor)
    {
        var mcpServers = oldExecutor.Schema.Services
            .GetRequiredService<ConcurrentDictionary<string, McpServer>>();

        foreach (var mcpServer in mcpServers.Values)
        {
            mcpServer.SendNotificationAsync(ToolListChangedNotification).FireAndForget();
        }
    }
}
