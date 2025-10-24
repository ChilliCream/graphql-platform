using Microsoft.Extensions.Options;

namespace HotChocolate.Execution.Configuration;

internal sealed class DefaultRequestExecutorOptionsMonitor(
    IOptionsMonitor<RequestExecutorSetup> optionsMonitor)
    : IRequestExecutorOptionsMonitor
{
    public RequestExecutorSetup Get(string schemaName) => optionsMonitor.Get(schemaName);

    public IDisposable? OnChange(Action<string> listener)
        => optionsMonitor.OnChange((_, schemaName) => listener(schemaName!));
}
