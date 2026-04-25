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
    private readonly ConcurrentDictionary<string, McpRegistration> _registrations = new();

    public McpManager(
        IOptionsMonitor<McpSetup> optionsMonitor,
        IServiceProvider applicationServices)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(applicationServices);
        _optionsMonitor = optionsMonitor;
        _applicationServices = applicationServices;
        SchemaNames = applicationServices
            .GetService<IRequestExecutorProvider>()?.SchemaNames ?? [];
    }

    public ImmutableArray<string> SchemaNames { get; }

    public McpSetup GetSetup(string? schemaName = null)
    {
        schemaName ??= ISchemaDefinition.DefaultName;
        return _optionsMonitor.Get(schemaName);
    }

    public McpRequestExecutorProxy GetRequestExecutorProxy(string? schemaName = null)
        => GetOrAddRegistration(schemaName).ExecutorProxy;

    public StreamableHttpHandlerProxy GetStreamableHttpHandlerProxy(string? schemaName = null)
        => GetOrAddRegistration(schemaName).HandlerProxy;

    private McpRegistration GetOrAddRegistration(string? schemaName)
    {
        schemaName ??= ISchemaDefinition.DefaultName;
        return _registrations.GetOrAdd(
            schemaName,
            static (name, manager) => manager.CreateRegistration(name),
            this);
    }

    private McpRegistration CreateRegistration(string schemaName)
    {
        var executorProxy = new McpRequestExecutorProxy(
            _applicationServices.GetRequiredService<IRequestExecutorProvider>(),
            _applicationServices.GetRequiredService<IRequestExecutorEvents>(),
            schemaName);

        var handlerProxy = new StreamableHttpHandlerProxy(executorProxy);

        return new McpRegistration(executorProxy, handlerProxy);
    }

    private sealed record McpRegistration(
        McpRequestExecutorProxy ExecutorProxy,
        StreamableHttpHandlerProxy HandlerProxy);
}
