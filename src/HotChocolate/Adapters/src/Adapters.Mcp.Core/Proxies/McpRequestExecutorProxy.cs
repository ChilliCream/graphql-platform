using System.Collections.Concurrent;
using HotChocolate.Execution;
using HotChocolate.Features;
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
    private McpExecutorSession? _session;

    public McpExecutorSession GetOrCreateSession()
    {
        return _session
            ?? Task.Factory
                .StartNew(async () => await GetOrCreateSessionAsync(CancellationToken.None))
                .Unwrap()
                .GetAwaiter()
                .GetResult();
    }

    public async ValueTask<McpExecutorSession> GetOrCreateSessionAsync(
        CancellationToken cancellationToken)
    {
        if (_session is not null)
        {
            return _session;
        }

        var executor = await GetExecutorAsync(cancellationToken).ConfigureAwait(false);
        return executor.Features.GetRequired<McpExecutorSession>();
    }

    protected override void OnConfigureRequestExecutor(
        IRequestExecutor newExecutor,
        IRequestExecutor? oldExecutor)
    {
        if (oldExecutor is not null)
        {
            newExecutor.Features.Set(
                oldExecutor.Schema.Services.GetRequiredService<ConcurrentDictionary<string, McpServer>>());
        }

        var session =
            new McpExecutorSession(
                newExecutor.Schema.Services.GetRequiredService<StreamableHttpHandler>(),
                newExecutor.Schema.Services.GetRequiredService<SseHandler>());

        newExecutor.Features.Set(session);
        _session = session;
    }

    protected override void OnAfterRequestExecutorSwapped(
        IRequestExecutor newExecutor,
        IRequestExecutor oldExecutor)
    {
        var mcpServers =
            newExecutor.Features.GetRequired<ConcurrentDictionary<string, McpServer>>();

        foreach (var mcpServer in mcpServers.Values)
        {
            mcpServer.SendNotificationAsync(ToolListChangedNotification).FireAndForget();
        }
    }
}
