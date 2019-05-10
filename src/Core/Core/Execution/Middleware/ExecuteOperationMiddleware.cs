using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Runtime;

namespace HotChocolate.Execution
{
    internal sealed class ExecuteOperationMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly IExecutionStrategyResolver _strategyResolver;
        private readonly Cache<DirectiveMiddlewareCompiler> _cache;
        private readonly QueryExecutionDiagnostics _diagnosticEvents;

        public ExecuteOperationMiddleware(
            QueryDelegate next,
            IExecutionStrategyResolver strategyResolver,
            Cache<DirectiveMiddlewareCompiler> directiveCache,
            QueryExecutionDiagnostics diagnosticEvents)
        {
            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _strategyResolver = strategyResolver ??
                throw new ArgumentNullException(nameof(strategyResolver));
            _cache = directiveCache ??
                throw new ArgumentNullException(nameof(directiveCache));
            _diagnosticEvents = diagnosticEvents ??
                throw new ArgumentNullException(nameof(diagnosticEvents));
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            if (IsContextIncomplete(context))
            {
                // TODO : resources
                context.Result = QueryResult.CreateError(new QueryError(
                    "The execute operation middleware expects the " +
                    "query document to be parsed and the operation to " +
                    "be resolved."));
            }
            else
            {
                Activity activity = _diagnosticEvents.BeginOperation(context);

                try
                {
                    IExecutionStrategy strategy = _strategyResolver
                        .Resolve(context.Operation.Type);
                    IExecutionContext executionContext =
                        CreateExecutionContext(context);

                    context.Result = await strategy.ExecuteAsync(
                        executionContext, executionContext.RequestAborted)
                        .ConfigureAwait(false);
                }
                finally
                {
                    _diagnosticEvents.EndOperation(activity, context);
                }
            }

            await _next(context).ConfigureAwait(false);
        }

        private IExecutionContext CreateExecutionContext(
            IQueryContext context)
        {
            DirectiveMiddlewareCompiler directives =
                GetOrCreateDirectiveLookup(
                    context.Request.Query,
                    context.Schema);

            var requestContext = new RequestContext
            (
                context.ServiceScope,
                (field, selection) =>
                    directives.GetOrCreateMiddleware(field, selection,
                    () => context.MiddlewareResolver.Invoke(field, selection)),
                context.CachedQuery,
                context.ContextData,
                _diagnosticEvents
            );

            return new ExecutionContext
            (
                context.Schema,
                context.Operation,
                requestContext,
                context.RequestAborted
            );
        }

        private DirectiveMiddlewareCompiler GetOrCreateDirectiveLookup(
            string query, ISchema schema)
        {
            return _cache.GetOrCreate(query,
                () => new DirectiveMiddlewareCompiler(schema));
        }

        private static bool IsContextIncomplete(IQueryContext context)
        {
            return context.Document == null ||
                context.Operation == null;
        }
    }
}
