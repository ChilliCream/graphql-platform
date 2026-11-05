using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Policies;
using HotChocolate.Fusion.Policies.Audit;
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
        var trail = context.Features.TryGet(out AuditInvocation? auditInvocation)
            ? auditInvocation.GetOrCreateTrail(_auditProvider, context.RequestServices)
            : _auditProvider.CreateTrail(context.RequestServices);
        PolicyDecisions?[]? decisionSets = null;
        IVariableValueCollection[]? updatedVariableSets = null;
        var isSubscription = plan.Operation.Definition.Operation is OperationType.Subscription;
        var expiry = isSubscription ? GetExpiry(user) : null;
        IAuditScope? subscriptionScope = null;
        IVariableValueCollection? subscriptionVariables = null;

        if (expiry <= _timeProvider.GetUtcNow())
        {
            await RefuseExpiredAsync(context, trail, user, plan, policies, variableSets);
            return;
        }

        for (var i = 0; i < variableSets.Length; i++)
        {
            var scope = trail.BeginRequest(
                new AuditScopeInfo(plan.Operation.Id, plan.Id, context.RequestIndex, i),
                user);

            PolicyEvaluation evaluation;

            try
            {
                evaluation = await _evaluator.EvaluateAsync(
                    context,
                    user,
                    plan,
                    policies,
                    variableSets[i],
                    scope,
                    i,
                    context.RequestAborted);
            }
            catch (Exception ex) when (PolicyEvaluator.IsFault(ex, context.RequestAborted))
            {
                await _evaluator.CommitFaultedScopeAsync(context, scope, ex);

                if (ex is OperationCanceledException)
                {
                    throw ThrowHelper.OperationPolicyFaulted(ex);
                }

                throw;
            }

            var failure = evaluation.Failure;

            if (failure is not null)
            {
                scope.Fail(failure);
            }

            await scope.CommitAsync(context.RequestAborted);

            if (failure is not null)
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

            if (isSubscription
                && decisions is not null
                && decisions.TryGetDenial(plan.Operation.RootSelectionSet, out var rootDenial))
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

            if (!isSubscription && variableSets[i] is VariableValueCollection values)
            {
                PolicyVariableValues.AddTo(values, policies.Variables, decisions, _booleanType);
            }
            else
            {
                updatedVariableSets ??= [.. variableSets];
                updatedVariableSets[i] = PolicyVariableValues.Create(
                    variableSets[i],
                    policies.Variables,
                    decisions,
                    _booleanType);
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

        if (expiry is { } end
            && subscriptionScope is not null
            && context.Result is ResponseStream stream)
        {
            context.Result = EndOnExpiry(
                context,
                user,
                plan,
                policies,
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
        PolicyPlan policies,
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
                policies,
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
        PolicyPlan policies,
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
                policies,
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
