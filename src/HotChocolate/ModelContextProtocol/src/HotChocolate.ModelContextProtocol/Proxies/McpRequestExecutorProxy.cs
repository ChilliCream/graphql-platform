using HotChocolate.Execution;
using HotChocolate.Features;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore;

namespace HotChocolate.ModelContextProtocol.Proxies;

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

    protected override void OnRequestExecutorUpdated(IRequestExecutor? executor)
    {
        if (executor is not null)
        {
            var session =
                new McpExecutorSession(
                    executor.Schema.Services.GetRequiredService<StreamableHttpHandler>(),
                    executor.Schema.Services.GetRequiredService<SseHandler>());

            executor.Features.Set(session);
            _session = session;
        }
    }
}

internal sealed record McpExecutorSession(
    StreamableHttpHandler StreamableHttpHandler,
    SseHandler SseHandler);
