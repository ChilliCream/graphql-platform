using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using HotChocolate.Language;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class ReadPersistedOperationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;
    private readonly IOperationDocumentStorage _operationDocumentStorage;
    private readonly IDocumentHashProvider _documentHashAlgorithm;
    private readonly PersistedOperationOptions _options;

    private ReadPersistedOperationMiddleware(
        RequestDelegate next,
        [SchemaService] IExecutionDiagnosticEvents diagnosticEvents,
        [SchemaService] IOperationDocumentStorage operationDocumentStorage,
        IDocumentHashProvider documentHashAlgorithm,
        PersistedOperationOptions options)
    {
        _next = next ??
            throw new ArgumentNullException(nameof(next));
        _diagnosticEvents = diagnosticEvents ??
            throw new ArgumentNullException(nameof(diagnosticEvents));
        _operationDocumentStorage = operationDocumentStorage ??
            throw new ArgumentNullException(nameof(operationDocumentStorage));
        _documentHashAlgorithm = documentHashAlgorithm ??
            throw new ArgumentNullException(nameof(documentHashAlgorithm));
        _options = options;
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        if (context.Document is null)
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
                await _operationDocumentStorage.TryReadAsync(
                    documentId.Value, context.RequestAborted)
                    .ConfigureAwait(false);

            if (operationDocument is not null)
            {
                context.DocumentId = documentId;
                context.Document = GetOrParseDocument(operationDocument);
                context.DocumentHash = GetDocumentHash(operationDocument);
                context.ValidationResult = DocumentValidatorResult.OK;
                context.IsCachedDocument = true;
                context.IsPersistedDocument = true;

                if (_options.SkipPersistedDocumentValidation)
                {
                    context.ValidationResult = DocumentValidatorResult.OK;
                }

                _diagnosticEvents.RetrievedDocumentFromStorage(context);
            }
        }
    }

    private static DocumentNode GetOrParseDocument(IOperationDocument document)
    {
        if (document is IOperationDocumentNodeProvider nodeProvider)
        {
            return nodeProvider.Document;
        }

        return Utf8GraphQLParser.Parse(document.AsSpan());
    }

    private string? GetDocumentHash(IOperationDocument document)
    {
        if (document is IOperationDocumentHashProvider hashProvider
            && _documentHashAlgorithm.Name.Equals(hashProvider.Hash.AlgorithmName)
            && _documentHashAlgorithm.Format.Equals(hashProvider.Hash.Format))
        {
            return hashProvider.Hash.Hash;
        }

        return null;
    }

    public static RequestCoreMiddlewareConfiguration Create()
        => new RequestCoreMiddlewareConfiguration(
            (core, next) =>
            {
                var diagnosticEvents = core.SchemaServices.GetRequiredService<IExecutionDiagnosticEvents>();
                var persistedOperationStore = core.SchemaServices.GetRequiredService<IOperationDocumentStorage>();
                var documentHashAlgorithm = core.Services.GetRequiredService<IDocumentHashProvider>();
                var middleware = new ReadPersistedOperationMiddleware(
                    next,
                    diagnosticEvents,
                    persistedOperationStore,
                    documentHashAlgorithm,
                    core.Options.PersistedOperations);
                return context => middleware.InvokeAsync(context);
            },
            nameof(ReadPersistedOperationMiddleware));
}
