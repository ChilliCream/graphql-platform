using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Execution.Utilities.OperationCompiler;

namespace HotChocolate.Execution.Pipeline
{
    internal sealed class OperationResolverMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDiagnosticEvents _diagnosticEvents;
        private readonly IReadOnlyList<ISelectionOptimizer>? _optimizers;

        public OperationResolverMiddleware(
            RequestDelegate next,
            IDiagnosticEvents diagnosticEvents,
            IEnumerable<ISelectionOptimizer> optimizers)
        {
            if (optimizers is null)
            {
                throw new ArgumentNullException(nameof(optimizers));
            }

            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _diagnosticEvents = diagnosticEvents ??
                throw new ArgumentNullException(nameof(diagnosticEvents));
            _optimizers = optimizers.ToArray();
        }

        public async ValueTask InvokeAsync(IRequestContext context)
        {
            if (context.Operation is { })
            {
                await _next(context).ConfigureAwait(false);
            }
            else if (context.Document is { } && context.ValidationResult is { HasErrors: false })
            {
                OperationDefinitionNode operation =
                    context.Document.GetOperation(context.Request.OperationName);

                ObjectType? rootType = ResolveRootType(operation.Operation, context.Schema);

                if (rootType is null)
                {
                    context.Result = ErrorHelper.RootTypeNotFound(operation.Operation);
                    return;
                }

                var fragments = new FragmentCollection(context.Schema, context.Document);

                IReadOnlyDictionary<SelectionSetNode, SelectionVariants> selectionSets =
                    Compile(context.Schema, fragments, operation, _optimizers);

                context.Operation = new Operation(
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
                context.Result = ErrorHelper.StateInvalidForOperationResolver();
            }
        }

        private static ObjectType? ResolveRootType(
            OperationType operationType, ISchema schema) =>
            operationType switch
            {
                OperationType.Query => schema.QueryType,
                OperationType.Mutation => schema.MutationType,
                OperationType.Subscription => schema.SubscriptionType,
                _ => throw ThrowHelper.RootTypeNotSupported(operationType)
            };
    }
}
