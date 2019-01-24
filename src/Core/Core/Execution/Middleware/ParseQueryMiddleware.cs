using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using HotChocolate.Runtime;

namespace HotChocolate.Execution
{
    internal sealed class ParseQueryMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly IQueryParser _parser;
        private readonly Cache<DocumentNode> _queryCache;

        public ParseQueryMiddleware(
            QueryDelegate next,
            IQueryParser parser,
            Cache<DocumentNode> queryCache)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _parser = parser
                ?? throw new ArgumentNullException(nameof(parser));
            _queryCache = queryCache
                ?? throw new ArgumentNullException(nameof(queryCache));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            Activity activity = DiagnosticEvents.BeginParsing(context);

            if (IsContextIncomplete(context))
            {
                context.Result = QueryResult.CreateError(new QueryError(
                   "The parse query middleware expects " +
                   "a valid query request."));
            }
            else
            {
                context.Document = _queryCache.GetOrCreate(
                    context.Request.Query,
                    () => ParseDocument(context.Request.Query));

                await _next(context).ConfigureAwait(false);
            }

            DiagnosticEvents.EndParsing(activity, context);
        }

        private DocumentNode ParseDocument(string queryText)
        {
            return _parser.Parse(queryText);
        }

        private static bool IsContextIncomplete(IQueryContext context)
        {
            return context.Request == null ||
                context.Request.Query == null;
        }
    }
}

