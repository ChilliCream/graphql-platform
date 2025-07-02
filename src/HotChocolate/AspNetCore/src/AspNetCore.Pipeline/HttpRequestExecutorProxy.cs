using HotChocolate.Features;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore;

public sealed class HttpRequestExecutorProxy(
    IRequestExecutorProvider executorProvider,
    IRequestExecutorEvents executorEvents,
    string schemaName)
    : RequestExecutorProxy(executorProvider, executorEvents, schemaName)
{
    private ExecutorSession? _session;

    public async ValueTask<ExecutorSession> GetOrCreateSessionAsync(CancellationToken cancellationToken)
    {
        if (_session is not null)
        {
            return _session;
        }

        var executor = await GetExecutorAsync(cancellationToken);
        return executor.Features.GetRequired<ExecutorSession>();
    }

    protected override void OnRequestExecutorUpdated(IRequestExecutor? executor)
    {
        if (executor is null)
        {
            _session = null;
            return;
        }

        var session = new ExecutorSession(executor);
        executor.Features.Set(session);
        _session = session;
    }

    public static HttpRequestExecutorProxy Create(IServiceProvider services, string schemaName)
    {
        var executorProvider = services.GetRequiredService<IRequestExecutorProvider>();
        var executorEvents = services.GetRequiredService<IRequestExecutorEvents>();
        return new HttpRequestExecutorProxy(executorProvider, executorEvents, schemaName);
    }
}
