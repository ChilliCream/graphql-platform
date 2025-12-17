using System.Collections.Immutable;
using HotChocolate.Utilities;
using HotChocolate.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

internal sealed class OpenApiDefinitionRegistry : IAsyncDisposable
{
    private readonly IOpenApiDefinitionStorage _storage;
    private readonly IDynamicOpenApiDocumentTransformer _transformer;
    private readonly IDynamicEndpointDataSource _dynamicEndpointDataSource;
    private readonly SemaphoreSlim _updateSemaphore = new(1, 1);
    private readonly EventObservable _eventObservable = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private ISchemaDefinition? _schema;
    private bool _disposed;

    private ImmutableDictionary<string, OpenApiModelDefinition> _modelsByName
        = ImmutableDictionary<string, OpenApiModelDefinition>.Empty;

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

            await UpdateAllDefinitionsAsync(definitions.ToList(), schema, events, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _updateSemaphore.Release();
        }
    }

    public IDisposable Subscribe(IObserver<OpenApiDocumentEvent> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _eventObservable.Subscribe(observer);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;

            _storage.Changed -= OnStorageChanged;
            _eventObservable.Dispose();
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

            await UpdateAllDefinitionsAsync(definitions.ToList(), _schema, events, cancellationToken).ConfigureAwait(false);
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

    private async Task UpdateAllDefinitionsAsync(
        List<IOpenApiDefinition> definitions,
        ISchemaDefinition schema,
        IOpenApiDiagnosticEvents events,
        CancellationToken cancellationToken)
    {
        var newModels = definitions.OfType<OpenApiModelDefinition>().ToList();
        var newEndpoints = definitions.OfType<OpenApiEndpointDefinition>().ToList();

        var validModelBuilder = ImmutableDictionary.CreateBuilder<string, OpenApiModelDefinition>();
        var validEndpointBuilder = ImmutableDictionary.CreateBuilder<string, OpenApiEndpointDefinition>();

        var documentValidator = schema.Services.GetRequiredService<DocumentValidator>();
        var validationContext = new OpenApiDefinitionValidationContext(
            ImmutableDictionary<string, OpenApiEndpointDefinition>.Empty,
            ImmutableDictionary<string, OpenApiModelDefinition>.Empty,
            schema,
            documentValidator);
        var validator = new OpenApiDefinitionValidator();

        // Validate models
        var validatedModelNames = new HashSet<string>();
        var modelDependenciesByName = new Dictionary<string, HashSet<string>>();

        var modelsByName = new Dictionary<string, List<OpenApiModelDefinition>>();
        foreach (var newModel in newModels)
        {
            if (!modelsByName.TryGetValue(newModel.Name, out var modelsWithName))
            {
                modelsWithName = [];
                modelsByName[newModel.Name] = modelsWithName;
                modelDependenciesByName[newModel.Name] = [.. newModel.ExternalFragmentReferences];
            }

            modelsWithName.Add(newModel);
        }

        var remainingModels = new HashSet<string>(modelsByName.Keys);
        var previousRemainingCount = -1;

        while (remainingModels.Count > 0)
        {
            if (remainingModels.Count == previousRemainingCount)
            {
                // Circular dependency or missing dependencies
                foreach (var modelName in remainingModels)
                {
                    var modelsWithName = modelsByName[modelName];
                    foreach (var model in modelsWithName)
                    {
                        var validationResult =
                            await validator.ValidateAsync(model, validationContext, cancellationToken)
                                .ConfigureAwait(false);

                        if (!validationResult.IsValid)
                        {
                            events.ValidationErrors(validationResult.Errors.Value);
                        }
                    }
                }

                break;
            }

            previousRemainingCount = remainingModels.Count;
            var modelNamesToValidate = new List<string>();

            foreach (var modelName in remainingModels)
            {
                var dependencies = modelDependenciesByName[modelName];
                if (dependencies.Count == 0 || dependencies.All(validatedModelNames.Contains))
                {
                    modelNamesToValidate.Add(modelName);
                }
            }

            foreach (var modelName in modelNamesToValidate)
            {
                var modelsWithName = modelsByName[modelName];
                var allValid = true;

                foreach (var model in modelsWithName)
                {
                    var validationResult =
                        await validator.ValidateAsync(model, validationContext, cancellationToken)
                            .ConfigureAwait(false);

                    if (validationResult.IsValid)
                    {
                        validModelBuilder.Add(model.Name, model);
                        validationContext.ModelsByName.TryAdd(model.Name, model);
                    }
                    else
                    {
                        allValid = false;
                        events.ValidationErrors(validationResult.Errors.Value);
                    }
                }

                if (allValid && modelsWithName.Count > 0)
                {
                    validatedModelNames.Add(modelName);
                }

                remainingModels.Remove(modelName);
            }
        }

        // Validate endpoints
        foreach (var newEndpoint in newEndpoints)
        {
            var validationResult = await validator.ValidateAsync(newEndpoint, validationContext, cancellationToken)
                .ConfigureAwait(false);

            if (!validationResult.IsValid)
            {
                events.ValidationErrors(validationResult.Errors.Value);
            }
            else
            {
                var endpointName = newEndpoint.OperationDefinition.Name!.Value;
                validEndpointBuilder.Add(endpointName, newEndpoint);
                validationContext.EndpointsByName[endpointName] = newEndpoint;
            }
        }

        _modelsByName = validModelBuilder.ToImmutable();

        UpdateEndpointsAndOpenApiDefinitions(
            validEndpointBuilder.Values,
            validModelBuilder.Values,
            schema);
    }

    private void UpdateEndpointsAndOpenApiDefinitions(
        IEnumerable<OpenApiEndpointDefinition> endpoints,
        IEnumerable<OpenApiModelDefinition> models,
        ISchemaDefinition schema)
    {
        var sortedEndpoints = endpoints.OrderBy(e => e.OperationDefinition.Name!.Value).ToArray();
        var sortedModels = models.OrderBy(m => m.Name).ToArray();

        _transformer.AddDefinitions(sortedEndpoints, sortedModels, schema);

        // Update endpoints
        var httpEndpoints = new List<Endpoint>();

        foreach (var endpoint in sortedEndpoints)
        {
            try
            {
                var httpEndpoint = OpenApiEndpointFactory.Create(endpoint, _modelsByName, schema);
                httpEndpoints.Add(httpEndpoint);
            }
            catch
            {
                // If the construction of an endpoint fails, we just skip over it.
            }
        }

        _dynamicEndpointDataSource.SetEndpoints(httpEndpoints);

        _eventObservable.RaiseEvent(OpenApiDocumentEvent.Updated());
    }

    private sealed class EventObservable : IObservable<OpenApiDocumentEvent>, IDisposable
    {
#if NET9_0_OR_GREATER
        private readonly Lock _sync = new();
#else
        private readonly object _sync = new();
#endif
        private readonly List<Subscription> _subscriptions = [];
        private bool _disposed;

        public IDisposable Subscribe(IObserver<OpenApiDocumentEvent> observer)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(observer);

            var subscription = new Subscription(this, observer);

            lock (_sync)
            {
                _subscriptions.Add(subscription);
            }

            return subscription;
        }

        public void RaiseEvent(OpenApiDocumentEvent eventMessage)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            lock (_sync)
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription.Observer.OnNext(eventMessage);
                }
            }
        }

        private void Unsubscribe(Subscription subscription)
        {
            lock (_sync)
            {
                _subscriptions.Remove(subscription);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_sync)
                {
                    foreach (var subscription in _subscriptions)
                    {
                        subscription.Observer.OnCompleted();
                    }

                    _subscriptions.Clear();
                }

                _disposed = true;
            }
        }

        private sealed class Subscription(
            EventObservable parent,
            IObserver<OpenApiDocumentEvent> observer)
            : IDisposable
        {
            private bool _disposed;

            public IObserver<OpenApiDocumentEvent> Observer { get; } = observer;

            public void Dispose()
            {
                if (!_disposed)
                {
                    parent.Unsubscribe(this);
                    _disposed = true;
                }
            }
        }
    }
}
