using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Policies;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationPolicyMiddleware
{
    private const string Key = "FusionOperationPolicyMiddleware";

    private readonly PolicyEvaluator _evaluator;
    private readonly FusionAuthorizationOptions _options;
    private readonly AuthenticationSchemeResolver _schemeResolver;
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;
    private readonly IInputType _booleanType;

    private OperationPolicyMiddleware(
        PolicyEvaluator evaluator,
        FusionAuthorizationOptions options,
        AuthenticationSchemeResolver schemeResolver,
        IFusionExecutionDiagnosticEvents diagnosticEvents,
        IInputType booleanType)
    {
        _evaluator = evaluator;
        _options = options;
        _schemeResolver = schemeResolver;
        _diagnosticEvents = diagnosticEvents;
        _booleanType = booleanType;
    }

    public ValueTask InvokeAsync(RequestContext context, RequestDelegate next)
    {
        var plan = context.GetOperationPlan() ?? throw ThrowHelper.OperationPolicyRequiresPlan();

        return plan.Policies is { } policies
            ? InvokeProtectedAsync(context, plan, policies, next)
            : next(context);
    }

    private async ValueTask InvokeProtectedAsync(
        RequestContext context,
        OperationPlan plan,
        PolicyPlan policies,
        RequestDelegate next)
    {
        var variableSets = context.VariableValues;

        if (variableSets.IsDefaultOrEmpty)
        {
            await next(context);
            return;
        }

        var user = GetUser(context);
        PolicyDecisions?[]? decisionSets = null;
        IVariableValueCollection[]? updatedVariableSets = null;

        for (var i = 0; i < variableSets.Length; i++)
        {
            var evaluation = await _evaluator.EvaluateAsync(
                context,
                user,
                plan,
                policies,
                variableSets[i],
                i,
                context.RequestAborted);

            if (evaluation.Failure is { } failure)
            {
                _diagnosticEvents.RequestError(context, failure);
                context.Result = ErrorHelper.AuthorizationFailed();
                return;
            }

            var decisions = evaluation.Decisions;

            if (decisions is not null
                && decisions.TryGetRejection(_options.RejectRequestOn, out var denial))
            {
                await RejectAsync(context, denial);
                return;
            }

            if (decisions is not null)
            {
                decisionSets ??= new PolicyDecisions?[variableSets.Length];
                decisionSets[i] = decisions;
            }

            if (policies.Variables.IsEmpty)
            {
                continue;
            }

            if (variableSets[i] is VariableValueCollection values)
            {
                PolicyVariableValues.AddTo(values, policies.Variables, decisions, _booleanType);
            }
            else
            {
                updatedVariableSets ??= [.. variableSets];
                updatedVariableSets[i] = CreateVariableValues(variableSets[i], policies.Variables, decisions);
            }
        }

        if (updatedVariableSets is not null)
        {
            context.VariableValues = ImmutableCollectionsMarshal.AsImmutableArray(updatedVariableSets);
        }

        if (decisionSets is not null)
        {
            context.SetPolicyDecisions(ImmutableCollectionsMarshal.AsImmutableArray(decisionSets));
        }

        await next(context);
    }

    private async ValueTask RejectAsync(RequestContext context, SelectionDenial denial)
    {
        var challenge = denial.Kind is AuthorizationDenialKind.Unauthenticated
            ? await _schemeResolver.GetChallengeAsync(context.RequestAborted)
            : null;

        var result = ErrorHelper.AuthorizationRejected(denial, _options.EnableAttribution, challenge);

        _diagnosticEvents.RequestError(context, result.Errors[0]);
        context.Result = result;
    }

    private VariableValueCollection CreateVariableValues(
        IVariableValueCollection variableValues,
        ImmutableArray<PolicyVariable> variables,
        PolicyDecisions? decisions)
    {
        var count = 0;

        foreach (var _ in variableValues)
        {
            count++;
        }

        var values = new Dictionary<string, VariableValue>(count + variables.Length);

        foreach (var value in variableValues)
        {
            values[value.Name] = value;
        }

        var collection = new VariableValueCollection(values);
        PolicyVariableValues.AddTo(collection, variables, decisions, _booleanType);
        return collection;
    }

    private static ClaimsPrincipal GetUser(RequestContext context)
        => context.ContextData.TryGetValue(nameof(ClaimsPrincipal), out var value)
            && value is ClaimsPrincipal user
                ? user
                : new ClaimsPrincipal(new ClaimsIdentity());

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (fc, next) =>
            {
                var schema = fc.SchemaServices.GetRequiredService<FusionSchemaDefinition>();
                var options = fc.SchemaServices.GetRequiredService<FusionAuthorizationOptions>();
                var schemeResolver = fc.SchemaServices.GetRequiredService<AuthenticationSchemeResolver>();
                var diagnosticEvents = fc.SchemaServices.GetRequiredService<IFusionExecutionDiagnosticEvents>();
                var booleanType = GetBooleanType(schema);
                var middleware = new OperationPolicyMiddleware(
                    new PolicyEvaluator(options),
                    options,
                    schemeResolver,
                    diagnosticEvents,
                    booleanType);
                return requestContext => middleware.InvokeAsync(requestContext, next);
            },
            Key);

    private static IInputType GetBooleanType(FusionSchemaDefinition schema)
    {
        var typeNode = new NonNullTypeNode(new NamedTypeNode(SpecScalarNames.Boolean.Name));

        if (schema.Types.TryGetType(typeNode, out IInputType? type))
        {
            return type;
        }

        throw ThrowHelper.OperationPolicyRequiresBooleanType();
    }
}
