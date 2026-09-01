using System.Collections.Immutable;
using System.Security.Cryptography;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents a GraphQL operation execution plan in Hot Chocolate Fusion, containing
/// the structured nodes and metadata required for distributed query execution.
/// </summary>
public sealed record OperationPlan : IOperationPlan
{
    private static readonly JsonOperationPlanFormatter s_formatter = new();
    private readonly ExecutionNode?[] _nodesById = [];
    private readonly Dictionary<Operation, int> _planPartByOperation;
    private readonly Dictionary<PolicyOccurrenceLocation, PolicyDenialLookupEntry[]> _policyDenials;
    private readonly Dictionary<PolicyOccurrenceLocation, PolicyDenialLookupEntry[]>
        _policySubtreeDenials;
    private readonly ImmutableArray<string> _requestPolicyNames;

    private OperationPlan(
        string id,
        Operation operation,
        ImmutableArray<ExecutionNode> rootNodes,
        ImmutableArray<ExecutionNode> allNodes,
        ImmutableArray<DeliveryGroup> deliveryGroups,
        ImmutableArray<IncrementalPlan> incrementalPlans,
        ImmutableArray<OperationIncludeCondition> includeConditions,
        ImmutableArray<PolicyConditionExpression> policyExpressions,
        ImmutableArray<PolicyConditionSlot> policySlots,
        ImmutableArray<PolicyPlanEntry> policies,
        int searchSpace,
        int expandedNodes)
    {
        Id = id;
        Operation = operation;
        RootNodes = rootNodes;
        AllNodes = allNodes;
        SearchSpace = searchSpace;
        ExpandedNodes = expandedNodes;
        DeliveryGroups = deliveryGroups;
        IncrementalPlans = incrementalPlans;
        IncludeConditions = includeConditions;
        PolicyExpressions = policyExpressions;
        PolicySlots = policySlots;
        Policies = policies;
        _nodesById = CreateNodeLookup(
            allNodes,
            out var usesDynamicSchemaNames,
            out var usesBatchNodes);
        MaxNodeId = _nodesById.Length > 0 ? _nodesById.Length - 1 : 0;
        UsesDynamicSchemaNames = usesDynamicSchemaNames;
        UsesBatchNodes = usesBatchNodes;
        _planPartByOperation = CreatePlanPartLookup(operation, incrementalPlans);
        _policyDenials = CreatePolicyDenialLookup(policySlots);
        _policySubtreeDenials = CreatePolicySubtreeDenialLookup(
            operation,
            incrementalPlans,
            _policyDenials);
        _requestPolicyNames = CreateRequestPolicyNames(policies);
    }

    /// <summary>
    /// Gets the unique identifier for this operation plan.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the GraphQL operation associated with this execution plan.
    /// </summary>
    public Operation Operation { get; }

    /// <summary>
    /// Gets the variable definitions from the operation.
    /// </summary>
    public IReadOnlyList<VariableDefinitionNode> VariableDefinitions
        => Operation.Definition.VariableDefinitions;

    /// <summary>
    /// Gets the name of the operation, or <c>null</c> if the operation is anonymous.
    /// </summary>
    public string? OperationName => Operation.Name;

    /// <summary>
    /// Gets the root execution nodes that serve as entry points for query execution.
    /// </summary>
    public ImmutableArray<ExecutionNode> RootNodes { get; }

    /// <summary>
    /// Gets all execution nodes in the plan, including both root and nested nodes.
    /// </summary>
    public ImmutableArray<ExecutionNode> AllNodes { get; }

    /// <summary>
    /// Gets a number specifying how many possible plans were considered during planning.
    /// </summary>
    public int SearchSpace { get; }

    /// <summary>
    /// Gets the number of nodes expanded (dequeued) during the A* search.
    /// </summary>
    public int ExpandedNodes { get; }

    /// <summary>
    /// Gets every <see cref="DeliveryGroup"/> (delivery group) this plan uses, in
    /// ascending <see cref="DeliveryGroup.Id"/> order. One element per <c>@defer</c>
    /// occurrence in the operation. Empty if the operation has no <c>@defer</c>
    /// directives.
    /// </summary>
    public ImmutableArray<DeliveryGroup> DeliveryGroups { get; }

    /// <summary>
    /// Gets the incremental plans for this plan. This is a flat collection;
    /// deferred fragment nesting is represented by <see cref="DeliveryGroup.Parent"/>.
    /// Each plan carries its delivery group set on
    /// <see cref="IncrementalPlan.DeliveryGroups"/>.
    /// Empty if the operation has no <c>@defer</c> directives.
    /// </summary>
    public ImmutableArray<IncrementalPlan> IncrementalPlans { get; }

    /// <summary>
    /// Gets the ordered client include-condition table shared by every operation in the plan.
    /// </summary>
    public ImmutableArray<OperationIncludeCondition> IncludeConditions { get; }

    /// <summary>
    /// Gets the plan-time boolean gates for policy-protected coordinates.
    /// </summary>
    public ImmutableArray<PolicyConditionSlot> PolicySlots { get; }

    /// <summary>
    /// Gets the canonical policy expressions referenced by <see cref="PolicySlots"/>.
    /// </summary>
    public ImmutableArray<PolicyConditionExpression> PolicyExpressions { get; }

