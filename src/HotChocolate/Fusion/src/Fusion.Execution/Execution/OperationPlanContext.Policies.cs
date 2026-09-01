using System.Collections.Immutable;
using System.Security.Claims;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution;

public sealed partial class OperationPlanContext
{
    private readonly object _resolvedPoliciesSync = new();
    private Dictionary<string, IPolicy>? _resolvedPolicies;
    private PolicyRequestState? _policyRequestState;

    /// <summary>
    /// Returns the policy instance pinned before node execution for this operation.
    /// </summary>
    internal IPolicy ResolvePolicy(string name)
    {
        if (_policyRequestState is { } requestState)
        {
            return requestState.ResolvePolicy(name);
        }

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

            // An empty snapshot uses the schema's current policy snapshot.
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

    internal ValueTask<PolicyDecision> EvaluateRequestPolicyAsync(
        string name,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var requestState = GetOrCreatePolicyRequestState();
        return requestState.EvaluatePolicyOnceAsync(
            requestState.ResolvePolicy(name),
            user,
            cancellationToken);
    }

    /// <summary>
    /// Clears request-constant decisions and recomputes policy gates from the pinned policy instances.
    /// </summary>
    internal async ValueTask<PolicySlotEvaluationResult> ReevaluatePolicySlotsAsync(
        CancellationToken cancellationToken)
    {
        _policyRequestState?.ClearDecisions();
        PolicyDenyFlags = 0;

        var operationPlan = OperationPlan as OperationPlan
            ?? _policyRequestState?.OperationPlan;
        if (operationPlan is not { PolicySlots.IsEmpty: false })
        {
            if (Variables is PolicyVariableValueCollection emptyPolicyVariables)
            {
                emptyPolicyVariables.SetFlags(0, 0, 0);
            }

            _resultStore.SetPolicyExecutionState(operationPlan, _policyRequestState, 0);
            return default;
        }

        var requestState = _policyRequestState
            ?? PolicyRequestState.GetOrCreate(RequestContext, operationPlan, _diagnosticEvents);
        _policyRequestState = requestState;

        var variables = Variables is PolicyVariableValueCollection policyVariables
            ? policyVariables.Inner
            : Variables;
        PolicySlotEvaluationResult evaluation;

        using (_diagnosticEvents.EvaluateRequestPolicies(RequestContext))
        {
            evaluation = await requestState.EvaluateSlotsAsync(
                operationPlan,
                variables,
                cancellationToken)
                .ConfigureAwait(false);
        }

        if (Variables is PolicyVariableValueCollection currentVariables)
        {
            currentVariables.SetFlags(
                evaluation.LiveFlags,
                evaluation.DenyFlags,
                evaluation.FetchGateDenyFlags);
        }
        else
        {
            Variables = new PolicyVariableValueCollection(
                Variables,
                operationPlan.PolicySlots.Length,
                evaluation.LiveFlags,
                evaluation.DenyFlags,
                evaluation.FetchGateDenyFlags);
        }

        PolicyDenyFlags = evaluation.DenyFlags;
        _resultStore.SetPolicyExecutionState(
            operationPlan,
            requestState,
            PolicyDenyFlags);
        return evaluation;
    }

    private PolicyRequestState GetOrCreatePolicyRequestState()
    {
        if (_policyRequestState is { } requestState)
        {
            return requestState;
        }

        if (OperationPlan is not OperationPlan operationPlan)
        {
            throw ThrowHelper.PolicyOperationPlanMissing();
        }

        requestState = PolicyRequestState.GetOrCreate(
            RequestContext,
            operationPlan,
            _diagnosticEvents);
        _policyRequestState = requestState;
        return requestState;
    }
}
