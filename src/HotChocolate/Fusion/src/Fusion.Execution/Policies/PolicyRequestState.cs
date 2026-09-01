using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Holds the policy instances and request-constant decisions pinned to one request.
/// </summary>
internal sealed class PolicyRequestState
{
    private readonly RequestContext _requestContext;
    private readonly IFusionExecutionDiagnosticEvents _diagnosticEvents;
    private readonly Dictionary<string, IPolicy> _policies;
    private readonly ConcurrentDictionary<IPolicy, Lazy<Task<PolicyDecision>>> _decisions =
        new(ReferenceEqualityComparer.Instance);
    private readonly SemaphoreSlim _evaluationLock = new(1, 1);
    private readonly PolicyContext _policyContext;
    private PolicyDecision[] _expressionDecisions = [];
    private bool[] _evaluatedExpressions = [];
    private OperationResult?[]? _shortCircuitResults;
    private PolicySlotDenial[][] _coordinateDenials = [];

    private PolicyRequestState(
        RequestContext requestContext,
        IFusionExecutionDiagnosticEvents diagnosticEvents,
        Dictionary<string, IPolicy> policies,
        OperationPlan operationPlan)
    {
        _requestContext = requestContext;
        _diagnosticEvents = diagnosticEvents;
        _policies = policies;
        _policyContext = new PolicyContext(requestContext.Features);
        OperationPlan = operationPlan;
    }

    internal OperationPlan OperationPlan { get; }

    internal int ReductionBufferCapacity => _expressionDecisions.Length;

    internal static PolicyRequestState GetOrCreate(
        RequestContext requestContext,
        OperationPlan operationPlan,
        IFusionExecutionDiagnosticEvents diagnosticEvents)
    {
        HydrateUserState(requestContext);

        if (requestContext.Features.Get<PolicyRequestState>() is { } current)
        {
            return current;
        }

        var snapshot = requestContext.GetPolicySnapshot();
        if (snapshot.IsDefault)
        {
            snapshot = ((FusionSchemaDefinition)requestContext.Schema).Policies.GetSnapshot();
            requestContext.SetPolicySnapshot(snapshot);
        }

        var snapshotByName = new Dictionary<string, IPolicy>(StringComparer.Ordinal);
        foreach (var policy in snapshot)
        {
            snapshotByName.TryAdd(policy.Name, policy);
        }

        var policies = new Dictionary<string, IPolicy>(StringComparer.Ordinal);
        var requestRequirementHash = PolicyPlanEntry.ComputeRequirementHash(null);
        foreach (var entry in operationPlan.Policies)
        {
            if (policies.ContainsKey(entry.PolicyName))
            {
                continue;
            }

            if (!snapshotByName.TryGetValue(entry.PolicyName, out var policy))
            {
                throw ThrowHelper.PolicyNameNotFound(entry.PolicyName);
            }

            policies.Add(entry.PolicyName, policy);

            if (entry.RequirementHash == requestRequirementHash)
            {
                var pinnedPolicy = policies[entry.PolicyName];
                var requirementHash = PolicyPlanEntry.ComputeRequirementHash(
                    pinnedPolicy.Requirements.Resource);
                if (requirementHash != entry.RequirementHash)
                {
                    throw ThrowHelper.PolicyRequirementsChanged(entry.PolicyName);
                }
            }
        }

        var created = new PolicyRequestState(
            requestContext,
            diagnosticEvents,
            policies,
            operationPlan);
        requestContext.Features.Set(created);
        return created;
    }

    internal static void HydrateUserState(RequestContext requestContext)
    {
        if (requestContext.Features.Get<UserState>() is null
            && requestContext.ContextData.TryGetValue(nameof(ClaimsPrincipal), out var value)
            && value is ClaimsPrincipal principal)
        {
            requestContext.Features.Set(new UserState(principal));
        }
    }

    internal IPolicy ResolvePolicy(string name)
        => _policies.TryGetValue(name, out var policy)
            ? policy
            : throw ThrowHelper.PolicyNameNotFound(name);

    internal ValueTask<PolicyDecision> EvaluatePolicyOnceAsync(
        IPolicy policy,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var state = new PolicyEvaluationState(this, user, cancellationToken);
        var evaluation = _decisions.GetOrAdd(
            policy,
            static (currentPolicy, currentState) => new Lazy<Task<PolicyDecision>>(
                () => currentState.RequestState.EvaluatePolicyAsync(currentPolicy, currentState),
                LazyThreadSafetyMode.ExecutionAndPublication),
            state);

        return new ValueTask<PolicyDecision>(evaluation.Value);
    }

