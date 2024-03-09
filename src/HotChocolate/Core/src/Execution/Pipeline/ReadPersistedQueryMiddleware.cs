using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class ReadPersistedQueryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private readonly IOperationDocumentStore _operationDocumentStore;

    private ReadPersistedQueryMiddleware(RequestDelegate next,
        [SchemaService] IExecutionDiagnosticEvents diagnosticEvents,
        [SchemaService] IOperationDocumentStore operationDocumentStore)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _diagnosticEvents = diagnosticEvents ??
            throw new ArgumentNullException(nameof(diagnosticEvents));
        _operationDocumentStore = operationDocumentStore ??
            throw new ArgumentNullException(nameof(operationDocumentStore));
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        if (context.Document is null &&
            context.Request.Document is null)
        {
            await TryLoadQueryAsync(context).ConfigureAwait(false);
        }

        await _next(context).ConfigureAwait(false);
    }

    private async ValueTask TryLoadQueryAsync(IRequestContext context)
    {
        var documentId =
            context.Request.DocumentId ??
            context.DocumentId ??
            context.DocumentHash ??
            context.Request.DocumentHash;
        
        if (!OperationDocumentId.IsNullOrEmpty(documentId))
        {
            var operationDocument =
                await _operationDocumentStore.TryReadAsync(
                    documentId.Value, context.RequestAborted)
                    .ConfigureAwait(false);

            if (operationDocument is OperationDocument parsedDoc)
            {
                context.DocumentId = documentId;
                context.Document = parsedDoc.Document;
                context.ValidationResult = DocumentValidatorResult.Ok;
                context.IsCachedDocument = true;
                context.IsPersistedDocument = true;
                _diagnosticEvents.RetrievedDocumentFromStorage(context);
            }
            
            if (operationDocument is OperationDocumentSourceText sourceTextDoc)
            {
                context.DocumentId = documentId;
                context.Document = Utf8GraphQLParser.Parse(sourceTextDoc.AsSpan());
                context.ValidationResult = DocumentValidatorResult.Ok;
                context.IsCachedDocument = true;
                context.IsPersistedDocument = true;
                _diagnosticEvents.RetrievedDocumentFromStorage(context);
            }
        }
    }
    
    public static RequestCoreMiddleware Create()
        => (core, next) =>
        {
            var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();
            var persistedQueryStore = core.SchemaServices.GetRequiredService<IOperationDocumentStore>();
            var middleware = new ReadPersistedQueryMiddleware(next, diagnosticEvents, persistedQueryStore);
            return context => middleware.InvokeAsync(context);
        };
}
