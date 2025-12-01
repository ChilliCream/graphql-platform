using System.Collections.Immutable;
using System.Threading.Channels;
using HotChocolate.Utilities;
using HotChocolate.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

internal sealed class OpenApiDocumentManager : IAsyncDisposable, IObserver<OpenApiDefinitionStorageEventArgs>
{
    private readonly IOpenApiDefinitionStorage _storage;
    private readonly DynamicOpenApiDocumentTransformer _transformer;
    private readonly IDynamicEndpointDataSource _dynamicEndpointDataSource;
    private readonly IDisposable _storageSubscription;
    private readonly SemaphoreSlim _updateSemaphore = new(1, 1);
    private readonly EventObservable _eventObservable = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly CancellationToken _cancellationToken;

    private readonly Channel<OpenApiDefinitionStorageEventArgs> _channel =
        Channel.CreateBounded<OpenApiDefinitionStorageEventArgs>(
            new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

    private ISchemaDefinition? _schema;
    private bool _disposed;

    private ImmutableSortedDictionary<string, OpenApiFragmentDocument> _fragmentsById
        = ImmutableSortedDictionary<string, OpenApiFragmentDocument>.Empty;

    private ImmutableSortedDictionary<string, OpenApiOperationDocument> _operationsById
        = ImmutableSortedDictionary<string, OpenApiOperationDocument>.Empty;

    private ImmutableDictionary<string, OpenApiFragmentDocument> _fragmentsByName
#if NET10_0_OR_GREATER
        = [];
#else
        = ImmutableDictionary<string, OpenApiFragmentDocument>.Empty;
#endif

    public OpenApiDocumentManager(
        IOpenApiDefinitionStorage storage,
        DynamicOpenApiDocumentTransformer transformer,
        IDynamicEndpointDataSource dynamicEndpointDataSource)
    {
        _storage = storage;
        _transformer = transformer;
        _dynamicEndpointDataSource = dynamicEndpointDataSource;

        _cancellationToken = _cancellationTokenSource.Token;

        WaitForUpdatesAsync().FireAndForget();

        _storageSubscription = storage.Subscribe(this);
    }

    public async ValueTask UpdateSchemaAsync(
        ISchemaDefinition schema,
        CancellationToken cancellationToken = default)
    {
        await _updateSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        var isInitialized = _schema is not null;

        try
        {
            _schema = schema;

            var events = schema.Services.GetRequiredService<IOpenApiDiagnosticEvents>();

            if (!isInitialized)
            {
                var rawDocuments = await _storage.GetDocumentsAsync(cancellationToken).ConfigureAwait(false);

                var parser = new OpenApiDocumentParser(schema);
                var newDocuments = new List<IOpenApiDocument>();

                foreach (var document in rawDocuments)
                {
                    var parseResult = parser.Parse(document);

                    if (parseResult.IsValid)
                    {
                        newDocuments.Add(parseResult.Document);
                    }
                    else if (!parseResult.IsValid)
                    {
                        events.ValidationErrors(parseResult.Errors);
                    }
                }

                await InitializeAsync(newDocuments, schema, events, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await UpdateAllDocumentsAsync(schema, events, cancellationToken).ConfigureAwait(false);
            }
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

            _storageSubscription.Dispose();
            _eventObservable.Dispose();
            _updateSemaphore.Dispose();

            _channel.Writer.TryComplete();
            await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
            _cancellationTokenSource.Dispose();

            while (_channel.Reader.TryRead(out _))
            {
            }
        }
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(OpenApiDefinitionStorageEventArgs value)
    {
        _channel.Writer.TryWrite(value);
    }

    private async Task WaitForUpdatesAsync()
    {
        await foreach (var args in _channel.Reader.ReadAllAsync(_cancellationToken).ConfigureAwait(false))
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (_schema is null)
            {
                continue;
            }

            await _updateSemaphore.WaitAsync(_cancellationToken).ConfigureAwait(false);

            try
            {
                await HandleUpdateAsync(args, _schema, _cancellationToken).ConfigureAwait(false);
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
    }

    private async Task HandleUpdateAsync(
        OpenApiDefinitionStorageEventArgs args,
        ISchemaDefinition schema,
        CancellationToken cancellationToken)
    {
        var parser = new OpenApiDocumentParser(schema);
        var events = schema.Services.GetRequiredService<IOpenApiDiagnosticEvents>();

        if (args.Type is OpenApiDefinitionStorageEventType.Added or OpenApiDefinitionStorageEventType.Modified
            && args.Definition is not null)
        {
            var parseResult = parser.Parse(args.Definition);

            if (parseResult.IsValid)
            {
                if (args.Type is OpenApiDefinitionStorageEventType.Added)
                {
                    await AddDocumentAsync(parseResult.Document, schema, events, cancellationToken)
                        .ConfigureAwait(false);
                }
                else if (args.Type is OpenApiDefinitionStorageEventType.Modified)
                {
                    await UpdateDocumentAsync(parseResult.Document, schema, events, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            else if (!parseResult.IsValid)
            {
                events.ValidationErrors(parseResult.Errors);
            }
        }
        else if (args.Type is OpenApiDefinitionStorageEventType.Removed)
        {
            RemoveDocument(args.Id, schema, events);
        }
    }

    private Task InitializeAsync(
        List<IOpenApiDocument> documents,
        ISchemaDefinition schema,
        IOpenApiDiagnosticEvents events,
        CancellationToken cancellationToken)
    {
        var newFragments = documents.OfType<OpenApiFragmentDocument>().ToList();
        var newOperations = documents.OfType<OpenApiOperationDocument>().ToList();

        return UpdateAllDocumentsInternalAsync(newOperations, newFragments, schema, events, cancellationToken);
    }

    private Task UpdateAllDocumentsAsync(
        ISchemaDefinition schema,
        IOpenApiDiagnosticEvents events,
        CancellationToken cancellationToken)
    {
        return UpdateAllDocumentsInternalAsync(
            _operationsById.Values,
            _fragmentsById.Values,
            schema,
            events,
            cancellationToken);
    }

    /// <summary>
    /// An addition is pure in the sense that it does not affect other documents.
    /// Therefore, we only need to run validation on this single new document.
    /// </summary>
    private async Task AddDocumentAsync(
        IOpenApiDocument document,
        ISchemaDefinition schema,
        IOpenApiDiagnosticEvents events,
        CancellationToken cancellationToken)
    {
        if (document is OpenApiOperationDocument && _operationsById.ContainsKey(document.Id))
        {
            var error = new OpenApiValidationError(
                $"Tried to add a new operation document with Id '{document.Id}', but a document with this Id already exists.",
                document);
            events.ValidationErrors([error]);
            return;
        }

        if (document is OpenApiFragmentDocument && _fragmentsById.ContainsKey(document.Id))
        {
            var error = new OpenApiValidationError(
                $"Tried to add a new fragment document with Id '{document.Id}', but a document with this Id already exists.",
                document);
            events.ValidationErrors([error]);
            return;
        }

        var documentValidator = schema.Services.GetRequiredService<DocumentValidator>();

        var validationContext = new OpenApiValidationContext(
            _operationsById,
            _fragmentsById,
            schema,
            documentValidator);
        var validator = new OpenApiDocumentValidator();

        var validationResult = await validator.ValidateAsync(document, validationContext, cancellationToken)
            .ConfigureAwait(false);

        if (!validationResult.IsValid)
        {
            events.ValidationErrors(validationResult.Errors);
            return;
        }

        if (document is OpenApiOperationDocument operation)
        {
            _operationsById = _operationsById.SetItem(operation.Id, operation);
        }
        else if (document is OpenApiFragmentDocument fragment)
        {
            _fragmentsById = _fragmentsById.SetItem(fragment.Id, fragment);
            _fragmentsByName = _fragmentsByName.SetItem(fragment.Name, fragment);
        }

        UpdateEndpointsAndOpenApiDefinitions(schema);
    }

    // TODO: This should not perform the update, if it brings the system into an invalid state.
    private async Task UpdateDocumentAsync(
        IOpenApiDocument document,
        ISchemaDefinition schema,
        IOpenApiDiagnosticEvents events,
        CancellationToken cancellationToken)
    {
        if (document is OpenApiOperationDocument operation)
        {
            if (_operationsById.ContainsKey(document.Id))
            {
                var newOperations = _operationsById.SetItem(operation.Id, operation);

                await UpdateAllDocumentsInternalAsync(
                    newOperations.Values,
                    _fragmentsById.Values,
                    schema,
                    events,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await AddDocumentAsync(document, schema, events, cancellationToken).ConfigureAwait(false);
            }
        }
        else if (document is OpenApiFragmentDocument fragment)
        {
            if (_fragmentsById.ContainsKey(document.Id))
            {
                var newFragments = _fragmentsById.SetItem(fragment.Id, fragment);

                await UpdateAllDocumentsInternalAsync(
                    _operationsById.Values,
                    newFragments.Values,
                    schema,
                    events,
                    cancellationToken).ConfigureAwait(false);

                await AddDocumentAsync(document, schema, events, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await AddDocumentAsync(document, schema, events, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private void RemoveDocument(string id, ISchemaDefinition schema, IOpenApiDiagnosticEvents events)
    {
        // Fragments can only be removed if they are no longer being referenced.
        if (_fragmentsById.TryGetValue(id, out var fragmentToRemove))
        {
            var referencingOperations = new List<OpenApiOperationDocument>();
            foreach (var operation in _operationsById.Values)
            {
                if (operation.ExternalFragmentReferences.Contains(fragmentToRemove.Name))
                {
                    referencingOperations.Add(operation);
                }
            }

            if (referencingOperations.Count > 0)
            {
                var operationNames = string.Join(", ", referencingOperations
                    .Select(o => $"'{o.Name}'"));
                var error = new OpenApiValidationError(
                    $"Cannot remove fragment '{fragmentToRemove.Name}' because it is still referenced by the following operations: {operationNames}.",
                    fragmentToRemove);

                events.ValidationErrors([error]);
                return;
            }

            _fragmentsById = _fragmentsById.Remove(id);

            UpdateEndpointsAndOpenApiDefinitions(schema);
        }
        else if (_operationsById.ContainsKey(id))
        {
            _operationsById = _operationsById.Remove(id);

            UpdateEndpointsAndOpenApiDefinitions(schema);
        }
    }

    private async Task UpdateAllDocumentsInternalAsync(
        IEnumerable<OpenApiOperationDocument> newOperations,
        IEnumerable<OpenApiFragmentDocument> newFragments,
        ISchemaDefinition schema,
        IOpenApiDiagnosticEvents events,
        CancellationToken cancellationToken)
    {
        var validFragmentBuilder = ImmutableDictionary.CreateBuilder<string, OpenApiFragmentDocument>();
        var validOperationBuilder = ImmutableDictionary.CreateBuilder<string, OpenApiOperationDocument>();

        var documentValidator = schema.Services.GetRequiredService<DocumentValidator>();
        var validationContext = new OpenApiValidationContext(
            _operationsById,
            _fragmentsById,
            schema,
            documentValidator);
        var validator = new OpenApiDocumentValidator();

        // Validate fragments
        var validatedFragmentNames = new HashSet<string>();
        var validatedFragmentIds = new HashSet<string>();
        var fragmentDependenciesByName = new Dictionary<string, HashSet<string>>();

        var fragmentsByName = new Dictionary<string, List<OpenApiFragmentDocument>>();
        foreach (var newFragment in newFragments)
        {
            if (!fragmentsByName.TryGetValue(newFragment.Name, out var fragmentsWithName))
            {
                fragmentsWithName = [];
                fragmentsByName[newFragment.Name] = fragmentsWithName;
                fragmentDependenciesByName[newFragment.Name] = [.. newFragment.ExternalFragmentReferences];
            }

            fragmentsWithName.Add(newFragment);
        }

        var remainingFragments = new HashSet<string>(fragmentsByName.Keys);
        var previousRemainingCount = -1;

        while (remainingFragments.Count > 0)
        {
            if (remainingFragments.Count == previousRemainingCount)
            {
                // Circular dependency or missing dependencies
                foreach (var fragmentName in remainingFragments)
                {
                    var fragmentsWithName = fragmentsByName[fragmentName];
                    foreach (var fragment in fragmentsWithName)
                    {
                        if (validatedFragmentIds.Contains(fragment.Id))
                        {
                            continue;
                        }

                        var validationResult =
                            await validator.ValidateAsync(fragment, validationContext, cancellationToken)
                                .ConfigureAwait(false);

                        if (!validationResult.IsValid)
                        {
                            events.ValidationErrors(validationResult.Errors.Value);
                            validatedFragmentIds.Add(fragment.Id);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }

                break;
            }

            previousRemainingCount = remainingFragments.Count;
            var fragmentNamesToValidate = new List<string>();

            foreach (var fragmentName in remainingFragments)
            {
                var dependencies = fragmentDependenciesByName[fragmentName];
                if (dependencies.Count == 0 || dependencies.All(validatedFragmentNames.Contains))
                {
                    fragmentNamesToValidate.Add(fragmentName);
                }
            }

            foreach (var fragmentName in fragmentNamesToValidate)
            {
                var fragmentsWithName = fragmentsByName[fragmentName];
                var allValid = true;

                foreach (var fragment in fragmentsWithName)
                {
                    if (validatedFragmentIds.Contains(fragment.Id))
                    {
                        continue;
                    }

                    var validationResult =
                        await validator.ValidateAsync(fragment, validationContext, cancellationToken)
                            .ConfigureAwait(false);

                    if (validationResult.IsValid)
                    {
                        validatedFragmentIds.Add(fragment.Id);
                        validFragmentBuilder.Add(fragment.Id, fragment);
                        // Only add first valid fragment with this name to context
                        // Subsequent duplicates will be caught by validation rules
                        validationContext.FragmentsByName.TryAdd(fragment.Name, fragment);
                    }
                    else
                    {
                        allValid = false;
                        events.ValidationErrors(validationResult.Errors.Value);
                        validatedFragmentIds.Add(fragment.Id);
                    }
                }

                if (allValid && fragmentsWithName.Count > 0)
                {
                    validatedFragmentNames.Add(fragmentName);
                }

                remainingFragments.Remove(fragmentName);
            }
        }

        // Validate operations
        foreach (var newOperation in newOperations)
        {
            var validationResult = await validator.ValidateAsync(newOperation, validationContext, cancellationToken)
                .ConfigureAwait(false);

            if (!validationResult.IsValid)
            {
                events.ValidationErrors(validationResult.Errors.Value);
            }
            else
            {
                validOperationBuilder.Add(newOperation.Id, newOperation);
                validationContext.OperationsByName[newOperation.Name] = newOperation;
            }
        }

        _operationsById = validOperationBuilder.ToImmutableSortedDictionary();
        _fragmentsById = validFragmentBuilder.ToImmutableSortedDictionary();
        _fragmentsByName = _fragmentsById.ToImmutableDictionary(f => f.Value.Name, f => f.Value);

        UpdateEndpointsAndOpenApiDefinitions(schema);
    }

    private void UpdateEndpointsAndOpenApiDefinitions(ISchemaDefinition schema)
    {
        // Update OpenAPI definitions
        var sortedOperations = _operationsById.Values.OrderBy(o => o.Name).ToArray();
        var sortedFragments = _fragmentsById.Values.OrderBy(f => f.Name).ToArray();

        _transformer.AddDocuments(sortedOperations, sortedFragments, schema);

        // Update endpoints
        var endpoints = new List<Endpoint>();

        foreach (var operationDocument in sortedOperations)
        {
            try
            {
                var endpoint = OpenApiEndpointFactory.Create(operationDocument, _fragmentsByName, schema);
                endpoints.Add(endpoint);
            }
            catch
            {
                // If the construction of an endpoint fails, we just skip over it.
            }
        }

        _dynamicEndpointDataSource.SetEndpoints(endpoints);

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
