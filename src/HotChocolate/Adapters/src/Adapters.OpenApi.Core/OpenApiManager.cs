using System.Collections.Concurrent;
using System.Collections.Immutable;
#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using HotChocolate.Adapters.OpenApi.Configuration;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HotChocolate.Adapters.OpenApi;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class OpenApiManager : IOpenApiProvider
{
    private readonly IOptionsMonitor<OpenApiSetup> _optionsMonitor;
    private readonly IServiceProvider _applicationServices;
    private readonly ConcurrentDictionary<string, OpenApiRegistration> _registrations = new();

    public OpenApiManager(
        IOptionsMonitor<OpenApiSetup> optionsMonitor,
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

    public OpenApiSetup GetSetup(string? schemaName = null)
    {
        schemaName ??= ISchemaDefinition.DefaultName;
        return _optionsMonitor.Get(schemaName);
    }

    public IOpenApiDefinitionStorage GetDefinitionStorage(string? schemaName = null)
        => GetOrAddRegistration(schemaName).Storage;

    public OpenApiDefinitionRegistry GetDefinitionRegistry(string? schemaName = null)
        => GetOrAddRegistration(schemaName).Registry;

    public HttpRequestExecutorProxy GetRequestExecutorProxy(string? schemaName = null)
        => GetOrAddRegistration(schemaName).ExecutorProxy;

    public IDynamicEndpointDataSource GetEndpointDataSource(string? schemaName = null)
        => GetOrAddRegistration(schemaName).EndpointDataSource;

    public IDynamicOpenApiDocumentTransformer GetDocumentTransformer(string? schemaName = null)
        => GetOrAddRegistration(schemaName).DocumentTransformer;

    private OpenApiRegistration GetOrAddRegistration(string? schemaName)
    {
        schemaName ??= ISchemaDefinition.DefaultName;
        return _registrations.GetOrAdd(
            schemaName,
            static (name, manager) => manager.CreateRegistration(name),
            this);
    }

    private OpenApiRegistration CreateRegistration(string schemaName)
    {
        var setup = _optionsMonitor.Get(schemaName);

        var storageFactory = setup.StorageFactory
            ?? throw new InvalidOperationException(
                $"No OpenAPI definition storage is registered for schema '{schemaName}'. "
                + "Call AddOpenApiDefinitionStorage(...) when configuring the GraphQL server.");

        var endpointDataSourceFactory = setup.EndpointDataSourceFactory
            ?? throw new InvalidOperationException(
                "No OpenAPI endpoint data source factory is registered. "
                + "Call AddOpenApiDefinitionStorage(...) when configuring the GraphQL server.");

        var documentTransformerFactory = setup.DocumentTransformerFactory
            ?? throw new InvalidOperationException(
                "No OpenAPI document transformer factory is registered. "
                + "Call AddOpenApiDefinitionStorage(...) when configuring the GraphQL server.");

        var storage = storageFactory(_applicationServices);
        var endpointDataSource = endpointDataSourceFactory(_applicationServices, schemaName);
        var documentTransformer = documentTransformerFactory(_applicationServices, schemaName);
        var registry = new OpenApiDefinitionRegistry(storage, documentTransformer, endpointDataSource);
        var executorProxy = HttpRequestExecutorProxy.Create(_applicationServices, schemaName);

        return new OpenApiRegistration(
            storage,
            registry,
            executorProxy,
            endpointDataSource,
            documentTransformer);
    }

    private sealed record OpenApiRegistration(
        IOpenApiDefinitionStorage Storage,
        OpenApiDefinitionRegistry Registry,
        HttpRequestExecutorProxy ExecutorProxy,
        IDynamicEndpointDataSource EndpointDataSource,
        IDynamicOpenApiDocumentTransformer DocumentTransformer);
}