    internal void ClearDecisions()
    {
        _decisions.Clear();
        Array.Clear(_expressionDecisions);
        Array.Clear(_evaluatedExpressions);
        BeginReduction();
    }

    internal void BeginReduction()
    {
        foreach (var denials in _coordinateDenials)
        {
            if (denials is not null)
            {
                Array.Clear(denials);
            }
        }

        _shortCircuitResults = null;
    }

    internal bool TryGetCoordinateDenial(
        int slotOrdinal,
        int coordinateOrdinal,
        out PolicySlotDenial denial)
    {
        if ((uint)slotOrdinal < (uint)_coordinateDenials.Length
            && (uint)coordinateOrdinal < (uint)_coordinateDenials[slotOrdinal].Length
            && _coordinateDenials[slotOrdinal][coordinateOrdinal] is { IsDenied: true } current)
        {
            denial = current;
            return true;
        }

        denial = default;
        return false;
    }

    internal void SetShortCircuitResults(OperationResult?[] results)
        => _shortCircuitResults = results;

    internal bool TryGetShortCircuitResult(int variableIndex, out OperationResult result)
    {
        if (_shortCircuitResults is { } results
            && (uint)variableIndex < (uint)results.Length
            && results[variableIndex] is { } current)
        {
            result = current;
            return true;
        }

        result = null!;
        return false;
    }

    internal async ValueTask<PolicySlotEvaluationResult> EvaluateSlotsAsync(
        OperationPlan operationPlan,
        IVariableValueCollection variables,
        CancellationToken cancellationToken)
    {
        var includeFlags = operationPlan.Operation.CreateIncludeFlags(variables);
        var user = _requestContext.Features.Get<UserState>()?.User ?? new ClaimsPrincipal();

        var denyFlags = 0UL;
        var fetchGateDenyFlags = 0UL;
        var liveFlags = 0UL;
        var shortCircuit = false;
        PolicySlotDenial? shortCircuitDenial = null;

        EnsureReductionBuffers(operationPlan.PolicyExpressions.Length);

        if (_coordinateDenials.Length < operationPlan.PolicySlots.Length)
        {
            _coordinateDenials = new PolicySlotDenial[operationPlan.PolicySlots.Length][];
        }

        foreach (var slot in operationPlan.PolicySlots)
        {
            if (_coordinateDenials[slot.Ordinal] is not { } denials
                || denials.Length < slot.Coordinates.Length)
            {
                _coordinateDenials[slot.Ordinal] = new PolicySlotDenial[slot.Coordinates.Length];
            }
        }

        foreach (var slot in operationPlan.PolicySlots)
        {
            if (!IsLive(slot.GuardMasks, includeFlags))
            {
                continue;
            }

            liveFlags |= 1UL << slot.Ordinal;

            foreach (var application in slot.Applications)
            {
                var expressionOrdinal = application.ExpressionOrdinal;
                if (!_evaluatedExpressions[expressionOrdinal])
                {
                    _expressionDecisions[expressionOrdinal] =
                        await EvaluateExpressionAsync(
                            operationPlan.PolicyExpressions[expressionOrdinal],
                            user,
                            cancellationToken)
                            .ConfigureAwait(false);
                    _evaluatedExpressions[expressionOrdinal] = true;
                }
            }

            for (var coordinateOrdinal = 0;
                coordinateOrdinal < slot.Coordinates.Length;
                coordinateOrdinal++)
            {
                var coordinate = slot.Coordinates[coordinateOrdinal];
                if (!IsLive(coordinate.LiveGuardMasks, includeFlags))
                {
                    continue;
                }

                var gateIsLive = IsLive(coordinate.GateGuardMasks, includeFlags);

                var denied = false;
                var denialBehavior = default(PolicyDenialBehavior);
                var denialExpression = string.Empty;
                string? denialReason = null;

                foreach (var application in coordinate.Applications)
                {
                    var expressionDecision = _expressionDecisions[application.ExpressionOrdinal];
                    if (!expressionDecision.IsDenied)
                    {
                        continue;
                    }

                    if (!denied || application.OnDenied >= denialBehavior)
                    {
                        denied = true;
                        denialBehavior = application.OnDenied;
                        denialExpression = operationPlan
                            .PolicyExpressions[application.ExpressionOrdinal]
                            .Text;
                        denialReason = expressionDecision.Reason;
                    }
                }

                if (!denied || denialBehavior < slot.Rmax)
                {
                    continue;
                }

                denyFlags |= 1UL << slot.Ordinal;
                if (gateIsLive)
                {
                    fetchGateDenyFlags |= 1UL << slot.Ordinal;
                }

                PolicySlotDenial denial;
                if (_coordinateDenials[slot.Ordinal][coordinateOrdinal] is { IsDenied: true } current)
                {
                    denial = current;
                }
                else
                {
                    var reasonId = Guid.NewGuid();
                    var subjectId = GetSubjectId(user);
                    denial = new PolicySlotDenial(
                        true,
                        denialBehavior,
                        denialExpression,
                        denialReason,
                        reasonId,
                        subjectId,
                        coordinate.IsRoot);
                    _coordinateDenials[slot.Ordinal][coordinateOrdinal] = denial;
                    _diagnosticEvents.PolicySlotDenied(
                        _requestContext,
                        slot.VariableName,
                        denialExpression,
                        coordinate.TypeName,
                        coordinate.FieldName,
                        denialBehavior,
                        denialReason,
                        reasonId,
                        subjectId);
                }

                if (gateIsLive
                    && (coordinate.IsRoot || denialBehavior is PolicyDenialBehavior.Abort)
                    && !shortCircuit)
                {
                    shortCircuit = true;
                    shortCircuitDenial = denial;
                }
            }
        }

        return new PolicySlotEvaluationResult(
            liveFlags,
            denyFlags,
            fetchGateDenyFlags,
            shortCircuit,
            shortCircuitDenial ?? default);
    }

