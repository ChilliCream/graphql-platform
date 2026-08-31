using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Security.Claims;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution;

public sealed partial class OperationPlanContext
{
    private readonly object _resolvedPoliciesSync = new();
    private Dictionary<string, IPolicy>? _resolvedPolicies;

    private ConcurrentDictionary<
        IPolicy,
        Lazy<Task<PolicyDecision>>>? _policyDecisions;

    /// <summary>
    /// Returns the policy instance pinned before node execution for this operation.
    /// </summary>
    internal IPolicy ResolvePolicy(string name)
    {
        lock (_resolvedPoliciesSync)
        {
            if (_resolvedPolicies is not null
                && _resolvedPolicies.TryGetValue(name, out var resolved))
            {
                return resolved;
            }

            throw ThrowHelper.PolicyNameNotFound(name);
        }
    }

    private void PinPolicies(ImmutableArray<IPolicy> policySnapshot, IOperationPlan operationPlan)
    {
        foreach (var plan in EnumeratePlanParts(operationPlan))
        {
            if (plan is not OperationPlan { Policies.IsEmpty: false } rootPlan)
            {
                continue;
            }

            // Request execution captures this snapshot before cache lookup and planning. The fallback
            // preserves the contract for direct OperationPlanContext callers that bypass the request
            // pipeline, primarily focused unit tests.
            if (policySnapshot.IsDefault)
            {
                policySnapshot = Schema.Policies.GetSnapshot();
            }

            _resolvedPolicies ??= new Dictionary<string, IPolicy>(StringComparer.Ordinal);

            foreach (var entry in rootPlan.Policies)
            {
                if (_resolvedPolicies.ContainsKey(entry.PolicyName))
                {
                    continue;
                }

                foreach (var policy in policySnapshot)
                {
                    if (policy.Name.Equals(entry.PolicyName, StringComparison.Ordinal))
                    {
                        _resolvedPolicies.Add(entry.PolicyName, policy);
                        break;
                    }
                }

                if (!_resolvedPolicies.ContainsKey(entry.PolicyName))
                {
                    throw ThrowHelper.PolicyNameNotFound(entry.PolicyName);
                }
            }
        }
    }

    private static IEnumerable<IOperationPlan> EnumeratePlanParts(IOperationPlan rootPlan)
    {
        yield return rootPlan;

        foreach (var incrementalPlan in rootPlan.IncrementalPlans)
        {
            yield return incrementalPlan;
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
