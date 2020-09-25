using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;

namespace HotChocolate.Execution.Pipeline
{
    internal sealed class DocumentParserMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDiagnosticEvents _diagnosticEvents;
        private readonly IDocumentCache _documentCache;
        private readonly IDocumentHashProvider _documentHashProvider;

        public DocumentParserMiddleware(
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

        public async ValueTask InvokeAsync(IRequestContext context)
        {
            if (context.Document is null && context.Request.Query is { })
            {
                bool success = true;

                try
                {
                    using (_diagnosticEvents.ParseDocument(context))
                    {
                        context.DocumentId = ComputeDocumentHash(
                            context.DocumentHash, 
                            context.Request.QueryHash, 
                            context.Request.Query);
                        context.Document = ParseDocument(context.Request.Query);
                    }
                }
                catch (SyntaxException ex)
                {
                    success = false;

                    IError error = context.ErrorHandler.Handle(
                        ErrorBuilder.New()
                            .SetMessage(ex.Message)
                            .SetCode(ErrorCodes.Execution.SyntaxError)
                            .AddLocation(ex.Line, ex.Column)
                            .Build());

                    context.Exception = ex;
                    context.Result = QueryResultBuilder.CreateError(error);

                    _diagnosticEvents.SyntaxError(context, error);
                }

                if (success)
                {
                    await _next(context).ConfigureAwait(false);
                }
            }
            else
            {
                await _next(context).ConfigureAwait(false);
            }
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

        private string ComputeDocumentHash(string? documentHash, string? queryHash, IQuery query)
        {
            return documentHash ?? queryHash ?? _documentHashProvider.ComputeHash(query.AsSpan());
        }
    }
}
