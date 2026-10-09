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

        return GetSession(executor);
    }

    protected override void OnConfigureRequestExecutor(IRequestExecutor newExecutor, IRequestExecutor? oldExecutor)
    {
        _session = GetSession(newExecutor);
    }

    public static HttpRequestExecutorProxy Create(IServiceProvider services, string schemaName)
    {
        var executorProvider = services.GetRequiredService<IRequestExecutorProvider>();
        var executorEvents = services.GetRequiredService<IRequestExecutorEvents>();
        return new HttpRequestExecutorProxy(executorProvider, executorEvents, schemaName);
    }

    private static ExecutorSession GetSession(IRequestExecutor executor)
        => executor.Schema.Services.GetService<ExecutorSession>() ?? new ExecutorSession(executor);
}
