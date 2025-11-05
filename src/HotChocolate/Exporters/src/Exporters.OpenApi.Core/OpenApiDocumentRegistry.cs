using HotChocolate.Exporters.OpenApi.Validation;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Exporters.OpenApi;

internal sealed class OpenApiDocumentRegistry
    : IDisposable, IObserver<OpenApiDefinitionStorageEventArgs>, IOpenApiValidationContext
{
    private readonly IOpenApiDefinitionStorage _storage;
    private readonly DynamicOpenApiDocumentTransformer _transformer;
    private readonly IDynamicEndpointDataSource _dynamicEndpointDataSource;
    private readonly IDisposable _storageSubscription;
    private readonly SemaphoreSlim _updateSemaphore = new(1, 1);
    private readonly EventObservable _events = new();
    private ISchemaDefinition? _schema;
    private readonly Dictionary<string, OpenApiFragmentDocument> _fragmentDocuments = new();
    private readonly Dictionary<string, OpenApiOperationDocument> _operationDocuments = new();

    public OpenApiDocumentRegistry(
        IOpenApiDefinitionStorage storage,
        DynamicOpenApiDocumentTransformer transformer,
        IDynamicEndpointDataSource dynamicEndpointDataSource)
    {
        _storage = storage;
        _transformer = transformer;
        _dynamicEndpointDataSource = dynamicEndpointDataSource;
        _storageSubscription = storage.Subscribe(this);
    }

    public async ValueTask InitializeAsync(
        ISchemaDefinition schema,
        CancellationToken cancellationToken = default)
    {
        await _updateSemaphore.WaitAsync(cancellationToken);

        try
        {
            _schema = schema;
            var initialDocuments = await _storage.GetDocumentsAsync(cancellationToken);

            var parser = new OpenApiDocumentParser(schema);

            foreach (var document in initialDocuments)
            {
                var parsedDocument = parser.Parse(document.Id, document.Document);

                if (parsedDocument is OpenApiFragmentDocument fragmentDocument)
                {
                    _fragmentDocuments[document.Id] = fragmentDocument;
                }
                else if (parsedDocument is OpenApiOperationDocument operationDocument)
                {
                    _operationDocuments[document.Id] = operationDocument;
                }
            }

            await RebuildTransformsAndEndpointsAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _updateSemaphore.Release();
        }
    }

    public IDisposable Subscribe(IObserver<OpenApiDocumentEvent> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _events.Subscribe(observer);
    }

    public void Dispose()
    {
        _storageSubscription.Dispose();
        _events.Dispose();
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

        // Fire and forget async update - we can't make OnNext async as it's part of IObserver<T>
        _ = Task.Run(async () =>
        {
            await _updateSemaphore.WaitAsync();
            try
            {
                await HandleUpdateAsync(value).ConfigureAwait(false);
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
        var validator = new OpenApiDocumentValidator();

        switch (args.Type)
        {
            case OpenApiDefinitionStorageEventType.Added:
            case OpenApiDefinitionStorageEventType.Modified:
                if (args.Definition is null)
                {
                    return;
                }

                var parsedDocument = parser.Parse(args.Id, args.Definition.Document);

                if (parsedDocument is OpenApiFragmentDocument fragmentDocument)
                {
                    await validator.ValidateAsync(fragmentDocument, this, CancellationToken.None).ConfigureAwait(false);
                    _fragmentDocuments[args.Id] = fragmentDocument;
                }
                else if (parsedDocument is OpenApiOperationDocument operationDocument)
                {
                    await validator.ValidateAsync(operationDocument, this, CancellationToken.None).ConfigureAwait(false);
                    _operationDocuments[args.Id] = operationDocument;
                }

                await RebuildTransformsAndEndpointsAsync(CancellationToken.None).ConfigureAwait(false);
                _events.RaiseEvent(OpenApiDocumentEvent.Updated());
                break;

            case OpenApiDefinitionStorageEventType.Removed:
                var removed = false;
                if (_fragmentDocuments.Remove(args.Id))
                {
                    removed = true;
                }
                else if (_operationDocuments.Remove(args.Id))
                {
                    removed = true;
                }

                if (removed)
                {
                    await RebuildTransformsAndEndpointsAsync(CancellationToken.None).ConfigureAwait(false);
                    _events.RaiseEvent(OpenApiDocumentEvent.Updated());
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(args.Type), args.Type, "Unknown event type");
        }
    }

    private async ValueTask RebuildTransformsAndEndpointsAsync(CancellationToken cancellationToken)
    {
        if (_schema is null)
        {
            return;
        }

        var fragmentDocumentLookup = _fragmentDocuments.Values.ToDictionary(f => f.Name, f => f);
        var operationDocuments = _operationDocuments.Values.ToList();

        var validator = new OpenApiDocumentValidator();

        var validFragments = new List<OpenApiFragmentDocument>();
        var validOperations = new List<OpenApiOperationDocument>();

        // TODO: We need a queue mechanism here that resolves dependencies between fragment definitions
        foreach (var (_, fragmentDocument) in fragmentDocumentLookup)
        {
            await validator.ValidateAsync(fragmentDocument, this, cancellationToken).ConfigureAwait(false);
            validFragments.Add(fragmentDocument);
        }

        foreach (var operationDocument in operationDocuments)
        {
            await validator.ValidateAsync(operationDocument, this, cancellationToken).ConfigureAwait(false);
            validOperations.Add(operationDocument);
        }

        _transformer.AddDocuments(validOperations, validFragments, _schema);

        var endpoints = new List<Endpoint>();

        foreach (var operationDocument in validOperations)
        {
            try
            {
                var endpoint = OpenApiEndpointFactory.Create(operationDocument, fragmentDocumentLookup, _schema);
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
