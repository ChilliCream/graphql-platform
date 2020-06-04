using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Pipeline
{
    internal sealed class OperationVariableCoercionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDiagnosticEvents _diagnosticEvents;
        private readonly VariableCoercionHelper _coercionHelper;

        public OperationVariableCoercionMiddleware(
            RequestDelegate next,
            IDiagnosticEvents diagnosticEvents,
            VariableCoercionHelper coercionHelper)
        {
            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _diagnosticEvents = diagnosticEvents ??
                throw new ArgumentNullException(nameof(diagnosticEvents));
            _coercionHelper = coercionHelper ??
                throw new ArgumentNullException(nameof(coercionHelper));
        }

        public async ValueTask InvokeAsync(IRequestContext context)
        {
            if (context.Operation is { })
            {
                if (context.Request.VariableValues is null ||
                    context.Request.VariableValues is { Count: 0 })
                {
                    context.Variables = VariableValueCollection.Empty;
                }
                else
                {
                    var coercedValues = new Dictionary<string, VariableValue>();

                    _coercionHelper.CoerceVariableValues(
                        context.Schema,
                        context.Operation.Definition.VariableDefinitions,
                        context.Request.VariableValues,
                        coercedValues);

                    context.Variables = new VariableValueCollection(coercedValues);
                }

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
