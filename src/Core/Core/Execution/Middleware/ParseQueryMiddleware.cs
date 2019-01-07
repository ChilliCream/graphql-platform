using System;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Runtime;

namespace HotChocolate.Execution
{
    // diagnostics -> Exceptions -> Parse -> Validate -> ResolveOperation
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

        public Task InvokeAsync(IQueryContext context)
        {
            if (!IsContextValid(context))
            {
                context.Result = QueryResult.CreateError(new QueryError(
                   "The parse querymiddleware expectes " +
                   "a valid query request."));
                return Task.CompletedTask;
            }

            context.Document = _queryCache.GetOrCreate(
                context.Request.Query,
                () => ParseDocument(context.Request.Query));

            return _next(context);
        }

        private DocumentNode ParseDocument(string queryText)
        {
            return _parser.Parse(queryText);
        }

        private bool IsContextValid(IQueryContext context)
        {
            return context.Request != null
                && context.Request.Query != null;
        }
    }
}

