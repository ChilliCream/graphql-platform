using System.Buffers;
using System.Collections;
using System.Security.Claims;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Evaluates authorization policies for one or more result positions.
/// </summary>
public sealed class PolicyExecutionNode : ExecutionNode
{
    private readonly PolicyExecutionTarget[] _targets;
    private readonly ExecutionNodeCondition[] _conditions;

    internal PolicyExecutionNode(
        int id,
        PolicyExecutionTarget[] targets,
        ExecutionNodeCondition[] conditions)
    {
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentNullException.ThrowIfNull(conditions);

        Id = id;
        _targets = targets;
        _conditions = conditions;
    }

    public override int Id { get; }

    public override ExecutionNodeType Type => ExecutionNodeType.Policy;

    public override ReadOnlySpan<ExecutionNodeCondition> Conditions => _conditions;

    public override string? SchemaName => null;

    /// <summary>
    /// Gets the policy targets evaluated by this node.
    /// </summary>
    public ReadOnlySpan<PolicyExecutionTarget> Targets => _targets;

    protected override async ValueTask<ExecutionStatus> OnExecuteAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await ExecutePolicyAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ExecutionStatus.Failed;
        }
    }

    protected override void OnError(
        OperationPlanContext context,
        IDisposable? scope,
        Exception error)
    {
        try
        {
            context.AbortPolicyExecution();
        }
        catch (Exception abortError)
        {
            context.DiagnosticEvents.ExecutionNodeError(context, this, abortError);
        }
        finally
        {
            base.OnError(context, scope, error);
        }
    }

    private async ValueTask<ExecutionStatus> ExecutePolicyAsync(
        OperationPlanContext context,
        CancellationToken cancellationToken)
    {
        var schema = context.Schema;
        var user = context.Features.Get<UserState>()?.User ?? new ClaimsPrincipal();
        var policyContext = new PolicyContext(context);
        List<SelectionPath>? fullyDeniedPaths = null;
        var aborted = false;

        foreach (var target in _targets)
        {
            if (!AreConditionsMet(context, target.Conditions))
            {
                continue;
            }

            CompositeResultElement[] effects;
            CompositeResultElement[] entities;
            var effectCount = 0;

            if (target.Kind is PolicyTargetKind.Field)
            {
                var parentPath = target.Path.Parent ?? SelectionPath.Root;
                var parents = context.RentResultElements(parentPath, out var parentCount);

                try
                {
                    entities = new CompositeResultElement[parentCount];
                    effects = new CompositeResultElement[parentCount];
                    var responseName = target.Path.Name!;

                    for (var i = 0; i < parentCount; i++)
                    {
                        var entity = parents[i];

                        if (entity.TryGetProperty(responseName, out var effect)
                            && !effect.IsNullOrInvalidated)
                        {
                            entities[effectCount] = entity;
                            effects[effectCount] = effect;
                            effectCount++;
                        }
                    }
                }
                finally
                {
                    ArrayPool<CompositeResultElement>.Shared.Return(parents, clearArray: true);
                }
            }
            else
            {
                var collected = context.RentResultElements(target.Path, out effectCount);

                try
                {
                    entities = new CompositeResultElement[effectCount];
                    collected.AsSpan(0, effectCount).CopyTo(entities);
                    effects = entities;
                }
                finally
                {
                    ArrayPool<CompositeResultElement>.Shared.Return(collected, clearArray: true);
                }
            }

            if (effectCount == 0)
            {
                continue;
            }

            var selection = target.Kind is PolicyTargetKind.Field
                ? FindSelection(effects[0]) ?? FindSelection(entities[0])
                : null;
            var type = schema.Types.GetType<ITypeDefinition>(target.TypeName);

            var denied = new BitArray(effectCount);
            var denialBehaviors = new PolicyDenialBehavior[effectCount];
            var denialReasons = new string?[effectCount];
            var denialPolicies = new string?[effectCount];

            // Each distinct policy name is evaluated at most once per target,
            // even when it appears in multiple groups or applications. The OR
            // groups of an application are evaluated in order, and the remaining
            // groups are skipped once every entity is allowed, so a group whose
            // outcome can no longer change the result is not evaluated.
            var decisions = new Dictionary<string, TargetDecision>(StringComparer.Ordinal);

            foreach (var application in target.Policies)
            {
                var allowed = new bool[effectCount];

                foreach (var group in application.Groups)
                {
                    if (!HasUndecidedEntity(allowed, effectCount))
                    {
                        break;
                    }

                    var groupDenied = new bool[effectCount];

                    foreach (var name in group)
                    {
                        if (!decisions.TryGetValue(name, out var decision))
                        {
                            decision = await EvaluatePolicyForTargetAsync(
                                context,
                                policyContext,
                                name,
                                selection,
                                type,
                                user,
                                entities,
                                effectCount,
                                cancellationToken)
                                .ConfigureAwait(false);
                            decisions.Add(name, decision);
                        }

                        for (var i = 0; i < effectCount; i++)
                        {
                            groupDenied[i] |= decision.Denied[i];
                        }
                    }

                    for (var i = 0; i < effectCount; i++)
                    {
                        if (!groupDenied[i])
                        {
                            allowed[i] = true;
                        }
                    }
                }

                string? expression = null;

                for (var i = 0; i < effectCount; i++)
                {
                    if (allowed[i])
                    {
                        continue;
                    }

                    expression ??= application.Format();

                    if (!denied[i] || application.OnDenied >= denialBehaviors[i])
                    {
                        denialBehaviors[i] = application.OnDenied;
                        denialReasons[i] = GetDenialReason(application, decisions, i);
                        denialPolicies[i] = expression;
                    }

                    denied[i] = true;
                }
            }

            var allDenied = true;
            var abortIndex = -1;

            for (var i = 0; i < effectCount; i++)
            {
                if (!denied[i])
                {
                    allDenied = false;
                    continue;
                }

                if (denialBehaviors[i] is PolicyDenialBehavior.Abort)
                {
                    abortIndex = i;
                    break;
                }
            }

            if (abortIndex >= 0)
            {
                context.ApplyPolicyDenial(
                    effects[abortIndex],
                    PolicyDenialBehavior.Abort,
                    denialPolicies[abortIndex]!,
                    denialReasons[abortIndex]);
                aborted = true;
                break;
            }

            for (var i = 0; i < effectCount; i++)
            {
                if (!denied[i])
                {
                    continue;
                }

                var behavior = denialBehaviors[i];
                context.ApplyPolicyDenial(
                    effects[i],
                    behavior,
                    denialPolicies[i]!,
                    denialReasons[i]);
            }

            if (allDenied)
            {
                fullyDeniedPaths ??= [];
                fullyDeniedPaths.Add(target.Path);
            }
        }

        if (!aborted && fullyDeniedPaths is not null)
        {
            SelectUsefulDependents(context, fullyDeniedPaths);
        }

        // Drops every reference the reused context holds onto (entities, type, user) now that
        // this round's evaluations are complete.
        policyContext.Clear();

        return aborted ? ExecutionStatus.Failed : ExecutionStatus.Success;
    }

    private static async ValueTask<TargetDecision> EvaluatePolicyForTargetAsync(
        OperationPlanContext context,
        PolicyContext policyContext,
        string name,
        Selection? selection,
        ITypeDefinition type,
        ClaimsPrincipal user,
        CompositeResultElement[] entities,
        int effectCount,
        CancellationToken cancellationToken)
    {
        // The policy instance is resolved once per operation (ResolvePolicy). The plan itself was
        // built against the policy snapshot published at planning time (OperationPlanner reads it
        // once per plan), and PolicyCollection evicts a cached plan built for a different
        // requirement before it publishes a new set, so the resource requirement read here always
        // matches the plan the current published set corresponds to.
        var policy = context.ResolvePolicy(name);
        var resource = policy.Requirements.Resource;

        var policyDenied = new BitArray(effectCount);
        var policyReasons = new string?[effectCount];

        if (resource is null)
        {
            // A request-cacheable policy is evaluated at most once per request and its decision is
            // reused across every application.
            var decision = await context.EvaluatePolicyOnceAsync(
                policy,
                user,
                cancellationToken)
                .ConfigureAwait(false);

            if (decision.IsDenied)
            {
                for (var i = 0; i < effectCount; i++)
                {
                    policyDenied[i] = true;
                    policyReasons[i] = decision.Reason;
                }
            }

            return new TargetDecision(policyDenied, policyReasons);
        }

        EnsureRequirementsAreAvailable(name, resource, entities.AsSpan(0, effectCount));

        policyContext.ResetForResource(
            user,
            type,
            selection,
            context.Variables,
            new ReadOnlyMemory<CompositeResultElement>(entities, 0, effectCount));

        await policy.EvaluateAsync(policyContext, cancellationToken).ConfigureAwait(false);

        for (var i = 0; i < effectCount; i++)
        {
            policyDenied[i] = policyContext.IsDenied(i);
            policyReasons[i] = policyContext.GetReason(i);
        }

        return new TargetDecision(policyDenied, policyReasons);
    }

    private static bool HasUndecidedEntity(bool[] allowed, int effectCount)
    {
        for (var i = 0; i < effectCount; i++)
        {
            if (!allowed[i])
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetDenialReason(
        PolicyApplication application,
        Dictionary<string, TargetDecision> decisions,
        int index)
    {
        foreach (var group in application.Groups)
        {
            foreach (var name in group)
            {
                var decision = decisions[name];

                if (decision.Denied[index] && decision.Reasons[index] is { } reason)
                {
                    return reason;
                }
            }
        }

        return null;
    }

    private readonly struct TargetDecision(BitArray denied, string?[] reasons)
    {
        public BitArray Denied { get; } = denied;

        public string?[] Reasons { get; } = reasons;
    }

    private static Selection? FindSelection(CompositeResultElement element)
    {
        while (true)
        {
            if (element.Selection is { } selection)
            {
                return selection;
            }

            if (element.CompactPath.IsRoot)
            {
                return null;
            }

            element = element.Parent;
        }
    }

    private static bool AreConditionsMet(
        OperationPlanContext context,
        ReadOnlySpan<ExecutionNodeCondition> conditions)
    {
        foreach (var condition in conditions)
        {
            if (!context.Variables.TryGetValue<BooleanValueNode>(
                    condition.VariableName,
                    out var value))
            {
                throw ThrowHelper.MissingBooleanVariable(condition.VariableName);
            }

            if (value.Value != condition.PassingValue)
            {
                return false;
            }
        }

        return true;
    }

    private static void EnsureRequirementsAreAvailable(
        string policyName,
        SelectionSetNode requirements,
        ReadOnlySpan<CompositeResultElement> entities)
    {
        foreach (var entity in entities)
        {
            EnsureRequirementsAreAvailable(policyName, requirements, entity);
        }
    }

    private static void EnsureRequirementsAreAvailable(
        string policyName,
        SelectionSetNode requirements,
        CompositeResultElement entity)
    {
        foreach (var selection in requirements.Selections)
        {
            if (selection is not FieldNode field)
            {
                throw new InvalidOperationException(
                    $"Authorization policy '{policyName}' has an unsupported requirement selection.");
            }

            var responseName = field.Alias?.Value ?? field.Name.Value;

            if (!entity.TryGetProperty(responseName, out var value))
            {
                throw new InvalidOperationException(
                    $"Authorization policy '{policyName}' requires field '{responseName}', "
                    + "but the execution plan did not provide it.");
            }

            if (field.SelectionSet is { } childRequirements
                && !value.IsNullOrInvalidated)
            {
                if (value.ValueKind is JsonValueKind.Array)
                {
                    for (var i = 0; i < value.GetArrayLength(); i++)
                    {
                        var item = value[i];

                        if (!item.IsNullOrInvalidated)
                        {
                            EnsureRequirementsAreAvailable(policyName, childRequirements, item);
                        }
                    }
                }
                else
                {
                    EnsureRequirementsAreAvailable(policyName, childRequirements, value);
                }
            }
        }
    }

    private void SelectUsefulDependents(
        OperationPlanContext context,
        IReadOnlyList<SelectionPath> fullyDeniedPaths)
    {
        var hasSkippedDependent = false;
        var hasUsefulDependent = false;

        foreach (var dependent in Dependents)
        {
            switch (dependent)
            {
                case OperationExecutionNode operation:
                    if (IsCovered(operation.Target, fullyDeniedPaths))
                    {
                        hasSkippedDependent = true;
                    }
                    else
                    {
                        hasUsefulDependent = true;
                    }
                    break;

                case ApolloOperationExecutionNode operation:
                    if (IsCovered(operation.Target, fullyDeniedPaths))
                    {
                        hasSkippedDependent = true;
                    }
                    else
                    {
                        hasUsefulDependent = true;
                    }
                    break;

                case OperationBatchExecutionNode batch:
                    TrackCoveredOperations(
                        context,
                        batch.Operations,
                        fullyDeniedPaths,
                        ref hasSkippedDependent,
                        ref hasUsefulDependent);
                    break;

                case ApolloOperationBatchExecutionNode batch:
                    TrackCoveredOperations(
                        context,
                        batch.Operations,
                        fullyDeniedPaths,
                        ref hasSkippedDependent,
                        ref hasUsefulDependent);
                    break;

                default:
                    hasUsefulDependent = true;
                    break;
            }
        }

        if (!hasSkippedDependent)
        {
            return;
        }

        if (!hasUsefulDependent)
        {
            context.SkipAllDependents(this);
            return;
        }

        foreach (var dependent in Dependents)
        {
            if (IsUsefulDependent(dependent, fullyDeniedPaths))
            {
                context.EnqueueDependent(
                    this,
                    context.OperationPlan.GetExecutionNode(dependent));
            }
        }
    }

    private void TrackCoveredOperations<T>(
        OperationPlanContext context,
        ReadOnlySpan<T> operations,
        IReadOnlyList<SelectionPath> fullyDeniedPaths,
        ref bool hasSkippedDependent,
        ref bool hasUsefulDependent)
        where T : OperationDefinition
    {
        foreach (var operation in operations)
        {
            if (DependsOnPolicy(operation) && IsCovered(operation, fullyDeniedPaths))
            {
                context.TrackSkippedDefinition(this, operation);
                hasSkippedDependent = true;
            }
            else
            {
                hasUsefulDependent = true;
            }
        }
    }

    private bool IsUsefulDependent(
        IOperationPlanNode dependent,
        IReadOnlyList<SelectionPath> fullyDeniedPaths)
        => dependent switch
        {
            OperationExecutionNode operation
                => !IsCovered(operation.Target, fullyDeniedPaths),
            ApolloOperationExecutionNode operation
                => !IsCovered(operation.Target, fullyDeniedPaths),
            OperationBatchExecutionNode batch
                => HasUsefulOperation(batch.Operations, fullyDeniedPaths),
            ApolloOperationBatchExecutionNode batch
                => HasUsefulOperation(batch.Operations, fullyDeniedPaths),
            _ => true
        };

    private bool HasUsefulOperation<T>(
        ReadOnlySpan<T> operations,
        IReadOnlyList<SelectionPath> fullyDeniedPaths)
        where T : OperationDefinition
    {
        foreach (var operation in operations)
        {
            if (!DependsOnPolicy(operation) || !IsCovered(operation, fullyDeniedPaths))
            {
                return true;
            }
        }

        return false;
    }

    private bool DependsOnPolicy(OperationDefinition operation)
    {
        foreach (var dependency in operation.Dependencies)
        {
            if (ReferenceEquals(dependency, this))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCovered(
        OperationDefinition operation,
        IReadOnlyList<SelectionPath> fullyDeniedPaths)
        => operation switch
        {
            SingleOperationDefinition single => IsCovered(single.Target, fullyDeniedPaths),
            BatchOperationDefinition batch => AreAllCovered(batch.Targets, fullyDeniedPaths),
            _ => false
        };

    private static bool AreAllCovered(
        ReadOnlySpan<SelectionPath> targets,
        IReadOnlyList<SelectionPath> fullyDeniedPaths)
    {
        foreach (var target in targets)
        {
            if (!IsCovered(target, fullyDeniedPaths))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsCovered(
        SelectionPath target,
        IReadOnlyList<SelectionPath> fullyDeniedPaths)
    {
        foreach (var deniedPath in fullyDeniedPaths)
        {
            if (deniedPath.IsParentOfOrSame(target))
            {
                return true;
            }
        }

        return false;
    }
}
