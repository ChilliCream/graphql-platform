using System.Collections.Concurrent;
using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

public sealed partial class OperationPlanContext
{
    private ConcurrentDictionary<
        IAuthorizationPolicy,
        Lazy<Task<AuthorizationPolicyDecision>>>? _authorizationPolicyDecisions;

    internal ValueTask<AuthorizationPolicyDecision> EvaluatePolicyOnceAsync(
        IAuthorizationPolicy policy,
        ISelection? selection,
        ITypeDefinition type,
        ClaimsPrincipal user,
        PolicyApplication application,
        CompositeResultElement entity,
        CancellationToken cancellationToken)
    {
        var decisions = Volatile.Read(ref _authorizationPolicyDecisions);

        if (decisions is null)
        {
            var newDecisions = new ConcurrentDictionary<
                IAuthorizationPolicy,
                Lazy<Task<AuthorizationPolicyDecision>>>(ReferenceEqualityComparer.Instance);
            decisions = Interlocked.CompareExchange(
                ref _authorizationPolicyDecisions,
                newDecisions,
                null) ?? newDecisions;
        }

        var state = new AuthorizationPolicyEvaluationState(
            this,
            selection,
            type,
            user,
            application,
            entity,
            cancellationToken);
        var evaluation = decisions.GetOrAdd(
            policy,
            static (policy, state) => new Lazy<Task<AuthorizationPolicyDecision>>(
                () => EvaluatePolicyAsync(policy, state),
                LazyThreadSafetyMode.ExecutionAndPublication),
            state);

        return new ValueTask<AuthorizationPolicyDecision>(evaluation.Value);
    }

    private static async Task<AuthorizationPolicyDecision> EvaluatePolicyAsync(
        IAuthorizationPolicy policy,
        AuthorizationPolicyEvaluationState state)
    {
        var authorizationContext = new RequestAuthorizationContext(
            state.OperationContext,
            state.Selection,
            state.Type,
            state.User,
            state.Application);

        try
        {
            await policy.EvaluateAsync(
                authorizationContext,
                new EntityData([state.Entity], 1),
                state.CancellationToken)
                .ConfigureAwait(false);
            return authorizationContext.Decision;
        }
        finally
        {
            authorizationContext.Deactivate();
        }
    }

    private readonly record struct AuthorizationPolicyEvaluationState(
        OperationPlanContext OperationContext,
        ISelection? Selection,
        ITypeDefinition Type,
        ClaimsPrincipal User,
        PolicyApplication Application,
        CompositeResultElement Entity,
        CancellationToken CancellationToken);
}
