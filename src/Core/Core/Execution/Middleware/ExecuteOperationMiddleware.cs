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
        private static readonly Dictionary<OperationType, IExecutionStrategy> _executionStrategy =
            new Dictionary<OperationType, IExecutionStrategy>
            {
                { OperationType.Query, new QueryExecutionStrategy() },
                { OperationType.Mutation, new MutationExecutionStrategy() },
                { OperationType.Subscription, new SubscriptionExecutionStrategy() }
            };

        private readonly QueryDelegate _next;
        private readonly Cache<DirectiveLookup> _directiveCache;

        public ExecuteOperationMiddleware(
            QueryDelegate next,
            Cache<DirectiveLookup> directiveCache)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next));
            _directiveCache = directiveCache
                ?? new Cache<DirectiveLookup>(Defaults.CacheSize);
        }

        public async Task InvokeAsync(IQueryContext context)
        {
            if (!IsContextValid(context))
            {
                // TODO : Resources
                throw new InvalidOperationException();
            }


            if (!_executionStrategy.TryGetValue(context.Operation.Operation,
                out IExecutionStrategy strategy))
            {
                // TODO : Resources
                throw new NotSupportedException("Operation not supported!");
            }

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
            IServiceProvider services = context.Request.Services
                ?? context.Schema.Services;

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

