using HotChocolate.Execution;
using HotChocolate.Exporters.OpenApi.Validation;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.Exporters.OpenApi;

// TODO: Make this nicer and independent from executor lifetime
internal sealed class OpenApiWarmupTask(
    IOpenApiDefinitionStorage definitionStorage,
    DynamicOpenApiDocumentTransformer transformer,
    IDynamicEndpointDataSource dynamicEndpointDataSource) : IRequestExecutorWarmupTask
{
    public bool ApplyOnlyOnStartup => true;

    public async Task WarmupAsync(IRequestExecutor executor, CancellationToken cancellationToken)
    {
        var schema = executor.Schema;
        var documents = await definitionStorage.GetDocumentsAsync(cancellationToken);

        // TODO: Maybe this can be static without the schema reference?
        var parser = new OpenApiDocumentParser(schema);
        var fragmentDocumentLookup = new Dictionary<string, OpenApiFragmentDocument>();
        var operationDocuments = new List<OpenApiOperationDocument>();

        foreach (var document in documents)
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
        var context = new OpenApiValidationContext();

        // TODO: We need a queue mechanism here that resolves dependencies between fragment definitions
        foreach (var (_, fragmentDocument) in fragmentDocumentLookup)
        {
            await validator.ValidateAsync(fragmentDocument, context, cancellationToken).ConfigureAwait(false);

            context.AddValidDocument(fragmentDocument);
        }

        foreach (var operationDocument in operationDocuments)
        {
            await validator.ValidateAsync(operationDocument, context, cancellationToken).ConfigureAwait(false);

            context.AddValidDocument(operationDocument);
        }

        transformer.AddDocuments(
            context.ValidOperationDocuments,
            context.ValidFragmentDocuments,
            schema);

        var endpoints = new List<Endpoint>();

        foreach (var operationDocument in operationDocuments)
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

        dynamicEndpointDataSource.SetEndpoints(endpoints);
    }
}
