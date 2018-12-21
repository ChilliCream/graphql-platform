using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
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
            if (!IsContextValid(context))
            {
                // TODO : Resources
                throw new InvalidOperationException();
            }

            IExecutionStrategy strategy =
                _strategyResolver.Resolve(context.Operation.Operation);
            IExecutionContext execContext = CreateExecutionContext(context);
            context.Result = await strategy.ExecuteAsync(
                execContext, execContext.RequestAborted);
        }

        private IExecutionContext CreateExecutionContext(IQueryContext context)
        {
            DirectiveLookup directiveLookup = _directiveCache.GetOrCreate(
                context.Request.Query,
                () => CreateLookup(context.Schema, context.Document));

            OperationRequest request = CreateOperationRequest(context);

            VariableCollection variables = CoerceVariables(
                context.Schema, context.Operation,
                request.VariableValues);

            var executionContext = new ExecutionContext(
                context.Schema, directiveLookup, context.Document,
                context.Operation, request, variables,
                context.RequestAborted);

            return executionContext;
        }

        // TODO : remove operation request
        private OperationRequest CreateOperationRequest(
            IQueryContext context)
        {
            IServiceProvider services = context.Services;

            return new OperationRequest(services,
                context.Schema.Sessions.CreateSession(services))
            {
                VariableValues = context.Request.VariableValues,
                Properties = context.Request.Properties,
                InitialValue = context.Request.InitialValue,
            };
        }

        private DirectiveLookup CreateLookup(
            ISchema schema,
            DocumentNode document)
        {
            var directiveCollector = new DirectiveCollector(schema);
            directiveCollector.VisitDocument(document);
            return directiveCollector.CreateLookup();
        }

        private VariableCollection CoerceVariables(
            ISchema schema,
            OperationDefinitionNode operation,
            IReadOnlyDictionary<string, object> variableValues)
        {
            var variableBuilder = new VariableValueBuilder(schema, operation);
            return variableBuilder.CreateValues(variableValues);
        }

        private bool IsContextValid(IQueryContext context)
        {
            return true;
        }
    }
}

