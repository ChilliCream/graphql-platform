using System.Collections.Concurrent;
using System.Collections.Immutable;
using HotChocolate.Adapters.OpenApi.Configuration;
using HotChocolate.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace HotChocolate.Adapters.OpenApi;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode(
    "JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode(
    "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class OpenApiManager : IDisposable
{
    private readonly IOptionsMonitor<OpenApiSetup> _setupMonitor;
    private readonly IOptionsMonitor<OpenApiTransportSetup> _transportSetupMonitor;
    private readonly IServiceProvider _applicationServices;
    private readonly ConcurrentDictionary<string, Lazy<OpenApiRegistration>> _registrations = new();
    private bool _disposed;

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

    public ImmutableArray<string> Names
        => _applicationServices
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

    public OpenApiRegistration Get(string? name = null)
    {
        name ??= ISchemaDefinition.DefaultName;
        return _registrations
            .GetOrAdd(
                name,
                static (key, manager) =>
                    new Lazy<OpenApiRegistration>(
                        () => manager.CreateRegistration(key),
                        LazyThreadSafetyMode.ExecutionAndPublication),
                this)
            .Value;
    }

    private OpenApiRegistration CreateRegistration(string name)
    {
        var setup = _setupMonitor.Get(name);
        var transportSetup = _transportSetupMonitor.Get(name);

        var storageFactory =
            setup.StorageFactory
            ?? throw new InvalidOperationException(
                $"No OpenAPI definition storage is registered for schema '{name}'. "
                    + "Call AddOpenApiDefinitionStorage(...) when configuring the GraphQL server.");

        var endpointDataSourceFactory =
            transportSetup.EndpointDataSourceFactory
            ?? throw new InvalidOperationException(
                $"No OpenAPI endpoint data source factory is registered for schema '{name}'. "
                    + "Call AddOpenApiAspNetCoreServices() when configuring the GraphQL server.");

        var documentTransformerFactory =
            transportSetup.DocumentTransformerFactory
            ?? throw new InvalidOperationException(
                $"No OpenAPI document transformer factory is registered for schema '{name}'. "
                    + "Call AddOpenApiAspNetCoreServices() when configuring the GraphQL server.");

        var storage = storageFactory(_applicationServices);
        var endpointDataSource = endpointDataSourceFactory();
        var documentTransformer = documentTransformerFactory();
        var registry = new OpenApiDefinitionRegistry(storage, documentTransformer, endpointDataSource);
        var executorProxy = HttpRequestExecutorProxy.Create(_applicationServices, name);

        return new OpenApiRegistration(registry, executorProxy, endpointDataSource, documentTransformer);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var lazy in _registrations.Values)
        {
            if (!lazy.IsValueCreated)
            {
                continue;
            }

            var registration = lazy.Value;

            registration.Registry.Dispose();
            registration.ExecutorProxy.Dispose();
        }

        _registrations.Clear();
    }
}
