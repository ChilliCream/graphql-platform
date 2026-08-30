using System.Collections.Concurrent;
using System.Security.Claims;

namespace HotChocolate.Fusion.Execution;

public sealed partial class OperationPlanContext
{
    private readonly object _resolvedPoliciesSync = new();
    private Dictionary<string, IPolicy>? _resolvedPolicies;

    private ConcurrentDictionary<
        IPolicy,
        Lazy<Task<PolicyDecision>>>? _policyDecisions;

    /// <summary>
    /// Resolves the policy with the specified name for this operation. The instance is resolved at
    /// most once per operation so that a provider update during the operation does not change which
    /// instance the operation observes.
    /// </summary>
    internal IPolicy ResolvePolicy(string name)
    {
        lock (_resolvedPoliciesSync)
        {
            _resolvedPolicies ??= new Dictionary<string, IPolicy>(StringComparer.Ordinal);

            if (_resolvedPolicies.TryGetValue(name, out var resolved))
            {
                return resolved;
            }

            var policy = Schema.Policies.Get(name);
            _resolvedPolicies.Add(name, policy);
            return policy;
        }
    }

    private void ClearResolvedPolicies() => _resolvedPolicies?.Clear();

    /// <summary>
    /// Evaluates a request-cacheable policy at most once per request and reuses the resulting
    /// decision for every application reached during the request.
    /// </summary>
    internal ValueTask<PolicyDecision> EvaluatePolicyOnceAsync(
        IPolicy policy,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var decisions = Volatile.Read(ref _policyDecisions);

        if (decisions is null)
        {
            var newDecisions = new ConcurrentDictionary<
                IPolicy,
                Lazy<Task<PolicyDecision>>>(ReferenceEqualityComparer.Instance);
            decisions = Interlocked.CompareExchange(
                ref _policyDecisions,
                newDecisions,
                null) ?? newDecisions;
        }

        var state = new PolicyEvaluationState(this, user, cancellationToken);
        var evaluation = decisions.GetOrAdd(
            policy,
            static (policy, state) => new Lazy<Task<PolicyDecision>>(
                () => EvaluatePolicyAsync(policy, state),
                LazyThreadSafetyMode.ExecutionAndPublication),
            state);

        return new ValueTask<PolicyDecision>(evaluation.Value);
    }

    private static async Task<PolicyDecision> EvaluatePolicyAsync(
        IPolicy policy,
        PolicyEvaluationState state)
    {
        var policyContext = new PolicyContext(state.OperationContext);
        policyContext.ResetForRequest(state.User);

        await policy.EvaluateAsync(policyContext, state.CancellationToken).ConfigureAwait(false);

        return policyContext.GetDecision(0);
    }

    private readonly record struct PolicyEvaluationState(
        OperationPlanContext OperationContext,
        ClaimsPrincipal User,
        CancellationToken CancellationToken);
}