    /// <summary>
    /// Gets every authorization policy requirement pair this plan references, whether reached through
    /// a request-constant <see cref="PolicySlots"/> condition or through a policy execution node target.
    /// Empty when the plan references no policy.
    /// </summary>
    public ImmutableArray<PolicyPlanEntry> Policies { get; }

    internal ImmutableArray<string> RequestPolicyNames => _requestPolicyNames;

    /// <summary>
    /// Gets the highest plan node identifier that can be resolved by this plan.
    /// </summary>
    public int MaxNodeId { get; }

    internal bool UsesDynamicSchemaNames { get; }

    internal bool UsesBatchNodes { get; }

    internal bool TryGetPlanPart(Operation operation, out int planPart)
        => _planPartByOperation.TryGetValue(operation, out planPart);

    internal ReadOnlySpan<PolicyDenialLookupEntry> GetPolicyDenials(
        int planPart,
        int selectionSetId,
        int selectionId)
        => _policyDenials.TryGetValue(
            new PolicyOccurrenceLocation(planPart, selectionSetId, selectionId),
            out var entries)
                ? entries
                : [];

    internal ReadOnlySpan<PolicyDenialLookupEntry> GetPolicySubtreeDenials(
        int planPart,
        int selectionSetId,
        int selectionId)
        => _policySubtreeDenials.TryGetValue(
            new PolicyOccurrenceLocation(planPart, selectionSetId, selectionId),
            out var entries)
                ? entries
                : [];

    private static Dictionary<Operation, int> CreatePlanPartLookup(
        Operation operation,
        ImmutableArray<IncrementalPlan> incrementalPlans)
    {
        var result = new Dictionary<Operation, int>(incrementalPlans.Length + 1)
        {
            [operation] = 0
        };
        for (var i = 0; i < incrementalPlans.Length; i++)
        {
            result.Add(incrementalPlans[i].Operation, i + 1);
        }

        return result;
    }

    private static ImmutableArray<string> CreateRequestPolicyNames(
        ImmutableArray<PolicyPlanEntry> policies)
    {
        var requestRequirementHash = PolicyPlanEntry.ComputeRequirementHash(null);
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var policy in policies)
        {
            if (policy.RequirementHash == requestRequirementHash)
            {
                names.Add(policy.PolicyName);
            }
        }

