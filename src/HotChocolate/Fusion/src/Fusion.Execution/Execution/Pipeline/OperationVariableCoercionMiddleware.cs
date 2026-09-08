using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationVariableCoercionMiddleware
{
    private static readonly ImmutableArray<IVariableValueCollection> s_noVariables = [VariableValueCollection.Empty];
    private readonly ICoreExecutionDiagnosticEvents _diagnosticEvents;

    private OperationVariableCoercionMiddleware(
        ICoreExecutionDiagnosticEvents diagnosticEvents)
    {
        _diagnosticEvents = diagnosticEvents;
    }

    public ValueTask InvokeAsync(
        RequestContext context,
        RequestDelegate next)
    {
        if (!context.TryGetNormalizedOperation(out var operation))
        {
            context.Result = ErrorHelper.StateInvalidForVariableCoercion();
            return default;
        }

        // Warmup requests carry no real variable values, and a GraphQL-Cost validate request
        // that supplies no variables is analyzed with the static-bound path. In both cases
        // coercion is skipped and the request context's variable values stay empty.
        if (context.IsWarmupRequest() || IsCostValidationWithoutVariables(context))
        {
            return next(context);
        }

        return TryCoerceVariables(
            context,
            operation.VariableDefinitions,
            _diagnosticEvents)
            ? next(context)
            : default;
    }

    private static bool IsCostValidationWithoutVariables(RequestContext context)
        => context.Request is OperationRequest operationRequest
            && context.ContextData.ContainsKey(ExecutionContextData.ValidateCost)
            && HasNoVariableValues(operationRequest.VariableValues);

    private static bool HasNoVariableValues(JsonDocumentOwner? variableValues)
    {
        if (variableValues is null)
        {
            return true;
        }

        var root = variableValues.Document.RootElement;

        if (root.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return true;
        }

        if (root.ValueKind is not JsonValueKind.Object)
        {
            return false;
        }

#if NET10_0_OR_GREATER
        return root.GetPropertyCount() == 0;
#else
        return !root.EnumerateObject().MoveNext();
#endif
    }

    private static bool TryCoerceVariables(
        RequestContext context,
        IReadOnlyList<VariableDefinitionNode> variableDefinitions,
        ICoreExecutionDiagnosticEvents diagnosticEvents)
    {
        if (context.VariableValues.Length > 0)
        {
            return true;
        }

        if (variableDefinitions.Count == 0)
        {
            context.VariableValues = s_noVariables;
            return true;
        }

        if (context.Request is OperationRequest operationRequest)
        {
            using (diagnosticEvents.CoerceVariables(context))
            {
                if (VariableCoercionHelper.TryCoerceVariableValues(
                    context,
                    context.Schema,
                    variableDefinitions,
                    operationRequest.VariableValues?.Document.RootElement ?? default,
                    out var coercedValues,
                    out var error))
                {
                    context.VariableValues = [new VariableValueCollection(coercedValues)];
                    return true;
                }

                context.Result = OperationResult.FromError(error);
                return false;
            }
        }

        if (context.Request is VariableBatchRequest variableBatchRequest)
        {
            using (diagnosticEvents.CoerceVariables(context))
            {
                var variableValuesSetInput = variableBatchRequest.VariableValues.Document.RootElement;
                var variableValuesSet = new IVariableValueCollection[variableValuesSetInput.GetArrayLength()];
                var i = 0;

                foreach (var variableValuesInput in variableValuesSetInput.EnumerateArray())
                {
                    if (VariableCoercionHelper.TryCoerceVariableValues(
                        context,
                        context.Schema,
                        variableDefinitions,
                        variableValuesInput,
                        out var coercedValues,
                        out var error))
                    {
                        variableValuesSet[i++] = new VariableValueCollection(coercedValues);
                    }
                    else
                    {
                        context.Result = OperationResult.FromError(error);
                        return false;
                    }
                }

                context.VariableValues = ImmutableCollectionsMarshal.AsImmutableArray(variableValuesSet);
                return true;
            }
        }

        throw new NotSupportedException("Request type not supported.");
    }

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (fc, next) =>
            {
                var diagnosticEvents = fc.SchemaServices.GetRequiredService<ICoreExecutionDiagnosticEvents>();
                var middleware = new OperationVariableCoercionMiddleware(diagnosticEvents);
                return requestContext => middleware.InvokeAsync(requestContext, next);
            },
            WellKnownRequestMiddleware.OperationVariableCoercionMiddleware);
}
