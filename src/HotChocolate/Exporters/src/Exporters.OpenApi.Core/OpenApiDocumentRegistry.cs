using HotChocolate.Exporters.OpenApi.Validation;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Exporters.OpenApi;

// TODO: Incremental updates
internal sealed class OpenApiDocumentRegistry
    : IDisposable, IObserver<OpenApiDefinitionStorageEventArgs>, IOpenApiValidationContext
{
    private readonly IOpenApiDefinitionStorage _storage;
    private readonly DynamicOpenApiDocumentTransformer _transformer;
    private readonly IDynamicEndpointDataSource _dynamicEndpointDataSource;
    private readonly IDisposable _storageSubscription;
    private bool _initialized;

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
        if (!_initialized)
        {
            var initialDocuments = await _storage.GetDocumentsAsync(cancellationToken);

            var parser = new OpenApiDocumentParser(schema);
            var fragmentDocumentLookup = new Dictionary<string, OpenApiFragmentDocument>();
            var operationDocuments = new List<OpenApiOperationDocument>();

            foreach (var document in initialDocuments)
            {
                var parsedDocument = parser.Parse(document.Id, document.Document);

                if (parsedDocument is OpenApiFragmentDocument fragmentDocument)
                {
                    fragmentDocumentLookup.Add(fragmentDocument.Name, fragmentDocument);
                }
                else if (parsedDocument is OpenApiOperationDocument operationDocument)
                {
                    operationDocuments.Add(operationDocument);
                }
            }

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

            _transformer.AddDocuments(validOperations, validFragments, schema);

            var endpoints = new List<Endpoint>();

            foreach (var operationDocument in validOperations)
            {
                try
                {
                    var endpoint = OpenApiEndpointFactory.Create(operationDocument, fragmentDocumentLookup, schema);

                    endpoints.Add(endpoint);
                }
                catch
                {
                    // If the construction of an endpoint fails, we just skip over it.
                }
            }

            _dynamicEndpointDataSource.SetEndpoints(endpoints);

            _initialized = true;
        }
    }

    public void Dispose()
    {
        _storageSubscription.Dispose();
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
    }
}
