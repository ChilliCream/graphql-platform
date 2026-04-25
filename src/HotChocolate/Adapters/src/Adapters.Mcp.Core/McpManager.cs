using System.Collections.Concurrent;
using System.Collections.Immutable;
using HotChocolate.Adapters.Mcp.Configuration;
using HotChocolate.Adapters.Mcp.Proxies;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HotChocolate.Adapters.Mcp;

internal sealed class McpManager : IMcpProvider
{
    private readonly IOptionsMonitor<McpSetup> _optionsMonitor;
    private readonly IServiceProvider _applicationServices;
    private readonly ConcurrentDictionary<string, Lazy<McpRegistration>> _registrations = new();

    public McpManager(IOptionsMonitor<McpSetup> optionsMonitor, IServiceProvider applicationServices)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(applicationServices);
        _optionsMonitor = optionsMonitor;
        _applicationServices = applicationServices;
    }

    public ImmutableArray<string> Names
        => _applicationServices
            .GetServices<IConfigureOptions<McpSetup>>()
            .OfType<ConfigureNamedOptions<McpSetup>>()
            .Select(c => c.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .ToImmutableArray()!;

    public McpSetup GetSetup(string? name = null)
    {
        name ??= ISchemaDefinition.DefaultName;
        return _optionsMonitor.Get(name);
    }

    public McpRegistration Get(string? name = null)
    {
        name ??= ISchemaDefinition.DefaultName;
        return _registrations
            .GetOrAdd(
                name,
                static (key, manager) =>
                    new Lazy<McpRegistration>(
                        () => manager.CreateRegistration(key),
                        LazyThreadSafetyMode.ExecutionAndPublication),
                this)
            .Value;
    }

    private McpRegistration CreateRegistration(string name)
    {
        var executorProxy = new McpRequestExecutorProxy(
            _applicationServices.GetRequiredService<IRequestExecutorProvider>(),
            _applicationServices.GetRequiredService<IRequestExecutorEvents>(),
            name);

        var handlerProxy = new StreamableHttpHandlerProxy(executorProxy);

        return new McpRegistration(executorProxy, handlerProxy);
    }
}
