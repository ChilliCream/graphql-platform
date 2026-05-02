#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Collections.Immutable;
using System.Reactive.Linq;
using HotChocolate.Adapters.OpenApi.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode(
    "JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode(
    "JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class OpenApiDefinitionRegistry : IDisposable
{
    private static readonly OpenApiDefinitionValidator s_validator = new();

    private readonly IOpenApiDefinitionStorage _storage;
    private readonly IDynamicOpenApiDocumentTransformer _transformer;
    private readonly IDynamicEndpointDataSource _dynamicEndpointDataSource;
    private readonly SemaphoreSlim _updateSemaphore = new(1, 1);
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly IDisposable _subscription;

    private ISchemaDefinition? _schema;
    private ImmutableSortedDictionary<string, IOpenApiDefinition> _definitionsByName
        = ImmutableSortedDictionary<string, IOpenApiDefinition>.Empty;
    private bool _initialized;
    private bool _disposed;

    public OpenApiDefinitionRegistry(
        IOpenApiDefinitionStorage storage,
        IDynamicOpenApiDocumentTransformer transformer,
        IDynamicEndpointDataSource dynamicEndpointDataSource)
    {
        _storage = storage;
        _transformer = transformer;
        _dynamicEndpointDataSource = dynamicEndpointDataSource;

        _subscription = _storage
            .Buffer(TimeSpan.FromMilliseconds(500), 10)
            .Where(batch => batch.Count > 0)
            .Subscribe(ProcessBatch);
    }

    public async ValueTask UpdateSchemaAsync(
        ISchemaDefinition schema,
        CancellationToken cancellationToken = default)
    {
        await _updateSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _schema = schema;

            if (!_initialized)
            {
                var definitions = await _storage.GetDefinitionsAsync(cancellationToken).ConfigureAwait(false);
                var builder = ImmutableSortedDictionary.CreateBuilder<string, IOpenApiDefinition>();

                foreach (var definition in definitions)
                {
                    // First wins on duplicate canonical keys, mirroring the de-duping
                    // behavior the registry has always had at the endpoint and model lookup layer.
                    builder.TryAdd(GetDefinitionKey(definition), definition);
                }

                _definitionsByName = builder.ToImmutable();
                _initialized = true;
            }

            Rebuild(schema);
        }
        finally
        {
            _updateSemaphore.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _subscription.Dispose();

        // Cancel before disposing the semaphore so any in-flight WaitAsync(token)
        // observes the cancellation and exits, instead of being orphaned forever
        // in the semaphore's waiter list (Dispose does not complete pending waits).
        _cancellationTokenSource.Cancel();

        _updateSemaphore.Dispose();
        _cancellationTokenSource.Dispose();
    }

    private void ProcessBatch(IList<OpenApiDefinitionStorageEventArgs> batch)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _updateSemaphore.Wait(_cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        try
        {
            if (_disposed || _schema is null)
            {
                return;
            }

            foreach (var eventArg in batch)
            {
                switch (eventArg.Type)
                {
                    case OpenApiDefinitionStorageEventType.Updated:
                        _definitionsByName = _definitionsByName.SetItem(
                            eventArg.Name,
                            eventArg.Definition!);
                        break;

                    case OpenApiDefinitionStorageEventType.Removed:
                        _definitionsByName = _definitionsByName.Remove(eventArg.Name);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            Rebuild(_schema);
        }
        catch
        {
            // We ignore any unexpected exceptions while processing updates.
        }
        finally
        {
            try
            {
                _updateSemaphore.Release();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    private void Rebuild(ISchemaDefinition schema)
    {
        var events = schema.Services.GetRequiredService<IOpenApiDiagnosticEvents>();
        var validationContext = new OpenApiDefinitionValidationContext(schema);

        var validDefinitions = new List<IOpenApiDefinition>(_definitionsByName.Count);

        foreach (var definition in _definitionsByName.Values)
        {
            var validationResult = s_validator.Validate(definition, validationContext);

            if (!validationResult.IsValid)
            {
                events.ValidationErrors(validationResult.Errors.Value);
                continue;
            }

            validDefinitions.Add(definition);
        }

        UpdateEndpointsAndOpenApiDefinitions(validDefinitions, schema);
    }

    private void UpdateEndpointsAndOpenApiDefinitions(
        List<IOpenApiDefinition> definitions,
        ISchemaDefinition schema)
    {
        var endpoints = definitions
            .OfType<OpenApiEndpointDefinition>()
            .OrderBy(e => e.OperationDefinition.Name?.Value)
            .ToArray();
        var models = definitions
            .OfType<OpenApiModelDefinition>()
            .OrderBy(m => m.Name)
            .ToArray();

        var modelsByName = CreateModelsByNameLookup(models);

        _transformer.AddDefinitions(endpoints, models, modelsByName, schema);

        var httpEndpoints = new List<Endpoint>();
        var processedEndpoints = new HashSet<(string, string)>();

        foreach (var endpoint in endpoints)
        {
            var key = (endpoint.HttpMethod, endpoint.Route);

            if (!processedEndpoints.Add(key))
            {
                continue;
            }

            try
            {
                var httpEndpoint = OpenApiEndpointFactory.Create(endpoint, modelsByName, schema);
                httpEndpoints.Add(httpEndpoint);
            }
            catch
            {
                // If the construction of an endpoint fails, we just skip over it.
            }
        }

        _dynamicEndpointDataSource.SetEndpoints(httpEndpoints);
    }

    private static Dictionary<string, OpenApiModelDefinition> CreateModelsByNameLookup(
        IEnumerable<OpenApiModelDefinition> models)
    {
        var lookup = new Dictionary<string, OpenApiModelDefinition>();

        foreach (var model in models)
        {
            lookup.TryAdd(model.Name, model);
        }

        return lookup;
    }

    // The canonical name a storage must use as <see cref="OpenApiDefinitionStorageEventArgs.Name"/>
    // so that Updated/Removed events line up with the registry's local index.
    internal static string GetDefinitionKey(IOpenApiDefinition definition) => definition switch
    {
        OpenApiEndpointDefinition endpoint => $"{endpoint.HttpMethod} {endpoint.Route}",
        OpenApiModelDefinition model => model.Name,
        _ => throw new InvalidOperationException(
            $"Unknown OpenAPI definition type: {definition.GetType()}.")
    };
}
