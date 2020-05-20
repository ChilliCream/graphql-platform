using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline
{
    internal sealed class ParseQueryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDiagnosticEvents _diagnosticEvents;
        private readonly IDocumentCache _documentCache;
        private readonly IDocumentHashProvider _documentHashProvider;

        public ParseQueryMiddleware(
            RequestDelegate next,
            IDiagnosticEvents diagnosticEvents,
            IDocumentCache documentCache,
            IDocumentHashProvider documentHashProvider)
        {
            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _diagnosticEvents = diagnosticEvents ??
                throw new ArgumentNullException(nameof(diagnosticEvents));
            _documentCache = documentCache ??
                throw new ArgumentNullException(nameof(documentCache));
            _documentHashProvider = documentHashProvider ??
                throw new ArgumentNullException(nameof(documentHashProvider));
        }

        public async Task InvokeAsync(IRequestContext context)
        {
            IReadOnlyQueryRequest request = context.Request;

            if (request.Query is { })
            {
                using (_diagnosticEvents.ParseDocument(context))
                {
                    context.DocumentId = ComputeDocumentHash(
                        request.QueryHash, request.Query);
                    context.Document = _documentCache.GetOrParseDocument(
                        context.DocumentId, request.Query, ParseDocument);
                }
            }

            await _next(context).ConfigureAwait(false);
        }

        private static DocumentNode ParseDocument(IQuery query)
        {
            if (query is QueryDocument parsed)
            {
                return parsed.Document;
            }

            if (query is QuerySourceText source)
            {
                return Utf8GraphQLParser.Parse(source.AsSpan());
            }

            throw ThrowHelper.QueryTypeNotSupported();
        }

        private string ComputeDocumentHash(string? queryHash, IQuery query)
        {
            return queryHash ?? _documentHashProvider.ComputeHash(query.AsSpan());
        }
    }
}
