using System.Buffers;
using System.Security.Claims;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Features;
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

            var selection = FindSelection(effects[0]) ?? FindSelection(entities[0]);
            var type = schema.Types.GetType<ITypeDefinition>(target.TypeName);

            var denied = new bool[effectCount];
            var denialBehaviors = new PolicyDenialBehavior[effectCount];
            var denialReasons = new string?[effectCount];
            var denialPolicies = new string?[effectCount];

            foreach (var application in target.Policies)
            {
                var policy = schema.Policies.Get(application.Name);
                var requirements = policy.Requirements;
                var plannedRequirements = GetPlannedRequirements(
                    target.Requirements,
                    application.Name);

                if ((requirements is null) != (plannedRequirements is null)
                    || (requirements is not null
                        && !SyntaxComparer.BySyntax.Equals(requirements, plannedRequirements)))
                {
                    throw new InvalidOperationException(
                        $"Authorization policy '{application.Name}' requirements do not match "
                        + "the requirements used to build the operation plan.");
                }

                if (requirements is not null)
                {
                    EnsureRequirementsAreAvailable(
                        application.Name,
                        requirements,
                        entities.AsSpan(0, effectCount));
                }

                if (requirements is null)
                {
                    var decision = await context.EvaluatePolicyOnceAsync(
                        policy,
                        selection,
                        type,
                        user,
                        application,
                        entities[0],
                        cancellationToken)
                        .ConfigureAwait(false);

                    if (decision.IsDenied)
                    {
                        ApplyDeniedDecision(
                            application,
                            decision.Reason,
                            denied,
                            denialBehaviors,
                            denialReasons,
                            denialPolicies,
                            effectCount);
                    }

                    continue;
                }

                var authorizationContext =
                    new AuthorizationContext(
                        context,
                        selection,
                        type,
                        user,
                        application,
                        denied,
                        denialBehaviors,
                        denialReasons,
                        denialPolicies,
                        effectCount);

                var entityData = new EntityData(entities, effectCount);

                try
                {
                    await policy.EvaluateAsync(
                        authorizationContext,
                        entityData,
                        cancellationToken)
                        .ConfigureAwait(false);
                }
                finally
                {
                    authorizationContext.Deactivate();
                }
            }

            var allDenied = true;

            for (var i = 0; i < effectCount; i++)
            {
                if (!denied[i])
                {
                    allDenied = false;
                    continue;
                }

                var behavior = denialBehaviors[i];
                context.ApplyPolicyDenial(
                    effects[i],
                    behavior,
                    denialPolicies[i]!,
                    denialReasons[i]);
                aborted |= behavior is PolicyDenialBehavior.Abort;
            }

            if (allDenied)
            {
                fullyDeniedPaths ??= [];
                fullyDeniedPaths.Add(target.Path);
            }

            if (aborted)
            {
                break;
            }
        }

        if (!aborted && fullyDeniedPaths is not null)
        {
            SelectUsefulDependents(context, fullyDeniedPaths);
        }

        return aborted ? ExecutionStatus.Failed : ExecutionStatus.Success;
    }

    private static void ApplyDeniedDecision(
        PolicyApplication application,
        string? reason,
        bool[] denied,
        PolicyDenialBehavior[] denialBehaviors,
        string?[] denialReasons,
        string?[] denialPolicies,
        int effectCount)
    {
        for (var i = 0; i < effectCount; i++)
        {
            if (!denied[i] || application.OnDenied >= denialBehaviors[i])
            {
                denialBehaviors[i] = application.OnDenied;
                denialReasons[i] = reason;
                denialPolicies[i] = application.Name;
            }

            denied[i] = true;
        }
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

    private static SelectionSetNode? GetPlannedRequirements(
        ReadOnlySpan<AuthorizationPolicyRequirement> requirements,
        string name)
    {
        SelectionSetNode? selectionSet = null;

        foreach (var requirement in requirements)
        {
            if (!requirement.PolicyName.Equals(name, StringComparison.Ordinal))
            {
                continue;
            }

            if (selectionSet is not null)
            {
                throw new InvalidOperationException(
                    $"Authorization policy '{name}' has duplicate requirements in the operation plan.");
            }

            selectionSet = requirement.SelectionSet;
        }

        if (selectionSet is not null)
        {
            return selectionSet;
        }

        return null;
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
