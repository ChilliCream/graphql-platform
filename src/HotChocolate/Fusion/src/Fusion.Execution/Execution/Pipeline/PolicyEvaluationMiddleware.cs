using System.Runtime.InteropServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class PolicyEvaluationMiddleware
{
    private const string MiddlewareName = "PolicyEvaluationMiddleware";
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;
    private readonly IErrorHandler _errorHandler;

    private PolicyEvaluationMiddleware(
        IFusionExecutionDiagnosticEvents diagnosticEvents,
        IErrorHandler errorHandler)
    {
        _diagnosticEvents = diagnosticEvents;
        _errorHandler = errorHandler;
    }

    public async ValueTask InvokeAsync(
        RequestContext context,
        RequestDelegate next,
        CancellationToken cancellationToken)
    {
        if (context.IsWarmupRequest())
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        PolicyRequestState.HydrateUserState(context);

        var operationPlan = context.GetOperationPlan();
        if (operationPlan is null)
        {
            throw ThrowHelper.PolicyOperationPlanMissing();
        }

        if (operationPlan.PolicySlots.IsDefaultOrEmpty)
        {
            if (!operationPlan.RequestPolicyNames.IsDefaultOrEmpty)
            {
                PolicyRequestState.GetOrCreate(
                    context,
                    operationPlan,
                    _diagnosticEvents);
            }

            await next(context).ConfigureAwait(false);
            return;
        }

        var variables = ImmutableCollectionsMarshal.AsArray(context.VariableValues)!;
        var evaluatedVariables = new IVariableValueCollection[variables.Length];
        var shortCircuitResults = new OperationResult?[variables.Length];
        PolicyRequestState requestState;

        try
        {
            using (_diagnosticEvents.EvaluateRequestPolicies(context))
            {
                requestState = PolicyRequestState.GetOrCreate(
                    context,
                    operationPlan,
                    _diagnosticEvents);
                requestState.BeginReduction();

                for (var i = 0; i < variables.Length; i++)
                {
                    var evaluation = await requestState.EvaluateSlotsAsync(
                        operationPlan,
                        variables[i],
                        cancellationToken)
                        .ConfigureAwait(false);
                    evaluatedVariables[i] = new PolicyVariableValueCollection(
                        variables[i],
                        operationPlan.PolicySlots.Length,
                        evaluation.LiveFlags,
                        evaluation.DenyFlags,
                        evaluation.FetchGateDenyFlags);

                    if (evaluation.ShouldShortCircuit)
                    {
                        shortCircuitResults[i] = ErrorHelper.PolicyRequestDenied(
                            variables.Length > 1 ? i : null,
                            evaluation.ShortCircuitDenial,
                            _errorHandler);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            _diagnosticEvents.RequestError(context, error);
            context.Result = ErrorHelper.PolicyRequestEvaluationFailed();
            return;
        }

        context.VariableValues = ImmutableCollectionsMarshal.AsImmutableArray(evaluatedVariables);
        requestState.SetShortCircuitResults(shortCircuitResults);

        if (variables.Length == 1 && shortCircuitResults[0] is { } shortCircuitResult)
        {
            context.Result = shortCircuitResult;
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    public static RequestMiddlewareConfiguration Create()
        => new(
            (factoryContext, next) =>
            {
                var diagnosticEvents = factoryContext.SchemaServices
                    .GetRequiredService<IFusionExecutionDiagnosticEvents>();
                var errorHandler = factoryContext.SchemaServices.GetRequiredService<IErrorHandler>();
                var middleware = new PolicyEvaluationMiddleware(diagnosticEvents, errorHandler);
                return requestContext => middleware.InvokeAsync(
                    requestContext,
                    next,
                    requestContext.RequestAborted);
            },
            MiddlewareName);
}