    private void EnsureReductionBuffers(int expressionCount)
    {
        if (_expressionDecisions.Length >= expressionCount)
        {
            return;
        }

        _expressionDecisions = new PolicyDecision[expressionCount];
        _evaluatedExpressions = new bool[expressionCount];
    }

    private async Task<PolicyDecision> EvaluatePolicyAsync(
        IPolicy policy,
        PolicyEvaluationState state)
    {
        await _evaluationLock.WaitAsync(state.CancellationToken).ConfigureAwait(false);
        var start = Stopwatch.GetTimestamp();

        try
        {
            _policyContext.ResetForRequest(state.User);
            await policy.EvaluateAsync(_policyContext, state.CancellationToken).ConfigureAwait(false);
            var decision = _policyContext.GetDecision(0);
            _diagnosticEvents.PolicyEvaluated(
                _requestContext,
                policy.Name,
                decision.IsDenied,
                Stopwatch.GetElapsedTime(start));
            return decision;
        }
        finally
        {
            _policyContext.Clear();
            _evaluationLock.Release();
        }
    }

    private async ValueTask<PolicyDecision> EvaluateExpressionAsync(
        PolicyConditionExpression expression,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        string? reason = null;

        foreach (var group in expression.Groups)
        {
            var groupDenied = false;

            foreach (var name in group)
            {
                var decision = await EvaluatePolicyOnceAsync(
                    ResolvePolicy(name),
                    user,
                    cancellationToken)
                    .ConfigureAwait(false);
                if (decision.IsDenied)
                {
                    groupDenied = true;
                    reason ??= decision.Reason;
                }
            }

            if (!groupDenied)
            {
                return default;
            }
        }

        return new PolicyDecision(true, reason);
    }

    private static bool IsLive(ImmutableArray<ulong> guardMasks, ulong includeFlags)
    {
        foreach (var guardMask in guardMasks)
        {
            if ((includeFlags & guardMask) == guardMask)
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetSubjectId(ClaimsPrincipal user)
        => user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

    private readonly record struct PolicyEvaluationState(
        PolicyRequestState RequestState,
        ClaimsPrincipal User,
        CancellationToken CancellationToken);
}

internal readonly record struct PolicySlotEvaluationResult(
    ulong LiveFlags,
    ulong DenyFlags,
    ulong FetchGateDenyFlags,
    bool ShouldShortCircuit,
    PolicySlotDenial ShortCircuitDenial);

internal readonly record struct PolicySlotDenial(
    bool IsDenied,
    PolicyDenialBehavior Behavior,
    string Expression,
    string? Reason,
    Guid ReasonId,
    string? SubjectId,
    bool IsRoot);
