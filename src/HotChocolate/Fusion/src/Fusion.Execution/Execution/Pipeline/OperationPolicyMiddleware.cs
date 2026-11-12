using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Policies;
using HotChocolate.Fusion.Policies.Auditing;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Pipeline;

internal sealed class OperationPolicyMiddleware
{
    private const string Key = "FusionOperationPolicyMiddleware";
    private const string ExpiryClaimType = "exp";

    private readonly PolicyEvaluator _evaluator;
    private readonly IAuditProvider _auditProvider;
    private readonly FusionPolicyOptions _options;
    private readonly AuthenticationSchemeResolver _schemeResolver;
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;
    private readonly IInputType _booleanType;
    private readonly TimeProvider _timeProvider;
    private readonly IErrorHandler _errorHandler;

    private OperationPolicyMiddleware(
        PolicyEvaluator evaluator,
        IAuditProvider auditProvider,
        FusionPolicyOptions options,
        AuthenticationSchemeResolver schemeResolver,
        IFusionExecutionDiagnosticEvents diagnosticEvents,
        IInputType booleanType,
        TimeProvider timeProvider,
        IErrorHandler errorHandler)
    {
        _evaluator = evaluator;
        _auditProvider = auditProvider;
        _options = options;
        _schemeResolver = schemeResolver;
        _diagnosticEvents = diagnosticEvents;
        _booleanType = booleanType;
        _timeProvider = timeProvider;
        _errorHandler = errorHandler;
    }

    public ValueTask InvokeAsync(RequestContext context, RequestDelegate next)
    {
        var plan = context.GetOperationPlan() ?? throw ThrowHelper.OperationPolicyRequiresPlan();

        return plan.Policies.IsEmpty
            ? next(context)
            : InvokeProtectedAsync(context, plan, next);
    }

    private async ValueTask InvokeProtectedAsync(
        RequestContext context,
        OperationPlan plan,
        RequestDelegate next)
    {
        var policies = plan.Policies;
        var variableSets = context.VariableValues;

        if (variableSets.IsDefaultOrEmpty)
        {
            await next(context);
            return;
        }

        var user = GetUser(context);
        var trail = context.Features.TryGet(out AuditInvocation? auditInvocation)
            ? auditInvocation.GetOrCreateTrail(_auditProvider, context.RequestServices)
            : _auditProvider.CreateTrail(context.RequestServices);
        PolicyEvaluationResult[] policyResults = [];
        IVariableValueCollection[]? updatedVariableSets = null;
        var isSubscription = plan.Operation.Definition.Operation is OperationType.Subscription;
        var expiry = isSubscription ? GetExpiry(user) : null;
        IAuditScope? subscriptionScope = null;
        IVariableValueCollection? subscriptionVariables = null;

        if (expiry <= _timeProvider.GetUtcNow())
        {
            await RefuseExpiredAsync(context, trail, user, plan, variableSets);
            return;
        }

        for (var i = 0; i < variableSets.Length; i++)
        {
            var scope = trail.BeginRequest(
                new AuditScopeInfo(plan.Operation.Id, plan.Id, context.RequestIndex, i),
                user);

            PolicyEvaluationResult policyResult;

            try
            {
                policyResult = await _evaluator.EvaluateAsync(
                    context,
                    user,
                    plan,
                    variableSets[i],
                    scope,
                    i,
                    context.RequestAborted);
            }
            catch (Exception ex) when (PolicyEvaluator.IsFault(ex, context.RequestAborted))
            {
                await _evaluator.CommitFaultedScopeAsync(context, scope, ex);

                if (ex is PolicyNotResolvedException)
                {
                    _diagnosticEvents.RequestError(context, ex);
                    context.Result = ErrorHelper.AuthorizationFailed();
                    return;
                }

                if (ex is OperationCanceledException)
                {
                    throw ThrowHelper.OperationPolicyFaulted(ex);
                }

                throw;
            }

            await scope.CommitAsync(context.RequestAborted);

            if (policyResult.TryGetRejection(_options.RejectRequestOn, out var denial))
            {
                await RejectAsync(context, denial);
                return;
            }

            if (!policyResult.Denials.IsEmpty)
            {
                if (policyResults.Length == 0)
                {
                    policyResults = new PolicyEvaluationResult[variableSets.Length];
                    Array.Fill(policyResults, PolicyEvaluationResult.Empty);
                }

                policyResults[i] = policyResult;
            }

            if (isSubscription
                && policyResult.TryGetDenial(plan.Operation.RootSelectionSet.Selections[0], out var rootDenial))
            {
                await RejectAsync(context, rootDenial);
                return;
            }

            if (expiry is not null)
            {
                subscriptionScope = scope;
                subscriptionVariables = variableSets[i];
            }

            if (policies.Variables.IsEmpty)
            {
                continue;
            }

            if (variableSets[i] is not VariableValueCollection values)
            {
                values = VariableValueCollection.Copy(variableSets[i], policies.Variables.Length);
                updatedVariableSets ??= [.. variableSets];
                updatedVariableSets[i] = values;
            }

            foreach (var variable in policies.Variables)
            {
                values.Add(
                    variable.Name,
                    new VariableValue(
                        variable.Name,
                        _booleanType,
                        variable.IsTrue(policyResult) ? BooleanValueNode.True : BooleanValueNode.False));
            }
        }

        if (updatedVariableSets is not null)
        {
            context.VariableValues = ImmutableCollectionsMarshal.AsImmutableArray(updatedVariableSets);
        }

        if (policyResults.Length > 0)
        {
            context.SetPolicyEvaluationResults(ImmutableCollectionsMarshal.AsImmutableArray(policyResults));
        }

        await next(context);

        if (expiry is { } end
            && subscriptionScope is not null
            && context.Result is ResponseStream stream)
        {
            context.Result = EndOnExpiry(
                context,
                user,
                plan,
                subscriptionScope,
                subscriptionVariables!,
                end,
                stream);
        }
    }

