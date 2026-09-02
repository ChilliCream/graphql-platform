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

    internal bool TryGetDeniedSubscriptionRoot(
        out PolicySlotDenial denial,
        out string? responseName)
    {
        var operationPlan = OperationPlan as OperationPlan
            ?? _policyRequestState?.OperationPlan;
        var requestState = _policyRequestState;

        if (PolicyDenyFlags == 0 || operationPlan is null || requestState is null)
        {
            denial = default;
            responseName = null;
            return false;
        }

        var rootSelectionSet = operationPlan.Operation.RootSelectionSet;
        var rootSelection = rootSelectionSet.Selections[0];
        var fieldCandidates = operationPlan.GetPolicyDenials(
            0,
            rootSelectionSet.Id,
            rootSelection.Id);
        var fieldIndex = 0;
        var rootSlotOrdinal = 0;
        var rootCoordinateOrdinal = 0;
        denial = default;
        responseName = null;
        var hasRootObjectCandidate = TryGetNextRootObjectCandidate(
            operationPlan.PolicySlots,
            ref rootSlotOrdinal,
            ref rootCoordinateOrdinal,
            out var rootObjectCandidate);

        while (fieldIndex < fieldCandidates.Length || hasRootObjectCandidate)
        {
            var fieldCandidate = fieldIndex < fieldCandidates.Length
                ? fieldCandidates[fieldIndex]
                : default;
            var useFieldCandidate = fieldIndex < fieldCandidates.Length
                && (!hasRootObjectCandidate
                    || fieldCandidate.SlotOrdinal < rootObjectCandidate.SlotOrdinal
                    || (fieldCandidate.SlotOrdinal == rootObjectCandidate.SlotOrdinal
                        && fieldCandidate.CoordinateOrdinal < rootObjectCandidate.CoordinateOrdinal));
            var candidate = useFieldCandidate ? fieldCandidate : rootObjectCandidate;

            if (useFieldCandidate)
            {
                fieldIndex++;
            }
            else
            {
                rootCoordinateOrdinal++;
                hasRootObjectCandidate = TryGetNextRootObjectCandidate(
                    operationPlan.PolicySlots,
                    ref rootSlotOrdinal,
                    ref rootCoordinateOrdinal,
                    out rootObjectCandidate);
            }

            if ((PolicyDenyFlags & (1UL << candidate.SlotOrdinal)) == 0
                || !requestState.TryGetCoordinateDenial(
                    candidate.SlotOrdinal,
                    candidate.CoordinateOrdinal,
                    out denial))
            {
                continue;
            }

            responseName = useFieldCandidate ? rootSelection.ResponseName : null;
            return true;
        }

        return false;

        static bool TryGetNextRootObjectCandidate(
            ImmutableArray<PolicyConditionSlot> slots,
            ref int slotOrdinal,
            ref int coordinateOrdinal,
            out PolicyDenialLookupEntry candidate)
        {
            while (slotOrdinal < slots.Length)
            {
                var slot = slots[slotOrdinal];
                while (coordinateOrdinal < slot.Coordinates.Length)
                {
                    var coordinate = slot.Coordinates[coordinateOrdinal];
                    if (coordinate.IsRoot)
                    {
                        candidate = new PolicyDenialLookupEntry(
                            slot.Ordinal,
                            coordinateOrdinal,
                            coordinate.LiveGuardMasks);
                        return true;
                    }

                    coordinateOrdinal++;
                }

                slotOrdinal++;
                coordinateOrdinal = 0;
            }

            candidate = default;
            return false;
        }
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
