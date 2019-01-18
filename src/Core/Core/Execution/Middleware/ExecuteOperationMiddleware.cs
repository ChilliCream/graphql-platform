using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Runtime;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class ExecuteOperationMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly IExecutionStrategyResolver _strategyResolver;
        private readonly Cache<DirectiveMiddlewareCompiler> _cache;

        public ExecuteOperationMiddleware(
            QueryDelegate next,
            IExecutionStrategyResolver strategyResolver,
            Cache<DirectiveMiddlewareCompiler> directiveCache)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _strategyResolver = strategyResolver
                ?? throw new ArgumentNullException(nameof(strategyResolver));
            _cache = directiveCache
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
            DirectiveMiddlewareCompiler directives =
                GetOrCreateDirectiveLookup(context);

            return new ExecutionContext(
                context.Schema,
                context.ServiceScope,
                context.Operation,
                context.Variables,
                f,
                context.ContextData,
                context.RequestAborted);
        }

        private DirectiveMiddlewareCompiler GetOrCreateDirectiveLookup(
            IQueryContext context)
        {
            return _cache.GetOrCreate(context.Request.Query,
                () =>
                {
                    var directiveCollector = new CollectDirectivesVisitor();
                    ILookup<FieldSelection, IDirective> directives =
                        directiveCollector.CollectDirectives(context.Schema);

                    var middlewareCompiler = new DirectiveMiddlewareCompiler(
                        directives);

                    return middlewareCompiler;
                });
        }

        private FieldDelegate ResolveMiddleware(
            Func<FieldSelection, FieldDelegate> middlewareResolver,
            DirectiveMiddlewareCompiler directives)
        {

        }

        private static bool IsContextIncomplete(IQueryContext context)
        {
            return context.Document == null
                || context.Operation == null
                || context.Variables == null;
        }
    }

    internal sealed class EnableDirectivesMiddleware
    {
        private readonly QueryDelegate _next;
        private readonly IExecutionStrategyResolver _strategyResolver;
        private readonly Cache<DirectiveMiddlewareCompiler> _cache;

        public EnableDirectivesMiddleware(
            QueryDelegate next,
            Cache<DirectiveMiddlewareCompiler> directiveCache)
        {
        }
    }
}

