using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Pipeline;

internal sealed class ReadPersistedQueryMiddleware(
    RequestDelegate next,
    [SchemaService] IExecutionDiagnosticEvents diagnosticEvents,
    [SchemaService] IReadStoredQueries persistedQueryStore)
{
    private readonly RequestDelegate _next = next ??
        throw new ArgumentNullException(nameof(next));
    private readonly IExecutionDiagnosticEvents _diagnosticEvents = diagnosticEvents ??
        throw new ArgumentNullException(nameof(diagnosticEvents));
    private readonly IReadStoredQueries _persistedQueryStore = persistedQueryStore ??
        throw new ArgumentNullException(nameof(persistedQueryStore));

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        if (context.Document is null &&
            context.Request.Query is null)
        {
            await TryLoadQueryAsync(context).ConfigureAwait(false);
        }

        await _next(context).ConfigureAwait(false);
    }

    private async ValueTask TryLoadQueryAsync(IRequestContext context)
    {
        var queryId =
            context.Request.QueryId ??
            context.DocumentId ??
            context.DocumentHash ??
            context.Request.QueryHash;

        if (queryId is not null)
        {
            var queryDocument =
                await _persistedQueryStore.TryReadQueryAsync(
                    queryId, context.RequestAborted)
                    .ConfigureAwait(false);

            if (queryDocument is not null)
            {
                context.DocumentId = queryId;
                context.Document = queryDocument.Document;
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
            var persistedQueryStore = core.SchemaServices.GetRequiredService<IReadStoredQueries>();
            var middleware = new ReadPersistedQueryMiddleware(next, diagnosticEvents, persistedQueryStore);
            return context => middleware.InvokeAsync(context);
        };
}
