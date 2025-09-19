using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.PersistedOperations;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class ReadPersistedOperationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICoreExecutionDiagnosticEvents _diagnosticEvents;
    private readonly IOperationDocumentStorage _operationDocumentStorage;
    private readonly IDocumentHashProvider _documentHashAlgorithm;
    private readonly PersistedOperationOptions _options;

    private ReadPersistedOperationMiddleware(
        RequestDelegate next,
        ICoreExecutionDiagnosticEvents diagnosticEvents,
        IOperationDocumentStorage operationDocumentStorage,
        IDocumentHashProvider documentHashAlgorithm,
        PersistedOperationOptions options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);
        ArgumentNullException.ThrowIfNull(operationDocumentStorage);
        ArgumentNullException.ThrowIfNull(documentHashAlgorithm);
        ArgumentNullException.ThrowIfNull(options);

        _next = next;
        _diagnosticEvents = diagnosticEvents;
        _operationDocumentStorage = operationDocumentStorage;
        _documentHashAlgorithm = documentHashAlgorithm;
        _options = options;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        var documentInfo = context.OperationDocumentInfo;
        if (documentInfo.Document is null)
        {
            await TryLoadQueryAsync(context, documentInfo, context.RequestAborted).ConfigureAwait(false);
        }

        await _next(context).ConfigureAwait(false);
    }

    private async ValueTask TryLoadQueryAsync(
        RequestContext context,
        OperationDocumentInfo documentInfo,
        CancellationToken ct)
    {
        if (documentInfo.Id.IsEmpty)
        {
            return;
        }

        var operationDocument = await _operationDocumentStorage.TryReadAsync(documentInfo.Id, ct).ConfigureAwait(false);

        if (operationDocument is not null)
        {
            documentInfo.Document = GetOrParseDocument(operationDocument);
            documentInfo.Hash = GetDocumentHash(operationDocument);
            documentInfo.IsValidated = false;
            documentInfo.IsPersisted = true;
            documentInfo.IsCached = false;

            if (_options.SkipPersistedDocumentValidation)
            {
                documentInfo.IsValidated = true;
            }

            _diagnosticEvents.RetrievedDocumentFromStorage(context);
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

    private OperationDocumentHash GetDocumentHash(IOperationDocument document)
    {
        if (document is IOperationDocumentHashProvider hashProvider
            && _documentHashAlgorithm.Name.Equals(hashProvider.Hash.AlgorithmName)
            && _documentHashAlgorithm.Format.Equals(hashProvider.Hash.Format))
        {
            return hashProvider.Hash;
        }

        return OperationDocumentHash.Empty;
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var diagnosticEvents = core.SchemaServices.GetRequiredService<ICoreExecutionDiagnosticEvents>();
                var store = core.SchemaServices.GetRequiredService<IOperationDocumentStorage>();
                var options = core.SchemaServices.GetRequiredService<PersistedOperationOptions>();
                var hashAlgorithm = core.Services.GetRequiredService<IDocumentHashProvider>();
                var middleware = new ReadPersistedOperationMiddleware(
                    next,
                    diagnosticEvents,
                    store,
                    hashAlgorithm,
                    options);
                return context => middleware.InvokeAsync(context);
            },
            nameof(ReadPersistedOperationMiddleware));
}
