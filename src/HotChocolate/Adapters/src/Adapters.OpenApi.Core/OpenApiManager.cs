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
    private readonly IOptionsMonitor<OpenApiSetup> _setupMonitor;
    private readonly IOptionsMonitor<OpenApiTransportSetup> _transportSetupMonitor;
    private readonly IServiceProvider _applicationServices;
    private readonly ConcurrentDictionary<string, Lazy<OpenApiRegistration>> _registrations = new();

    public OpenApiManager(
        IOptionsMonitor<OpenApiSetup> setupMonitor,
        IOptionsMonitor<OpenApiTransportSetup> transportSetupMonitor,
        IServiceProvider applicationServices)
    {
        ArgumentNullException.ThrowIfNull(setupMonitor);
        ArgumentNullException.ThrowIfNull(transportSetupMonitor);
        ArgumentNullException.ThrowIfNull(applicationServices);
        _setupMonitor = setupMonitor;
        _transportSetupMonitor = transportSetupMonitor;
        _applicationServices = applicationServices;
    }

    public ImmutableArray<string> Names =>
        _applicationServices
            .GetServices<IConfigureOptions<OpenApiSetup>>()
            .OfType<ConfigureNamedOptions<OpenApiSetup>>()
            .Select(c => c.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .ToImmutableArray()!;

    public OpenApiSetup GetSetup(string? name = null)
    {
        name ??= ISchemaDefinition.DefaultName;
        return _setupMonitor.Get(name);
    }

    public IOpenApiDefinitionStorage GetDefinitionStorage(string? name = null)
        => GetOrAddRegistration(name).Storage;

    public OpenApiDefinitionRegistry GetDefinitionRegistry(string? name = null)
        => GetOrAddRegistration(name).Registry;

    public HttpRequestExecutorProxy GetRequestExecutorProxy(string? name = null)
        => GetOrAddRegistration(name).ExecutorProxy;

    public IDynamicEndpointDataSource GetEndpointDataSource(string? name = null)
        => GetOrAddRegistration(name).EndpointDataSource;

    public IDynamicOpenApiDocumentTransformer GetDocumentTransformer(string? name = null)
        => GetOrAddRegistration(name).DocumentTransformer;

    private OpenApiRegistration GetOrAddRegistration(string? name)
    {
        name ??= ISchemaDefinition.DefaultName;
        return _registrations.GetOrAdd(
            name,
            static (key, manager) => new Lazy<OpenApiRegistration>(
                () => manager.CreateRegistration(key),
                LazyThreadSafetyMode.ExecutionAndPublication),
            this).Value;
    }

    private OpenApiRegistration CreateRegistration(string name)
    {
        var setup = _setupMonitor.Get(name);
        var transportSetup = _transportSetupMonitor.Get(name);

        var storageFactory = setup.StorageFactory
            ?? throw new InvalidOperationException(
                $"No OpenAPI definition storage is registered for schema '{name}'. "
                + "Call AddOpenApiDefinitionStorage(...) when configuring the GraphQL server.");

        var storage = storageFactory(_applicationServices);
        var endpointDataSource = transportSetup.EndpointDataSourceFactory!();
        var documentTransformer = transportSetup.DocumentTransformerFactory!();
        var registry = new OpenApiDefinitionRegistry(storage, documentTransformer, endpointDataSource);
        var executorProxy = HttpRequestExecutorProxy.Create(_applicationServices, name);

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
