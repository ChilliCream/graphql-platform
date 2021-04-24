using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using static HotChocolate.Execution.Pipeline.PipelineTools;

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
            if (context.Variables is not null)
            {
                await _next(context).ConfigureAwait(false);
            }
            else if (context.Operation is not null)
            {
                CoerceVariables(
                    context,
                    _coercionHelper,
                    context.Operation.Definition.VariableDefinitions);

                await _next(context).ConfigureAwait(false);
            }
            else
            {
                context.Result = ErrorHelper.StateInvalidForOperationVariableCoercion();
            }
        }
    }
}
