using System;
using System.Threading.Tasks;
using HotChocolate.Runtime;

namespace HotChocolate.Execution
{
    internal sealed class ExecuteOperationMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly IExecutionStrategyResolver _strategyResolver;
        private readonly Cache<DirectiveLookup> _directiveCache;

        public ExecuteOperationMiddleware(
            QueryDelegate next,
            IExecutionStrategyResolver strategyResolver,
            Cache<DirectiveLookup> directiveCache)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _strategyResolver = strategyResolver
                ?? throw new ArgumentNullException(nameof(strategyResolver));
            _directiveCache = directiveCache
                ?? throw new ArgumentNullException(nameof(directiveCache));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            if (IsContextIncomplete(context))
            {
                context.Result = QueryResult.CreateError(new QueryError(
                    "The execute operation middleware expects the " +
                    "query document to be parsed, the operation to " +
                    "be resolved and the variables to be coerced."));
            }
            else
            {
                IExecutionStrategy strategy = _strategyResolver
                    .Resolve(context.Operation.Type);

                IExecutionContext executionContext =
                    CreateExecutionContext(context);

                context.Result = await strategy.ExecuteAsync(
                    executionContext, executionContext.RequestAborted)
                    .ConfigureAwait(false);
            }

            await _next(context).ConfigureAwait(false);
        }

        private IExecutionContext CreateExecutionContext(IQueryContext context)
        {
            DirectiveLookup directives = GetOrCreateDirectiveLookup(context);

            return new ExecutionContext(
                context.Schema,
                context.ServiceScope,
                context.Operation,
                context.Variables,
                directives,
                context.ContextData,
                context.RequestAborted);
        }

        private DirectiveLookup GetOrCreateDirectiveLookup(
            IQueryContext context)
        {
            return _directiveCache.GetOrCreate(
                context.Request.Query,
                () =>
                {
                    var directiveCollector = new DirectiveCollector(
                        context.Schema);

                    directiveCollector.VisitDocument(context.Document);

                    return directiveCollector.CreateLookup();
                });
        }

        private static bool IsContextIncomplete(IQueryContext context)
        {
            return context.Document == null
                || context.Operation == null
                || context.Variables == null;
        }
    }
}

