using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Execution.Utilities.FieldCollector;

namespace HotChocolate.Execution.Pipeline
{
    internal sealed class OperationResolverMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDiagnosticEvents _diagnosticEvents;

        public OperationResolverMiddleware(
            RequestDelegate next,
            IDiagnosticEvents diagnosticEvents)
        {
            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _diagnosticEvents = diagnosticEvents ??
                throw new ArgumentNullException(nameof(diagnosticEvents));
        }

        public async ValueTask InvokeAsync(IRequestContext context)
        {
            if (context.Document is { } && context.ValidationResult is { HasErrors: false })
            {
                OperationDefinitionNode operation =
                    context.Document.GetOperation(context.Request.OperationName);

                ObjectType rootType = ResolveRootType(operation.Operation, context.Schema);

                var fragments = new FragmentCollection(context.Schema, context.Document);

                IReadOnlyDictionary<SelectionSetNode, PreparedSelectionSet> selectionSets =
                    PrepareSelectionSets(context.Schema, fragments, operation);

                context.Operation = new PreparedOperation(
                    context.OperationId ?? Guid.NewGuid().ToString("N"),
                    context.Document,
                    operation,
                    rootType,
                    selectionSets);
                context.OperationId = context.Operation.Id;

                await _next(context).ConfigureAwait(false);
            }
            else
            {
                // TODO : ERRORHELPER
                context.Result = QueryResultBuilder.CreateError((IError)null);
            }
        }

        private static ObjectType ResolveRootType(
            OperationType operationType, ISchema schema) =>
            operationType switch
            {
                OperationType.Query => schema.QueryType,
                OperationType.Mutation => schema.MutationType,
                OperationType.Subscription => schema.SubscriptionType,
                _ => throw new GraphQLException("THROWHELPER")
            };
    }
}
