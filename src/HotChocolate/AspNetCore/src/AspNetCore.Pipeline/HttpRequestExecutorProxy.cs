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

    public ValueTask<ExecutorSession> GetOrCreateSessionAsync(CancellationToken cancellationToken)
    {
        if (_session is { } session)
        {
            return new ValueTask<ExecutorSession>(session);
        }

        return GetOrCreateSessionSlowAsync(this, cancellationToken);

        static async ValueTask<ExecutorSession> GetOrCreateSessionSlowAsync(
            HttpRequestExecutorProxy proxy,
            CancellationToken cancellationToken)
        {
            var executor = await proxy.GetExecutorAsync(cancellationToken);
            return executor.Features.GetRequired<ExecutorSession>();
        }
    }

    protected override void OnConfigureRequestExecutor(IRequestExecutor newExecutor, IRequestExecutor? oldExecutor)
    {
        var session = new ExecutorSession(newExecutor);
        newExecutor.Features.Set(session);
        _session = session;
    }

    public static HttpRequestExecutorProxy Create(IServiceProvider services, string schemaName)
    {
        var executorProvider = services.GetRequiredService<IRequestExecutorProvider>();
        var executorEvents = services.GetRequiredService<IRequestExecutorEvents>();
        return new HttpRequestExecutorProxy(executorProvider, executorEvents, schemaName);
    }
}