        return [.. names.Order(StringComparer.Ordinal)];
    }

    private static Dictionary<PolicyOccurrenceLocation, PolicyDenialLookupEntry[]>
        CreatePolicyDenialLookup(ImmutableArray<PolicyConditionSlot> slots)
    {
        var builders = new Dictionary<
            PolicyOccurrenceLocation,
            Dictionary<(int SlotOrdinal, int CoordinateOrdinal), PolicyDenialLookupEntry>>();

        foreach (var slot in slots)
        {
            for (var coordinateOrdinal = 0;
                coordinateOrdinal < slot.Coordinates.Length;
                coordinateOrdinal++)
            {
                var coordinate = slot.Coordinates[coordinateOrdinal];
                if (coordinate.IsRoot)
                {
                    continue;
                }

                foreach (var occurrence in coordinate.Occurrences)
                {
                    var location = new PolicyOccurrenceLocation(
                        occurrence.PlanPart,
                        occurrence.SelectionSetId,
                        occurrence.SelectionId);
                    if (!builders.TryGetValue(location, out var entries))
                    {
                        entries = [];
                        builders.Add(location, entries);
                    }

                    entries.TryAdd(
                        (slot.Ordinal, coordinateOrdinal),
                        new PolicyDenialLookupEntry(
                            slot.Ordinal,
                            coordinateOrdinal,
                            coordinate.LiveGuardMasks));
                }
            }
        }

        return CompletePolicyDenialLookup(builders);
    }

    private static Dictionary<PolicyOccurrenceLocation, PolicyDenialLookupEntry[]>
        CreatePolicySubtreeDenialLookup(
            Operation operation,
            ImmutableArray<IncrementalPlan> incrementalPlans,
            IReadOnlyDictionary<PolicyOccurrenceLocation, PolicyDenialLookupEntry[]> denials)
    {
        var builders = new Dictionary<
            PolicyOccurrenceLocation,
            Dictionary<(int SlotOrdinal, int CoordinateOrdinal), PolicyDenialLookupEntry>>();

        foreach (var (location, entries) in denials)
        {
            Add(location, entries);
            var compiledOperation = location.PlanPart == 0
                ? operation
                : incrementalPlans[location.PlanPart - 1].Operation;
            var declaringSelection = location.SelectionId < 0
                ? compiledOperation.GetSelectionSetById(location.SelectionSetId).DeclaringSelection
                : compiledOperation.GetSelectionById(location.SelectionId)
                    .DeclaringSelectionSet.DeclaringSelection;

            while (declaringSelection is not null)
            {
                Add(
                    new PolicyOccurrenceLocation(
                        location.PlanPart,
                        declaringSelection.DeclaringSelectionSet.Id,
                        declaringSelection.Id),
                    entries);
                declaringSelection = declaringSelection.DeclaringSelectionSet.DeclaringSelection;
            }
        }

        return CompletePolicyDenialLookup(builders);

        void Add(
            PolicyOccurrenceLocation location,
            ReadOnlySpan<PolicyDenialLookupEntry> entries)
        {
            if (!builders.TryGetValue(location, out var target))
            {
                target = [];
                builders.Add(location, target);
            }

            foreach (var entry in entries)
            {
                target.TryAdd((entry.SlotOrdinal, entry.CoordinateOrdinal), entry);
            }
        }
    }

    private static Dictionary<PolicyOccurrenceLocation, PolicyDenialLookupEntry[]>
        CompletePolicyDenialLookup(
            Dictionary<
                PolicyOccurrenceLocation,
                Dictionary<(int SlotOrdinal, int CoordinateOrdinal), PolicyDenialLookupEntry>> builders)
    {
        var result = new Dictionary<PolicyOccurrenceLocation, PolicyDenialLookupEntry[]>(builders.Count);
        foreach (var (location, entries) in builders)
        {
            var values = new PolicyDenialLookupEntry[entries.Count];
            entries.Values.CopyTo(values, 0);
            Array.Sort(
                values,
                static (left, right) =>
                {
                    var result = left.SlotOrdinal.CompareTo(right.SlotOrdinal);
                    return result != 0
                        ? result
                        : left.CoordinateOrdinal.CompareTo(right.CoordinateOrdinal);
                });
            result.Add(location, values);
        }

        return result;
    }

    /// <summary>
    /// Retrieves the execution node associated with a plan node identifier.
    /// </summary>
    /// <param name="id">
    /// The identifier of an execution node, or of an operation definition inside
    /// a batch.
    /// </param>
    /// <returns>The execution node associated with the specified identifier.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no node with the specified ID exists.</exception>
    public ExecutionNode GetNodeById(int id)
    {
        if ((uint)id < (uint)_nodesById.Length
            && _nodesById[id] is { } node)
        {
            return node;
        }

        throw ThrowHelper.NodeNotFound(id);
    }

    /// <summary>
    /// Returns the <see cref="ExecutionNode"/> responsible for executing the
    /// given plan node. If the plan node is already an execution node it is
    /// returned directly; if it is an operation definition inside a batch, the
    /// containing batch node is returned.
    /// </summary>
    public ExecutionNode GetExecutionNode(IOperationPlanNode planNode)
    {
        if (planNode is ExecutionNode executionNode)
        {
            return executionNode;
        }

        if ((uint)planNode.Id < (uint)_nodesById.Length
            && _nodesById[planNode.Id] is { } node)
        {
            return node;
        }

        throw ThrowHelper.NodeNotFound(planNode.Id);
    }

    /// <summary>
    /// Creates a new operation plan with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier for the operation plan.</param>
    /// <param name="operation">The GraphQL operation.</param>
    /// <param name="rootNodes">The root execution nodes.</param>
    /// <param name="allNodes">All execution nodes in the plan.</param>
    /// <param name="deliveryGroups">
    /// Every <see cref="DeliveryGroup"/> (delivery group) this plan uses, in ascending
    /// <see cref="DeliveryGroup.Id"/> order.
    /// </param>
    /// <param name="incrementalPlans">
    /// The incremental plans for <c>@defer</c> support. The collection is flat;
    /// deferred fragment nesting is represented by <see cref="DeliveryGroup.Parent"/>.
    /// Each plan carries its delivery group set on
    /// <see cref="IncrementalPlan.DeliveryGroups"/>.
    /// </param>
    /// <param name="includeConditions">The client include-condition table shared by every operation.</param>
    /// <param name="policySlots">
    /// The plan-time policy condition slots used by this plan.
    /// </param>
    /// <param name="policyExpressions">The canonical policy expressions used by the condition slots.</param>
    /// <param name="policies">Every policy name and requirement fingerprint referenced by the plan.</param>
    /// <param name="searchSpace">A number specifying how many possible plans were considered during planning.</param>
    /// <param name="expandedNodes">The number of expanded nodes during planner search.</param>
    /// <returns>A new <see cref="OperationPlan"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when node collections are invalid.</exception>
    public static OperationPlan Create(
        string id,
        Operation operation,
        ImmutableArray<ExecutionNode> rootNodes,
        ImmutableArray<ExecutionNode> allNodes,
        ImmutableArray<DeliveryGroup> deliveryGroups,
        ImmutableArray<IncrementalPlan> incrementalPlans,
        ImmutableArray<OperationIncludeCondition> includeConditions,
        ImmutableArray<PolicyConditionExpression> policyExpressions,
        ImmutableArray<PolicyConditionSlot> policySlots,
        ImmutableArray<PolicyPlanEntry> policies,
        int searchSpace,
        int expandedNodes)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentOutOfRangeException.ThrowIfLessThan(rootNodes.Length, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(allNodes.Length, 0);
        policySlots = PolicyArtifactBinder.Bind(
            operation,
            incrementalPlans,
            policyExpressions,
            policySlots,
            policies,
            allNodes);
        ValidateIncludeConditions(operation, incrementalPlans, includeConditions);
        ValidatePolicyArtifacts(operation, policyExpressions, policySlots, policies, allNodes, incrementalPlans);

        return new OperationPlan(
            id,
            operation,
            rootNodes,
            allNodes,
            deliveryGroups,
            incrementalPlans,
            includeConditions,
            policyExpressions,
            policySlots,
            policies,
            searchSpace,
            expandedNodes);
    }

    internal static OperationPlan CreateParsed(
        string id,
        Operation operation,
        ImmutableArray<ExecutionNode> rootNodes,
        ImmutableArray<ExecutionNode> allNodes,
        ImmutableArray<DeliveryGroup> deliveryGroups,
        ImmutableArray<IncrementalPlan> incrementalPlans,
        ImmutableArray<OperationIncludeCondition> includeConditions,
        ImmutableArray<PolicyConditionExpression> policyExpressions,
        ImmutableArray<PolicyConditionSlot> policySlots,
        ImmutableArray<PolicyPlanEntry> policies,
        int searchSpace,
        int expandedNodes)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(operation);
        ValidateIncludeConditions(operation, incrementalPlans, includeConditions);
        ValidatePolicyArtifacts(operation, policyExpressions, policySlots, policies, allNodes, incrementalPlans);
        PolicyArtifactBinder.Validate(
            operation,
            incrementalPlans,
            policyExpressions,
            policySlots,
            policies,
            allNodes);

        return new OperationPlan(
            id,
            operation,
            rootNodes,
            allNodes,
            deliveryGroups,
            incrementalPlans,
            includeConditions,
            policyExpressions,
            policySlots,
            policies,
            searchSpace,
            expandedNodes);
    }

    /// <summary>
    /// Creates a new operation plan with an identifier derived from the plan content.
    /// </summary>
    /// <param name="operation">The GraphQL operation.</param>
    /// <param name="rootNodes">The root execution nodes.</param>
    /// <param name="allNodes">All execution nodes in the plan.</param>
    /// <param name="deliveryGroups">
    /// Every <see cref="DeliveryGroup"/> (delivery group) this plan uses, in ascending
    /// <see cref="DeliveryGroup.Id"/> order.
    /// </param>
    /// <param name="incrementalPlans">
    /// The incremental plans for <c>@defer</c> support. The collection is flat;
    /// deferred fragment nesting is represented by <see cref="DeliveryGroup.Parent"/>.
    /// Each plan carries its delivery group set on
    /// <see cref="IncrementalPlan.DeliveryGroups"/>.
    /// </param>
    /// <param name="includeConditions">The client include-condition table shared by every operation.</param>
    /// <param name="policySlots">
    /// The plan-time policy condition slots used by this plan.
    /// </param>
    /// <param name="policyExpressions">The canonical policy expressions used by the condition slots.</param>
    /// <param name="policies">Every policy name and requirement fingerprint referenced by the plan.</param>
    /// <param name="searchSpace">A number specifying how many possible plans were considered during planning.</param>
    /// <param name="expandedNodes">The number of expanded nodes during planner search.</param>
    /// <returns>A new <see cref="OperationPlan"/> instance with a content-based identifier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when node collections are invalid.</exception>
    public static OperationPlan Create(
        Operation operation,
        ImmutableArray<ExecutionNode> rootNodes,
        ImmutableArray<ExecutionNode> allNodes,
        ImmutableArray<DeliveryGroup> deliveryGroups,
        ImmutableArray<IncrementalPlan> incrementalPlans,
        ImmutableArray<OperationIncludeCondition> includeConditions,
        ImmutableArray<PolicyConditionExpression> policyExpressions,
        ImmutableArray<PolicyConditionSlot> policySlots,
        ImmutableArray<PolicyPlanEntry> policies,
        int searchSpace,
        int expandedNodes)
        => Create(
            operation,
            rootNodes,
            allNodes,
            deliveryGroups,
            incrementalPlans,
            includeConditions,
            policyExpressions,
            policySlots,
            policies,
            PolicyArtifactBinder.CreatePolicySnapshot(operation),
            searchSpace,
            expandedNodes);

    internal static OperationPlan Create(
        Operation operation,
        ImmutableArray<ExecutionNode> rootNodes,
        ImmutableArray<ExecutionNode> allNodes,
        ImmutableArray<DeliveryGroup> deliveryGroups,
        ImmutableArray<IncrementalPlan> incrementalPlans,
        ImmutableArray<OperationIncludeCondition> includeConditions,
        ImmutableArray<PolicyConditionExpression> policyExpressions,
        ImmutableArray<PolicyConditionSlot> policySlots,
        ImmutableArray<PolicyPlanEntry> policies,
        PolicyArtifactPolicySnapshot policySnapshot,
        int searchSpace,
        int expandedNodes)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(policySnapshot);
        ArgumentOutOfRangeException.ThrowIfLessThan(rootNodes.Length, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(allNodes.Length, 0);
        policySlots = PolicyArtifactBinder.Bind(
            operation,
            incrementalPlans,
            policyExpressions,
            policySlots,
            policies,
            allNodes,
            policySnapshot);
        ValidateIncludeConditions(operation, incrementalPlans, includeConditions);
        ValidatePolicyArtifacts(operation, policyExpressions, policySlots, policies, allNodes, incrementalPlans);

        using var buffer = new PooledArrayWriter(initialBufferSize: 4096);
        s_formatter.Format(
            buffer,
            operation,
            allNodes,
            includeConditions,
            policyExpressions,
            policySlots,
            policies);

        // Generate a unique identifier for the operation plan by hashing its serialized form.
        // The hash is appended to the same buffer to reuse the already-allocated memory.
        var hashDestination = buffer.GetSpan(32);

        SHA256.HashData(buffer.WrittenSpan, hashDestination);
        buffer.Advance(32);

#if NET9_0_OR_GREATER
        var id = Convert.ToHexStringLower(buffer.WrittenSpan[^32..]);
#else
        var id = Convert.ToHexString(buffer.WrittenSpan[^32..]).ToLowerInvariant();
#endif

        return new OperationPlan(
            id,
            operation,
            rootNodes,
            allNodes,
            deliveryGroups,
            incrementalPlans,
            includeConditions,
            policyExpressions,
            policySlots,
            policies,
            searchSpace,
            expandedNodes);
    }

    private static void ValidateIncludeConditions(
        Operation operation,
        ImmutableArray<IncrementalPlan> incrementalPlans,
        ImmutableArray<OperationIncludeCondition> includeConditions)
    {
        if (includeConditions.IsDefault || includeConditions.Length > 64)
        {
            throw ThrowHelper.InvalidOperationPlan(
                "The operation-wide include-condition table is invalid.");
        }

        var expected = IncludeConditionCollection.Create(includeConditions);
        if (expected.Count != includeConditions.Length)
        {
            throw ThrowHelper.InvalidOperationPlan(
                "The operation-wide include-condition table must contain unique entries.");
        }

        for (var i = 1; i < includeConditions.Length; i++)
        {
            var previous = includeConditions[i - 1];
            var current = includeConditions[i];
            var comparison = StringComparer.Ordinal.Compare(
                previous.SkipVariable,
                current.SkipVariable);
            if (comparison > 0
                || (comparison == 0
                    && StringComparer.Ordinal.Compare(
                        previous.IncludeVariable,
                        current.IncludeVariable) >= 0))
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "The operation-wide include-condition table must be in canonical order.");
            }
        }

        var witnessed = new bool[includeConditions.Length];
        ValidateOperation(operation);
        foreach (var incrementalPlan in incrementalPlans)
        {
            ValidateOperation(incrementalPlan.Operation);
        }

        if (witnessed.Any(static value => !value))
        {
            throw ThrowHelper.InvalidOperationPlan(
                "Every operation-wide include condition must be used by a compiled operation.");
        }

        void ValidateOperation(Operation current)
        {
            if (current.IncludeConditionCount != includeConditions.Length)
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "Every compiled operation must use the operation-wide include-condition table.");
            }

            for (var i = 0; i < includeConditions.Length; i++)
            {
                var actual = current.IncludeConditions[i];
                var serialized = includeConditions[i];
                if (!string.Equals(actual.Skip, serialized.SkipVariable, StringComparison.Ordinal)
                    || !string.Equals(actual.Include, serialized.IncludeVariable, StringComparison.Ordinal))
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "A compiled operation uses a different include-condition table.");
                }
            }

            var found = CreateWitnessedConditions(current.Definition);
            for (var i = 0; i < includeConditions.Length; i++)
            {
                if (found.Contains(current.IncludeConditions[i]))
                {
                    witnessed[i] = true;
                }
            }
        }

        static HashSet<IncludeCondition> CreateWitnessedConditions(OperationDefinitionNode definition)
        {
            var conditions = OperationCompiler.CreateIncludeConditionCollection(definition);
            return [.. conditions];
        }
    }

    private static void ValidatePolicyArtifacts(
        Operation operation,
        ImmutableArray<PolicyConditionExpression> expressions,
        ImmutableArray<PolicyConditionSlot> slots,
        ImmutableArray<PolicyPlanEntry> policies,
        ImmutableArray<ExecutionNode> allNodes,
        ImmutableArray<IncrementalPlan> incrementalPlans)
    {
        if (slots.Length > 64)
        {
            throw ThrowHelper.InvalidOperationPlan("An operation plan cannot contain more than 64 policy gates.");
        }

        var expressionNames = new HashSet<string>(StringComparer.Ordinal);
        var expressionKeys = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < expressions.Length; i++)
        {
            var expression = expressions[i];

            if (expression.Ordinal != i)
            {
                throw ThrowHelper.InvalidOperationPlan("Policy expression ordinals must be contiguous and zero-based.");
            }

            if (expression.Groups.IsDefaultOrEmpty)
            {
                throw ThrowHelper.InvalidOperationPlan("A policy expression must contain at least one policy name group.");
            }

            foreach (var group in expression.Groups)
            {
                if (group.IsDefaultOrEmpty)
                {
                    throw ThrowHelper.InvalidOperationPlan("A policy expression group must contain at least one policy name.");
                }

                foreach (var name in group)
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        throw ThrowHelper.InvalidOperationPlan("A policy expression name cannot be empty.");
                    }

                    expressionNames.Add(name);
                }
            }

            var canonical = PolicyNameGroups.Canonicalize(expression.Groups);
            if (!PolicyGroupsEqual(expression.Groups, canonical))
            {
                throw ThrowHelper.InvalidOperationPlan("Policy expression groups must be canonical.");
            }

            if (!expressionKeys.Add(PolicyNameGroups.CreateCanonicalKey(expression.Groups)))
            {
                throw ThrowHelper.InvalidOperationPlan("A canonical policy expression can occur only once.");
            }
        }

        var gateIdentities = new HashSet<string>(StringComparer.Ordinal);
        var referencedExpressions = new bool[expressions.Length];
        var includeConditionMask = operation.IncludeConditionCount == 64
            ? ulong.MaxValue
            : (1UL << operation.IncludeConditionCount) - 1;

        for (var i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];

            if (slot.Ordinal != i)
            {
                throw ThrowHelper.InvalidOperationPlan("Policy gate ordinals must be contiguous and zero-based.");
            }

            if (slot.Applications.IsDefaultOrEmpty)
            {
                throw ThrowHelper.InvalidOperationPlan("A policy gate must reference at least one policy expression.");
            }

            if (!Enum.IsDefined(slot.Rmax))
            {
                throw ThrowHelper.InvalidOperationPlan("A policy gate contains an undefined residual denial behavior.");
            }

            if (slot.GuardMasks.IsDefaultOrEmpty)
            {
                throw ThrowHelper.InvalidOperationPlan("A policy gate must contain at least one liveness guard mask.");
            }

            if (slot.Coordinates.IsDefaultOrEmpty)
            {
                throw ThrowHelper.InvalidOperationPlan("A policy gate must control at least one coordinate.");
            }

            if (!AreCanonicalGuardMasks(slot.GuardMasks, allowEmpty: false)
                || slot.GuardMasks.Any(mask => (mask & ~includeConditionMask) != 0))
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "Policy gate guard masks must be canonical and reference defined include conditions.");
            }

            var coordinateKeys = new HashSet<(string TypeName, string? FieldName, bool IsRoot)>();
            var slotApplicationSet = slot.Applications.ToHashSet();
            var coordinateLiveMasks = ImmutableArray.CreateBuilder<ulong>();
            foreach (var coordinate in slot.Coordinates)
            {
                if (string.IsNullOrWhiteSpace(coordinate.TypeName)
                    || (coordinate.FieldName is not null
                        && string.IsNullOrWhiteSpace(coordinate.FieldName))
                    || (coordinate.IsRoot && coordinate.FieldName is not null)
                    || coordinate.ResponseNames.IsDefault
                    || (coordinate.FieldName is null && !coordinate.ResponseNames.IsEmpty)
                    || (coordinate.FieldName is not null && coordinate.ResponseNames.IsDefaultOrEmpty)
                    || coordinate.Applications.IsDefaultOrEmpty
                    || coordinate.LiveGuardMasks.IsDefaultOrEmpty
                    || coordinate.GateGuardMasks.IsDefault
                    || !coordinateKeys.Add((
                        coordinate.TypeName,
                        coordinate.FieldName,
                        coordinate.IsRoot)))
                {
                    throw ThrowHelper.InvalidOperationPlan("A policy gate coordinate is malformed.");
                }

                string? previousResponseName = null;
                foreach (var responseName in coordinate.ResponseNames)
                {
                    if (string.IsNullOrWhiteSpace(responseName)
                        || (previousResponseName is not null
                            && StringComparer.Ordinal.Compare(previousResponseName, responseName) >= 0))
                    {
                        throw ThrowHelper.InvalidOperationPlan(
                            "Policy gate coordinate response names must be nonempty, unique, and canonical.");
                    }

                    previousResponseName = responseName;
                }

                var coordinateApplicationSet = new HashSet<PolicyConditionApplication>();
                foreach (var application in coordinate.Applications)
                {
                    if ((uint)application.ExpressionOrdinal >= (uint)expressions.Length
                        || !Enum.IsDefined(application.OnDenied)
                        || !slotApplicationSet.Contains(application))
                    {
                        throw ThrowHelper.InvalidOperationPlan(
                            "A policy gate coordinate contains an invalid expression reference.");
                    }

                    if (!coordinateApplicationSet.Add(application))
                    {
                        throw ThrowHelper.InvalidOperationPlan(
                            "A policy gate coordinate contains a duplicate expression reference.");
                    }
                }

                if (!coordinateApplicationSet.SetEquals(slotApplicationSet))
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "A policy gate coordinate must reference exactly the gate applications.");
                }

                if (!AreCanonicalGuardMasks(coordinate.LiveGuardMasks, allowEmpty: false)
                    || coordinate.LiveGuardMasks.Any(mask => (mask & ~includeConditionMask) != 0))
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "Policy gate coordinate live guard masks must be canonical and reference defined include conditions.");
                }

                if (!AreCanonicalGuardMasks(coordinate.GateGuardMasks, allowEmpty: true)
                    || coordinate.GateGuardMasks.Any(mask => (mask & ~includeConditionMask) != 0))
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "Policy gate coordinate fetch-gate guard masks must be canonical and reference defined include conditions.");
                }

                foreach (var gateMask in coordinate.GateGuardMasks)
                {
                    if (!IsMaskCoveredBy(gateMask, coordinate.LiveGuardMasks))
                    {
                        throw ThrowHelper.InvalidOperationPlan(
                            "Policy gate coordinate fetch-gate guard masks must be covered by its live guard masks.");
                    }
                }

                coordinateLiveMasks.AddRange(coordinate.LiveGuardMasks);
            }

            var expectedGuardMasks = CanonicalizeGuardMasks(coordinateLiveMasks.ToImmutable());
            if (!slot.GuardMasks.SequenceEqual(expectedGuardMasks))
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "Policy gate guard masks must exactly match the coordinate guard masks.");
            }

            var identity = new System.Text.StringBuilder();
            identity.Append((int)slot.Rmax);
            var references = new HashSet<(int Ordinal, PolicyDenialBehavior OnDenied)>();
            var previousExpressionOrdinal = -1;
            var previousOnDenied = default(PolicyDenialBehavior);

            foreach (var application in slot.Applications)
            {
                if ((uint)application.ExpressionOrdinal >= (uint)expressions.Length)
                {
                    throw ThrowHelper.InvalidOperationPlan("A policy gate references an undefined policy expression.");
                }

                if (!Enum.IsDefined(application.OnDenied))
                {
                    throw ThrowHelper.InvalidOperationPlan("A policy gate contains an undefined denial behavior.");
                }

                if (!references.Add((application.ExpressionOrdinal, application.OnDenied)))
                {
                    throw ThrowHelper.InvalidOperationPlan("A policy gate contains a duplicate expression reference.");
                }

                if (application.ExpressionOrdinal < previousExpressionOrdinal
                    || (application.ExpressionOrdinal == previousExpressionOrdinal
                        && application.OnDenied <= previousOnDenied))
                {
                    throw ThrowHelper.InvalidOperationPlan("Policy gate expression references must be canonical.");
                }

                previousExpressionOrdinal = application.ExpressionOrdinal;
                previousOnDenied = application.OnDenied;
                referencedExpressions[application.ExpressionOrdinal] = true;

                identity.Append('|');
                identity.Append(application.ExpressionOrdinal);
                identity.Append(':');
                identity.Append((int)application.OnDenied);
            }

            if (!gateIdentities.Add(identity.ToString()))
            {
                throw ThrowHelper.InvalidOperationPlan("An operation plan contains duplicate policy gate identities.");
            }
        }

        static bool AreCanonicalGuardMasks(ImmutableArray<ulong> masks, bool allowEmpty)
        {
            if (masks.IsDefault || (masks.IsEmpty && !allowEmpty))
            {
                return false;
            }

            if (masks.IsEmpty)
            {
                return true;
            }

            if (masks[0] == 0)
            {
                return masks.Length == 1;
            }

            for (var i = 0; i < masks.Length; i++)
            {
                if (i > 0 && masks[i] <= masks[i - 1])
                {
                    return false;
                }

                for (var j = 0; j < masks.Length; j++)
                {
                    if (i != j && (masks[i] & masks[j]) == masks[j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        static bool IsMaskCoveredBy(ulong mask, ImmutableArray<ulong> coveringMasks)
            => coveringMasks.Any(coveringMask => (mask & coveringMask) == coveringMask);

        static ImmutableArray<ulong> CanonicalizeGuardMasks(ImmutableArray<ulong> masks)
        {
            if (masks.Contains(0))
            {
                return [0];
            }

            var canonical = ImmutableArray.CreateBuilder<ulong>();
            foreach (var mask in masks.Distinct().Order())
            {
                if (!canonical.Any(existing => (mask & existing) == existing))
                {
                    canonical.Add(mask);
                }
            }

            return canonical.ToImmutable();
        }

        if (referencedExpressions.Any(referenced => !referenced))
        {
            throw ThrowHelper.InvalidOperationPlan("Every policy expression must be referenced by a policy gate.");
        }

        var actualInventory = new HashSet<(string Name, ulong Hash)>();
        string? previousPolicyName = null;
        var previousPolicyHash = 0UL;
        foreach (var policy in policies)
        {
            if (string.IsNullOrWhiteSpace(policy.PolicyName) || policy.RequirementHash == 0)
            {
                throw ThrowHelper.InvalidOperationPlan("A policy inventory entry must contain a name and a nonzero requirement fingerprint.");
            }

            if (!actualInventory.Add((policy.PolicyName, policy.RequirementHash)))
            {
                throw ThrowHelper.InvalidOperationPlan("A policy inventory entry must be unique by name and requirement fingerprint.");
            }

            if (previousPolicyName is not null
                && (StringComparer.Ordinal.Compare(previousPolicyName, policy.PolicyName) > 0
                    || (previousPolicyName.Equals(policy.PolicyName, StringComparison.Ordinal)
                        && previousPolicyHash >= policy.RequirementHash)))
            {
                throw ThrowHelper.InvalidOperationPlan("Policy inventory entries must be canonical.");
            }

            previousPolicyName = policy.PolicyName;
            previousPolicyHash = policy.RequirementHash;
        }

        var expectedInventory = new HashSet<(string Name, ulong Hash)>();
        AddTargetInventory(allNodes, expectedInventory);
        foreach (var incrementalPlan in incrementalPlans)
        {
            AddTargetInventory(incrementalPlan.AllNodes, expectedInventory);
        }

        foreach (var expressionName in expressionNames)
        {
            if (!expectedInventory.Any(entry => entry.Name.Equals(expressionName, StringComparison.Ordinal)))
            {
                expectedInventory.Add((expressionName, PolicyPlanEntry.ComputeRequirementHash(null)));
            }
        }

        if (!actualInventory.SetEquals(expectedInventory))
        {
            throw ThrowHelper.InvalidOperationPlan("The policy inventory must exactly cover every policy artifact in the operation plan.");
        }

        static void AddTargetInventory(
            ImmutableArray<ExecutionNode> nodes,
            HashSet<(string Name, ulong Hash)> inventory)
        {
            foreach (var policyNode in nodes.OfType<PolicyExecutionNode>())
            {
                if (policyNode.Targets.IsEmpty)
                {
                    throw ThrowHelper.InvalidOperationPlan("A policy execution node must contain at least one target.");
                }

                foreach (var target in policyNode.Targets)
                {
                    if (!Enum.IsDefined(target.Kind)
                        || string.IsNullOrWhiteSpace(target.TypeName)
                        || target.Policies.Length == 0)
                    {
                        throw ThrowHelper.InvalidOperationPlan("A policy target is malformed.");
                    }

                    var targetPolicyNames = new HashSet<string>(StringComparer.Ordinal);
                    var targetApplications = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var application in target.Policies)
                    {
                        if (!Enum.IsDefined(application.OnDenied)
                            || application.Groups.IsDefaultOrEmpty
                            || !PolicyGroupsEqual(
                                application.Groups,
                                PolicyNameGroups.Canonicalize(application.Groups)))
                        {
                            throw ThrowHelper.InvalidOperationPlan("A policy target application is malformed.");
                        }

                        var applicationKey = string.Concat(
                            ((int)application.OnDenied).ToString(
                                System.Globalization.CultureInfo.InvariantCulture),
                            ":",
                            PolicyNameGroups.CreateCanonicalKey(application.Groups));
                        if (!targetApplications.Add(applicationKey))
                        {
                            throw ThrowHelper.InvalidOperationPlan(
                                "A policy target cannot contain duplicate policy applications.");
                        }

                        foreach (var group in application.Groups)
                        {
                            if (group.IsDefaultOrEmpty)
                            {
                                throw ThrowHelper.InvalidOperationPlan("A policy target application group cannot be empty.");
                            }

                            foreach (var name in group)
                            {
                                if (string.IsNullOrWhiteSpace(name))
                                {
                                    throw ThrowHelper.InvalidOperationPlan("A policy target application name cannot be empty.");
                                }

                                targetPolicyNames.Add(name);
                            }
                        }
                    }

                    var namesWithRequirements = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var requirement in target.Requirements)
                    {
                        if (string.IsNullOrWhiteSpace(requirement.PolicyName)
                            || requirement.SelectionSet is null
                            || !targetPolicyNames.Contains(requirement.PolicyName))
                        {
                            throw ThrowHelper.InvalidOperationPlan("A policy target requirement is malformed.");
                        }

                        namesWithRequirements.Add(requirement.PolicyName);
                        inventory.Add((
                            requirement.PolicyName,
                            PolicyPlanEntry.ComputeRequirementHash(requirement.SelectionSet)));
                    }

                    foreach (var application in target.Policies)
                    {
                        foreach (var group in application.Groups)
                        {
                            foreach (var name in group)
                            {
                                if (!namesWithRequirements.Contains(name))
                                {
                                    inventory.Add((name, PolicyPlanEntry.ComputeRequirementHash(null)));
                                }
                            }
                        }
                    }
                }
            }
        }

        static bool PolicyGroupsEqual(
            ImmutableArray<ImmutableArray<string>> left,
            ImmutableArray<ImmutableArray<string>> right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (var i = 0; i < left.Length; i++)
            {
                if (!left[i].SequenceEqual(right[i], StringComparer.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }

    private static ExecutionNode?[] CreateNodeLookup(
        ImmutableArray<ExecutionNode> allNodes,
        out bool usesDynamicSchemaNames,
        out bool usesBatchNodes)
    {
        usesDynamicSchemaNames = false;
        usesBatchNodes = false;

        if (allNodes.IsDefaultOrEmpty)
        {
            return [];
        }

        var maxId = 0;

        foreach (var node in allNodes)
        {
            maxId = Math.Max(maxId, node.Id);

            switch (node.Type)
            {
                case ExecutionNodeType.Node:
                    usesDynamicSchemaNames = true;
                    break;

                case ExecutionNodeType.OperationBatch:
                    usesBatchNodes = true;
                    break;
            }

            if (node is OperationBatchExecutionNode batchNode)
            {
                foreach (var op in batchNode.Operations)
                {
                    maxId = Math.Max(maxId, op.Id);
                }
            }

            if (node is ApolloOperationBatchExecutionNode apolloBatchNode)
            {
                foreach (var op in apolloBatchNode.Operations)
                {
                    maxId = Math.Max(maxId, op.Id);
                }
            }
        }

        var nodesById = new ExecutionNode?[maxId + 1];

        foreach (var node in allNodes)
        {
            nodesById[node.Id] = node;

            // Map each operation definition ID to the containing batch node,
            // so GetNodeById can resolve definition IDs to execution nodes.
            if (node is OperationBatchExecutionNode batchNode)
            {
                foreach (var op in batchNode.Operations)
                {
                    nodesById[op.Id] = batchNode;
                }
            }

            if (node is ApolloOperationBatchExecutionNode apolloBatchNode)
            {
                foreach (var op in apolloBatchNode.Operations)
                {
                    nodesById[op.Id] = apolloBatchNode;
                }
            }
        }

        return nodesById;
    }
}

internal readonly record struct PolicyOccurrenceLocation(
    int PlanPart,
    int SelectionSetId,
    int SelectionId);

internal readonly record struct PolicyDenialLookupEntry(
    int SlotOrdinal,
    int CoordinateOrdinal,
    ImmutableArray<ulong> LiveGuardMasks);
