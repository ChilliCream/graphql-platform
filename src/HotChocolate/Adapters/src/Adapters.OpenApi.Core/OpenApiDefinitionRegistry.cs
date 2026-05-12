#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Reactive.Linq;
using HotChocolate.Utilities;
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
            .Subscribe(_ => HandleStorageChangedAsync().FireAndForget());
    }

    public async ValueTask UpdateSchemaAsync(
        ISchemaDefinition schema,
        CancellationToken cancellationToken = default)
    {
        await _updateSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _schema = schema;

            var events = schema.Services.GetRequiredService<IOpenApiDiagnosticEvents>();
            var definitions = await _storage.GetDefinitionsAsync(cancellationToken).ConfigureAwait(false);

            UpdateAllDefinitions(definitions.ToList(), schema, events);
        }
        finally
        {
            _updateSemaphore.Release();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            _subscription.Dispose();

            // Cancel before disposing the semaphore so any in-flight WaitAsync(token)
            // observes the cancellation and exits, instead of being orphaned forever
            // in the semaphore's waiter list (Dispose does not complete pending waits).
            _cancellationTokenSource.Cancel();

            _updateSemaphore.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }

    private async Task HandleStorageChangedAsync()
    {
        if (_disposed || _schema is null)
        {
            return;
        }

        var cancellationToken = _cancellationTokenSource.Token;

        try
        {
            await _updateSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
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

            var events = _schema.Services.GetRequiredService<IOpenApiDiagnosticEvents>();
            var definitions = await _storage.GetDefinitionsAsync(cancellationToken).ConfigureAwait(false);

            UpdateAllDefinitions(definitions.ToList(), _schema, events);
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

    private void UpdateAllDefinitions(
        List<IOpenApiDefinition> definitions,
        ISchemaDefinition schema,
        IOpenApiDiagnosticEvents events)
    {
        var validDefinitions = new List<IOpenApiDefinition>();
        var validationContext = new OpenApiDefinitionValidationContext(schema);

        foreach (var definition in definitions)
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

        // When multiple endpoints share (method, route): a descriptor with a valid document
        // wins. If no duplicate produces a valid descriptor, the first one whose document
        // failed validation is kept so the route is still registered (the middleware returns
        // HTTP 500 on call). Descriptors that fail to construct outright are skipped.
        // The chosen descriptors are tracked in a list so registration order is deterministic
        // (first appearance of each key in `endpoints`, which is already sorted by name).
        var chosenDescriptors = new List<OpenApiEndpointDescriptor>();
        var keyToIndex = new Dictionary<(string, string), int>();
        var keyHasValid = new HashSet<(string, string)>();

        foreach (var endpoint in endpoints)
        {
            var key = (endpoint.HttpMethod, endpoint.Route);

            if (keyHasValid.Contains(key))
            {
                continue;
            }

            OpenApiEndpointDescriptor descriptor;

            try
            {
                descriptor = OpenApiEndpointFactory.CreateEndpointDescriptor(endpoint, modelsByName, schema);
            }
            catch
            {
                continue;
            }

            if (descriptor.HasValidDocument)
            {
                if (keyToIndex.TryGetValue(key, out var existingIndex))
                {
                    // Promote: an earlier invalid descriptor is being replaced by a valid one.
                    chosenDescriptors[existingIndex] = descriptor;
                }
                else
                {
                    keyToIndex[key] = chosenDescriptors.Count;
                    chosenDescriptors.Add(descriptor);
                }

                keyHasValid.Add(key);
            }
            else if (!keyToIndex.ContainsKey(key))
            {
                keyToIndex[key] = chosenDescriptors.Count;
                chosenDescriptors.Add(descriptor);
            }
        }

        var httpEndpoints = new List<Endpoint>();

        foreach (var descriptor in chosenDescriptors)
        {
            try
            {
                httpEndpoints.Add(OpenApiEndpointFactory.CreateEndpoint(schema.Name, descriptor));
            }
            catch
            {
                // If wrapping the descriptor in an ASP.NET endpoint fails, we just skip over it.
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
}