    private async ValueTask RefuseExpiredAsync(
        RequestContext context,
        IAuditTrail trail,
        ClaimsPrincipal user,
        OperationPlan plan,
        ImmutableArray<IVariableValueCollection> variableSets)
    {
        for (var i = 0; i < variableSets.Length; i++)
        {
            var scope = trail.BeginRequest(
                new AuditScopeInfo(plan.Operation.Id, plan.Id, context.RequestIndex, i),
                user);

            _evaluator.RecordDenied(
                scope,
                plan,
                variableSets[i],
                PolicyEvaluator.UnauthenticatedReason);
            await scope.CommitAsync(context.RequestAborted);
        }

        var challenge = await _schemeResolver.GetChallengeAsync(context.RequestAborted);
        var result = ErrorHelper.TokenExpired(challenge);

        _diagnosticEvents.RequestError(context, result.Errors[0]);
        context.Result = result;
    }

    private ResponseStream EndOnExpiry(
        RequestContext context,
        ClaimsPrincipal user,
        OperationPlan plan,
        IAuditScope scope,
        IVariableValueCollection variables,
        DateTimeOffset expiry,
        ResponseStream stream)
    {
        ResponseStream? expiring = null;
        expiring = new ResponseStream(
            () => new ExpiringEventStream(
                stream.ReadResultsAsync(),
                expiring!,
                user,
                _evaluator,
                scope,
                plan,
                variables,
                expiry,
                _timeProvider,
                _errorHandler,
                failure => _diagnosticEvents.RequestError(context, failure)));
        expiring.RegisterForCleanup(stream);
        return expiring;
    }

    private static DateTimeOffset? GetExpiry(ClaimsPrincipal user)
        => user.FindFirst(ExpiryClaimType) is { Value: var value }
            && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds)
                ? DateTimeOffset.FromUnixTimeSeconds(seconds)
                : null;

    private async ValueTask RejectAsync(RequestContext context, SelectionDenial denial)
    {
        var challenge = denial.Kind is PolicyDenialKind.Unauthenticated
            ? await _schemeResolver.GetChallengeAsync(context.RequestAborted)
            : null;

        var result = ErrorHelper.AuthorizationRejected(denial, _options.EnableAttribution, challenge);

        _diagnosticEvents.RequestError(context, result.Errors[0]);
        context.Result = result;
    }

    internal static ClaimsPrincipal GetUser(RequestContext context)
        => context.ContextData.TryGetValue(nameof(ClaimsPrincipal), out var value)
            && value is ClaimsPrincipal user
                ? user
                : new ClaimsPrincipal(new ClaimsIdentity());

    public static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (fc, next) =>
            {
                var schema = fc.SchemaServices.GetRequiredService<FusionSchemaDefinition>();
                var options = fc.SchemaServices.GetRequiredService<FusionPolicyOptions>();
                var schemeResolver = fc.SchemaServices.GetRequiredService<AuthenticationSchemeResolver>();
                var diagnosticEvents = fc.SchemaServices.GetRequiredService<IFusionExecutionDiagnosticEvents>();
                var auditProvider = fc.SchemaServices.GetRequiredService<IAuditProvider>();
                var booleanType = GetBooleanType(schema);
                var timeProvider = fc.SchemaServices.GetService<TimeProvider>() ?? TimeProvider.System;
                var errorHandler = fc.SchemaServices.GetRequiredService<IErrorHandler>();
                var middleware = new OperationPolicyMiddleware(
                    new PolicyEvaluator(options, diagnosticEvents),
                    auditProvider,
                    options,
                    schemeResolver,
                    diagnosticEvents,
                    booleanType,
                    timeProvider,
                    errorHandler);
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
