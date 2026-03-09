#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using HotChocolate.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class OpenApiDefinitionRegistry : IAsyncDisposable
{
    private static readonly OpenApiDefinitionValidator s_validator = new();

    private readonly IOpenApiDefinitionStorage _storage;
    private readonly IDynamicOpenApiDocumentTransformer _transformer;
    private readonly IDynamicEndpointDataSource _dynamicEndpointDataSource;
    private readonly SemaphoreSlim _updateSemaphore = new(1, 1);
    private readonly CancellationTokenSource _cancellationTokenSource = new();

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

        _storage.Changed += OnStorageChanged;
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

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;

            _storage.Changed -= OnStorageChanged;
            _updateSemaphore.Dispose();

            await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
            _cancellationTokenSource.Dispose();
        }
    }

    private void OnStorageChanged(object? sender, EventArgs e)
    {
        if (_schema is null)
        {
            return;
        }

        HandleStorageChangedAsync().FireAndForget();
    }

    private async Task HandleStorageChangedAsync()
    {
        if (_schema is null)
        {
            return;
        }

        var cancellationToken = _cancellationTokenSource.Token;

        await _updateSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_schema is null)
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
            _updateSemaphore.Release();
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

        // Update endpoints
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
}
