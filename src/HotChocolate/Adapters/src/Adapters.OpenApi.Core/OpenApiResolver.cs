using System.Collections.Concurrent;
#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using HotChocolate.Adapters.OpenApi.Configuration;
using HotChocolate.AspNetCore;
using Microsoft.Extensions.Options;

namespace HotChocolate.Adapters.OpenApi;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class OpenApiResolver
{
    private readonly IServiceProvider _services;
    private readonly IOptionsMonitor<OpenApiSetup> _setupMonitor;
    private readonly ConcurrentDictionary<string, IOpenApiDefinitionStorage> _storages = new();
    private readonly ConcurrentDictionary<string, OpenApiDefinitionRegistry> _registries = new();
    private readonly ConcurrentDictionary<string, HttpRequestExecutorProxy> _executorProxies = new();
    private readonly ConcurrentDictionary<string, IDynamicEndpointDataSource> _endpointDataSources = new();
    private readonly ConcurrentDictionary<string, IDynamicOpenApiDocumentTransformer> _documentTransformers = new();

    public OpenApiResolver(
        IServiceProvider services,
        IOptionsMonitor<OpenApiSetup> setupMonitor)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(setupMonitor);
        _services = services;
        _setupMonitor = setupMonitor;
    }

    public IOpenApiDefinitionStorage GetDefinitionStorage(string schemaName)
    {
        ArgumentNullException.ThrowIfNull(schemaName);
        return _storages.GetOrAdd(
            schemaName,
            static (name, state) =>
            {
                var factory = state._setupMonitor.Get(name).StorageFactory
                    ?? throw new InvalidOperationException(
                        $"No OpenAPI definition storage is registered for schema '{name}'. "
                        + "Call AddOpenApiDefinitionStorage(...) when configuring the GraphQL server.");
                return factory(state._services);
            },
            this);
    }

    public OpenApiDefinitionRegistry GetDefinitionRegistry(string schemaName)
    {
        ArgumentNullException.ThrowIfNull(schemaName);
        return _registries.GetOrAdd(
            schemaName,
            static (name, state) => new OpenApiDefinitionRegistry(
                state.GetDefinitionStorage(name),
                state.GetDocumentTransformer(name),
                state.GetEndpointDataSource(name)),
            this);
    }

    public HttpRequestExecutorProxy GetRequestExecutorProxy(string schemaName)
    {
        ArgumentNullException.ThrowIfNull(schemaName);
        return _executorProxies.GetOrAdd(
            schemaName,
            static (name, state) => HttpRequestExecutorProxy.Create(state._services, name),
            this);
    }

    public IDynamicEndpointDataSource GetEndpointDataSource(string schemaName)
    {
        ArgumentNullException.ThrowIfNull(schemaName);
        return _endpointDataSources.GetOrAdd(
            schemaName,
            static (name, state) =>
            {
                var factory = state._setupMonitor.Get(name).EndpointDataSourceFactory
                    ?? throw new InvalidOperationException(
                        "No OpenAPI endpoint data source factory is registered. "
                        + "Call AddOpenApiDefinitionStorage(...) when configuring the GraphQL server.");
                return factory(state._services, name);
            },
            this);
    }

    public IDynamicOpenApiDocumentTransformer GetDocumentTransformer(string schemaName)
    {
        ArgumentNullException.ThrowIfNull(schemaName);
        return _documentTransformers.GetOrAdd(
            schemaName,
            static (name, state) =>
            {
                var factory = state._setupMonitor.Get(name).DocumentTransformerFactory
                    ?? throw new InvalidOperationException(
                        "No OpenAPI document transformer factory is registered. "
                        + "Call AddOpenApiDefinitionStorage(...) when configuring the GraphQL server.");
                return factory(state._services, name);
            },
            this);
    }
}
