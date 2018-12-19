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
                ?? new DefaultQueryParser();
            _queryCache = queryCache
                ?? new Cache<DocumentNode>(Defaults.CacheSize);
        }

        public Task InvokeAsync(IQueryContext context)
        {
            context.Document = _queryCache.GetOrCreate(
                context.Request.Query,
                () => ParseDocument(context.Request.Query));

            return _next(context);
        }

        private DocumentNode ParseDocument(string queryText)
        {
            return _parser.Parse(queryText);
        }
    }
}

