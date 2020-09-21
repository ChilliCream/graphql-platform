using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Validation;

namespace HotChocolate.Execution.Pipeline
{
    internal sealed class ReadPersistedQueryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDiagnosticEvents _diagnosticEvents;
        private readonly IReadStoredQueries _persistedQueryStore;

        public ReadPersistedQueryMiddleware(
            RequestDelegate next,
            IDiagnosticEvents diagnosticEvents,
            IReadStoredQueries persistedQueryStore)
        {
            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _diagnosticEvents = diagnosticEvents ??
                throw new ArgumentNullException(nameof(diagnosticEvents));
            _persistedQueryStore = persistedQueryStore ??
                throw new ArgumentNullException(nameof(persistedQueryStore));
        }

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
                QueryDocument? queryDocument =
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
    }
}
