using HotChocolate.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Exporters.OpenApi;

internal sealed class OpenApiDocumentManager : IDisposable, IObserver<OpenApiDefinitionStorageEventArgs>
{
    private readonly IOpenApiDefinitionStorage _storage;
    private readonly DynamicOpenApiDocumentTransformer _transformer;
    private readonly IDynamicEndpointDataSource _dynamicEndpointDataSource;
    private readonly IDisposable _storageSubscription;
    private readonly SemaphoreSlim _updateSemaphore = new(1, 1);
    private readonly EventObservable _eventObservable = new();
    private ISchemaDefinition? _schema;
    private readonly Dictionary<string, OpenApiFragmentDocument> _fragmentsById = [];
    private readonly Dictionary<string, OpenApiOperationDocument> _operationsById = [];

    public OpenApiDocumentManager(
        IOpenApiDefinitionStorage storage,
        DynamicOpenApiDocumentTransformer transformer,
        IDynamicEndpointDataSource dynamicEndpointDataSource)
    {
        _storage = storage;
        _transformer = transformer;
        _dynamicEndpointDataSource = dynamicEndpointDataSource;
        _storageSubscription = storage.Subscribe(this);
    }

    public async ValueTask UpdateSchemaAsync(
        ISchemaDefinition schema,
        CancellationToken cancellationToken = default)
    {
        await _updateSemaphore.WaitAsync(cancellationToken);

        var isInitialized = _schema is not null;

        try
        {
            _schema = schema;

            var newDocuments = new List<IOpenApiDocument>();

            if (!isInitialized)
            {
                var rawDocuments = await _storage.GetDocumentsAsync(cancellationToken);

                var parser = new OpenApiDocumentParser(schema);

                foreach (var document in rawDocuments)
                {
                    var parsedDocument = parser.Parse(document);

                    newDocuments.Add(parsedDocument);
                }
            }

            await RebuildAsync(newDocuments, cancellationToken);
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

    public void Dispose()
    {
        _storageSubscription.Dispose();
        _eventObservable.Dispose();
        _updateSemaphore.Dispose();
    }

    // Observer
    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(OpenApiDefinitionStorageEventArgs value)
    {
        if (_schema is null)
        {
            return;
        }

        // TODO: Maybe we should use channels here?
        _ = Task.Run(async () =>
        {
            await _updateSemaphore.WaitAsync();
            try
            {
                await HandleUpdateAsync(value);
            }
            finally
            {
                _updateSemaphore.Release();
            }
        });
    }

    private async ValueTask HandleUpdateAsync(OpenApiDefinitionStorageEventArgs args)
    {
        if (_schema is null)
        {
            return;
        }

        var parser = new OpenApiDocumentParser(_schema);

        switch (args.Type)
        {
            case OpenApiDefinitionStorageEventType.Added:
            case OpenApiDefinitionStorageEventType.Modified:
                if (args.Definition is null)
                {
                    return;
                }

                var parsedDocument = parser.Parse(args.Definition);

                await RebuildAsync([parsedDocument], CancellationToken.None);

                _eventObservable.RaiseEvent(OpenApiDocumentEvent.Updated());
                break;

            case OpenApiDefinitionStorageEventType.Removed:
                var removed = false;
                if (_fragmentsById.Remove(args.Id))
                {
                    removed = true;
                }
                else if (_operationsById.Remove(args.Id))
                {
                    removed = true;
                }

                if (removed)
                {
                    // TODO: Remove needs to be handled specifically
                    await RebuildAsync([], CancellationToken.None);
                    _eventObservable.RaiseEvent(OpenApiDocumentEvent.Updated());
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(args.Type), args.Type, "Unknown event type");
        }
    }

    private async ValueTask RebuildAsync(
        List<IOpenApiDocument> newDocuments,
        CancellationToken cancellationToken)
    {
        if (_schema is null)
        {
            return;
        }

        var documentValidator = _schema.Services.GetRequiredService<DocumentValidator>();

        var newFragments = new List<OpenApiFragmentDocument>();
        var newOperations = new List<OpenApiOperationDocument>();

        foreach (var newDocument in newDocuments)
        {
            if (newDocument is OpenApiOperationDocument operationDocument)
            {
                newOperations.Add(operationDocument);
            }
            else if (newDocument is OpenApiFragmentDocument fragmentDocument)
            {
                newFragments.Add(fragmentDocument);
            }
        }

        var events = _schema.Services.GetRequiredService<IOpenApiDiagnosticEvents>();
        var validationContext = new OpenApiValidationContext(
            _operationsById.Values,
            _fragmentsById.Values,
            _schema,
            documentValidator);
        var validator = new OpenApiDocumentValidator();

        // Validate fragments
        var validatedFragments = new HashSet<string>();
        var fragmentDependenciesByName = new Dictionary<string, HashSet<string>>();

        foreach (var newFragment in newFragments)
        {
            fragmentDependenciesByName[newFragment.Name] = new HashSet<string>(newFragment.ExternalFragmentReferences);
        }

        var newFragmentsByName = newFragments.ToDictionary(f => f.Name);
        var remainingFragments = new HashSet<string>(newFragmentsByName.Keys);
        var previousRemainingCount = -1;

        while (remainingFragments.Count > 0)
        {
            if (remainingFragments.Count == previousRemainingCount)
            {
                // Circular dependency or missing dependencies
                foreach (var fragmentName in remainingFragments)
                {
                    var fragment = newFragmentsByName[fragmentName];
                    var validationResult = await validator.ValidateAsync(fragment, validationContext, cancellationToken);

                    if (!validationResult.IsValid)
                    {
                        events.ValidationErrors(validationResult.Errors);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }

                break;
            }

            previousRemainingCount = remainingFragments.Count;
            var fragmentNamesToValidate = new List<string>();

            foreach (var fragmentName in remainingFragments)
            {
                var dependencies = fragmentDependenciesByName[fragmentName];
                if (dependencies.Count == 0 || dependencies.All(d => validatedFragments.Contains(d)))
                {
                    fragmentNamesToValidate.Add(fragmentName);
                }
            }

            foreach (var fragmentName in fragmentNamesToValidate)
            {
                var fragment = newFragmentsByName[fragmentName];
                var validationResult = await validator.ValidateAsync(fragment, validationContext, cancellationToken);

                if (validationResult.IsValid)
                {
                    validatedFragments.Add(fragmentName);
                    _fragmentsById[fragment.Id] = fragment;
                    validationContext.FragmentsByName[fragment.Name] = fragment;
                }
                else
                {
                    events.ValidationErrors(validationResult.Errors);
                }

                remainingFragments.Remove(fragmentName);
            }
        }

        // Validate operations
        foreach (var newOperation in newOperations)
        {
            var validationResult = await validator.ValidateAsync(newOperation, validationContext, cancellationToken);

            if (!validationResult.IsValid)
            {
                events.ValidationErrors(validationResult.Errors);
            }
            else
            {
                _operationsById[newOperation.Id] = newOperation;
                validationContext.OperationsByName[newOperation.Name] = newOperation;
            }
        }

        // Update OpenAPI definitions
        _transformer.AddDocuments(_operationsById.Values, _fragmentsById.Values, _schema);

        // Update endpoints
        var endpoints = new List<Endpoint>();

        foreach (var operationDocument in _operationsById.Values)
        {
            try
            {
                var endpoint = OpenApiEndpointFactory.Create(operationDocument, validationContext.FragmentsByName, _schema);
                endpoints.Add(endpoint);
            }
            catch
            {
                // If the construction of an endpoint fails, we just skip over it.
            }
        }

        _dynamicEndpointDataSource.SetEndpoints(endpoints);
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
