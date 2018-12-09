using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Runtime;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    public interface IQueryExecutionBuilder
    {
        IServiceCollection Services { get; }

        IQueryExecutionBuilder Use(QueryMiddleware middleware);

        IQueryExecuter BuildQueryExecuter();
    }

    public class QueryExecutionBuilder
        : IQueryExecutionBuilder
    {
        public IServiceCollection Services { get; }

        public IQueryExecuter BuildQueryExecuter()
        {
            throw new NotImplementedException();
        }

        public IQueryExecutionBuilder Use(QueryMiddleware middleware)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class ParseQueryMiddleware
    {
        public readonly QueryDelegate _next;
        public readonly IQueryParser _parser;
        public readonly Cache<DocumentNode> _queryCache;

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

    public interface IQueryContext
    {
        IServiceProvider Services { get; }
        IReadOnlyQueryRequest Request { get; }
        DocumentNode Document { get; set; }
        OperationDefinitionNode Operation { get; set; }
        QueryValidationResult ValidationResult { get; set; }
        IVariableCollection VariableCollection { get; set; }
        CancellationToken RequestAborted { get; set; }
        IDictionary<string, object> Custom { get; set; }
        IExecutionResult Result { get; set; }
    }

    public delegate QueryDelegate QueryMiddleware(QueryDelegate next);

    public delegate Task QueryDelegate(IQueryContext context);
}

