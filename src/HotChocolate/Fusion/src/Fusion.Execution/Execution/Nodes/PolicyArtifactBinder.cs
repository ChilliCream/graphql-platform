using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

internal static class PolicyArtifactBinder
{
    internal static bool TryFindNestedParentAuthorityGap(
        ImmutableArray<IncrementalPlan> incrementalPlans,
        ImmutableArray<ExecutionNode> rootNodes,
        out string coordinate,
        out string scope)
    {
        foreach (var incrementalPlan in incrementalPlans)
        {
            var operationOwners = CreateOperationOwners(incrementalPlan.AllNodes);
            var hasParentScope = TryResolveParentPlanScope(
                incrementalPlan,
                incrementalPlans,
                rootNodes,
                out var parentScope,
                out _);

            foreach (var policyNode in incrementalPlan.AllNodes.OfType<PolicyExecutionNode>())
            {
                PolicyExecutionTarget? failingTarget = null;
                if (hasParentScope
                    && TryFindParentRequirementProviders(
                        policyNode.Targets,
                        operationOwners,
                        parentScope,
                        out var expectedParentDependencies,
                        out failingTarget,
                        out _))
                {
                    var actualParentDependencies = policyNode.ParentDependencies.ToArray();
                    if (actualParentDependencies.SequenceEqual(expectedParentDependencies.Dependencies))
                    {
                        continue;
                    }

                    failingTarget = FindTargetForMissingParentDependency(
                        expectedParentDependencies,
                        actualParentDependencies)
                        ?? policyNode.Targets[0];
                }

                var target = failingTarget ?? policyNode.Targets[0];
                coordinate = target.Kind is PolicyTargetKind.Field
                    ? $"{target.TypeName}.{target.Path.Name}"
                    : target.TypeName;
                scope = GetDeferredScope(incrementalPlan);
                return true;
            }
        }

        coordinate = string.Empty;
        scope = string.Empty;
        return false;
    }

    internal static ImmutableArray<PolicyConditionSlot> Bind(
        Operation operation,
        ImmutableArray<IncrementalPlan> incrementalPlans,
        ImmutableArray<PolicyConditionExpression> expressions,
        ImmutableArray<PolicyConditionSlot> slots,
        ImmutableArray<PolicyPlanEntry> policies,
        ImmutableArray<ExecutionNode> allNodes)
        => Bind(
            operation,
            incrementalPlans,
            expressions,
            slots,
            policies,
            allNodes,
            CreatePolicySnapshot(operation));

    internal static ImmutableArray<PolicyConditionSlot> Bind(
        Operation operation,
        ImmutableArray<IncrementalPlan> incrementalPlans,
        ImmutableArray<PolicyConditionExpression> expressions,
        ImmutableArray<PolicyConditionSlot> slots,
        ImmutableArray<PolicyPlanEntry> policies,
        ImmutableArray<ExecutionNode> allNodes,
        PolicyArtifactPolicySnapshot policySnapshot)
    {
        var candidates = CreateCandidates(
            operation,
            incrementalPlans,
            policies,
            policySnapshot);
        var expected = ReconstructArtifacts(candidates);
        ValidateArtifactTables(expressions, slots, expected, allowUnboundOccurrences: true);
        var boundSlots = expected.Slots;
        BindTargets(allNodes, planPart: 0, operation, boundSlots, candidates);

        for (var i = 0; i < incrementalPlans.Length; i++)
        {
            BindTargets(
                incrementalPlans[i].AllNodes,
                i + 1,
                incrementalPlans[i].Operation,
                boundSlots,
                candidates);
        }

        Validate(
            operation,
            incrementalPlans,
            expressions,
            boundSlots,
            policies,
            allNodes,
            policySnapshot);
        return boundSlots;
    }

    internal static void Validate(
        Operation operation,
        ImmutableArray<IncrementalPlan> incrementalPlans,
        ImmutableArray<PolicyConditionExpression> expressions,
        ImmutableArray<PolicyConditionSlot> slots,
        ImmutableArray<PolicyPlanEntry> policies,
        ImmutableArray<ExecutionNode> allNodes)
        => Validate(
            operation,
            incrementalPlans,
            expressions,
            slots,
            policies,
            allNodes,
            CreatePolicySnapshot(operation));

    private static void Validate(
        Operation operation,
        ImmutableArray<IncrementalPlan> incrementalPlans,
        ImmutableArray<PolicyConditionExpression> expressions,
        ImmutableArray<PolicyConditionSlot> slots,
        ImmutableArray<PolicyPlanEntry> policies,
        ImmutableArray<ExecutionNode> allNodes,
        PolicyArtifactPolicySnapshot policySnapshot)
    {
        var candidates = CreateCandidates(
            operation,
            incrementalPlans,
            policies,
            policySnapshot);
        var expected = ReconstructArtifacts(candidates);
        var candidateByReference = candidates.ToDictionary(candidate => candidate.Reference);
        var claimed = new HashSet<PolicyOccurrenceReference>();

        foreach (var slot in slots)
        {
            (PolicyOccurrenceReference Occurrence, PolicyConditionCoordinate Coordinate)?
                previousCoordinate = null;

            foreach (var coordinate in slot.Coordinates)
            {
                if (coordinate.Occurrences.IsDefaultOrEmpty)
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "A policy gate coordinate must reference at least one compiled occurrence.");
                }

                foreach (var occurrence in coordinate.Occurrences)
                {
                    if (!candidateByReference.TryGetValue(occurrence, out var candidate)
                        || !MatchesCoordinate(coordinate, expressions, candidate))
                    {
                        throw ThrowHelper.InvalidOperationPlan(
                            "A policy gate coordinate does not match its compiled occurrence.");
                    }

                    if (!claimed.Add(occurrence))
                    {
                        throw ThrowHelper.InvalidOperationPlan(
                            "A compiled policy occurrence cannot be claimed by more than one gate coordinate.");
                    }
                }

                var expectedOccurrences = candidates
                    .Where(candidate => MatchesCoordinate(coordinate, expressions, candidate))
                    .Select(candidate => candidate.Reference)
                    .OrderBy(reference => reference.PlanPart)
                    .ThenBy(reference => reference.SelectionSetId)
                    .ThenBy(reference => reference.SelectionId)
                    .ThenBy(reference => reference.OccurrenceOrdinal)
                    .ThenBy(reference => reference.ApplicationOrdinal)
                    .ThenBy(reference => reference.Facet)
                    .ToImmutableArray();
                if (!coordinate.Occurrences.SequenceEqual(expectedOccurrences))
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "A policy gate coordinate must claim exactly its compiled occurrences.");
                }

                var matchingCandidates = expectedOccurrences
                    .Select(reference => candidateByReference[reference])
                    .ToArray();
                var expectedApplications = CreateExpectedApplications(
                    matchingCandidates,
                    expressions);
                if (!coordinate.Applications.SequenceEqual(expectedApplications))
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "A policy gate coordinate applications do not match declaration order.");
                }

                var firstOccurrence = expectedOccurrences[0];
                if (previousCoordinate is { } previous
                    && CompareCoordinates(
                        previous.Occurrence,
                        previous.Coordinate,
                        firstOccurrence,
                        coordinate) >= 0)
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "Policy gate coordinates must follow compiled occurrence order.");
                }

                previousCoordinate = (firstOccurrence, coordinate);
                var expectedLiveMasks = CanonicalizeMasks(
                    matchingCandidates.Select(candidate => candidate.GuardMask));
                if (!coordinate.LiveGuardMasks.SequenceEqual(expectedLiveMasks))
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "A policy gate coordinate liveness masks do not match its compiled occurrences.");
                }

                var expectedResponseNames = matchingCandidates
                    .Select(candidate => candidate.ResponseName)
                    .Where(responseName => responseName is not null)
                    .Cast<string>()
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .ToImmutableArray();
                if (!coordinate.ResponseNames.SequenceEqual(expectedResponseNames))
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "A policy gate coordinate response names do not match its compiled occurrences.");
                }

                var expectedRmax = candidates
                    .Where(candidate =>
                        candidate.Reference.Facet is PolicyOccurrenceFacet.ResidualEvaluation
                        && MatchesCoordinatePosition(coordinate, candidate))
                    .Select(candidate => candidate.Policy.OnDenied)
                    .DefaultIfEmpty(PolicyDenialBehavior.Null)
                    .Max();
                if (slot.Rmax != expectedRmax)
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "A policy gate residual denial behavior does not match its compiled applications.");
                }

                var gateCandidates = candidates
                    .Where(candidate =>
                        candidate.Reference.Facet is PolicyOccurrenceFacet.SlotGate
                        && MatchesCoordinatePosition(coordinate, candidate))
                    .ToArray();
                var expectedGateMasks = CanonicalizeMasks(gateCandidates
                    .Where(candidate => candidate.GateEligible)
                    .Select(candidate => candidate.GuardMask));
                if (!coordinate.GateGuardMasks.SequenceEqual(expectedGateMasks))
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "A policy gate coordinate fetch-gate masks do not match its compiled operations.");
                }

                foreach (var gateCandidate in gateCandidates.Where(candidate =>
                    candidate.RequiresFetchGateWitness))
                {
                    var realized = IsFetchGated(
                        gateCandidate,
                        slot.VariableName,
                        operation,
                        allNodes,
                        incrementalPlans);
                    if (!realized)
                    {
                        throw ThrowHelper.InvalidOperationPlan(
                            "A compiled policy occurrence fetch-gate realization does not match its required facet.");
                    }
                }
            }
        }

        ValidateTargets(
            allNodes,
            planPart: 0,
            operation,
            slots,
            candidateByReference,
            claimed);
        ValidatePolicyTopology(
            allNodes,
            parentScope: ParentPlanScope.Empty,
            planPart: 0,
            candidateByReference);
        for (var i = 0; i < incrementalPlans.Length; i++)
        {
            ValidateTargets(
                incrementalPlans[i].AllNodes,
                i + 1,
                incrementalPlans[i].Operation,
                slots,
                candidateByReference,
                claimed);
            ValidatePolicyTopology(
                incrementalPlans[i].AllNodes,
                parentScope: ResolveParentPlanScope(incrementalPlans[i], incrementalPlans, allNodes),
                i + 1,
                candidateByReference);
        }

        if (!claimed.SetEquals(candidateByReference.Keys))
        {
            throw ThrowHelper.InvalidOperationPlan(
                "Every required compiled policy occurrence facet must be claimed exactly once.");
        }

        ValidateArtifactTables(expressions, slots, expected, allowUnboundOccurrences: false);
    }

    private static ParentPlanScope ResolveParentPlanScope(
        IncrementalPlan incrementalPlan,
        ImmutableArray<IncrementalPlan> incrementalPlans,
        ImmutableArray<ExecutionNode> rootNodes)
    {
        if (!TryResolveParentPlanScope(
                incrementalPlan,
                incrementalPlans,
                rootNodes,
                out var parentScope,
                out var error))
        {
            throw ThrowHelper.InvalidOperationPlan(error!);
        }

        return parentScope;
    }

    private static bool TryResolveParentPlanScope(
        IncrementalPlan incrementalPlan,
        ImmutableArray<IncrementalPlan> incrementalPlans,
        ImmutableArray<ExecutionNode> rootNodes,
        out ParentPlanScope parentScope,
        out string? error)
    {
        var parentGroupIds = incrementalPlan.DeliveryGroups
            .Where(deliveryGroup => deliveryGroup.Parent is not null)
            .Select(deliveryGroup => deliveryGroup.Parent!.Id)
            .Distinct()
            .ToArray();

        if (parentGroupIds.Length == 0)
        {
            parentScope = new ParentPlanScope(
                [new ParentPlanPiece(0, rootNodes)],
                HasImmediateParentScope: true);
            error = null;
            return true;
        }

        if (parentGroupIds.Length != 1)
        {
            parentScope = ParentPlanScope.Empty;
            error = "An incremental plan must have one unambiguous immediate parent delivery group.";
            return false;
        }

        var parentGroupId = parentGroupIds[0];
        var candidatePlans = incrementalPlans
            .Where(candidate => !ReferenceEquals(candidate, incrementalPlan)
                && candidate.DeliveryGroups.Any(group => group.Id == parentGroupId))
            .ToArray();

        if (candidatePlans.Length == 0)
        {
            parentScope = ParentPlanScope.Empty;
            error = "A non-root delivery group must have a matching immediate parent plan scope.";
            return false;
        }

        parentScope = new ParentPlanScope(
            [.. candidatePlans.Select((candidate, index) => new ParentPlanPiece(
                index,
                candidate.AllNodes))],
            HasImmediateParentScope: true);
        error = null;
        return true;
    }

    private static string GetDeferredScope(IncrementalPlan incrementalPlan)
    {
        var deliveryGroup = incrementalPlan.DeliveryGroups
            .FirstOrDefault(group => group.Parent is not null)
            ?? incrementalPlan.DeliveryGroups.FirstOrDefault();
        return deliveryGroup?.Label
            ?? deliveryGroup?.Path?.ToString()
            ?? "nested defer";
    }

    private static void ValidatePolicyTopology(
        ImmutableArray<ExecutionNode> nodes,
        ParentPlanScope parentScope,
        int planPart,
        IReadOnlyDictionary<PolicyOccurrenceReference, Candidate> candidates)
    {
        var operationOwners = CreateOperationOwners(nodes);
        var claimedProducerIds = new HashSet<int>();
        PolicyOccurrenceReference? previousPolicyNodeOccurrence = null;

        foreach (var policyNode in nodes.OfType<PolicyExecutionNode>())
        {
            var targets = policyNode.Targets.ToArray();
            var targetCandidates = targets
                .SelectMany(target => target.Occurrences)
                .Where(reference => reference.PlanPart == planPart)
                .Select(reference => candidates[reference])
                .ToArray();
            var firstOccurrence = targets[0].Occurrences[0];
            if (previousPolicyNodeOccurrence is { } previousOccurrence
                && CompareOccurrencePosition(previousOccurrence, firstOccurrence) >= 0)
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "Policy execution nodes must follow compiled occurrence order.");
            }

            previousPolicyNodeOccurrence = firstOccurrence;
            var expectedProviders = FindRequirementProviders(targets, operationOwners);
            var producerOwners = operationOwners
                .Select(owner => new
                {
                    OwnerId = owner.Key,
                    Artifacts = owner.Value,
                    TargetDepth = owner.Value
                        .Where(artifact => targetCandidates.Length == 0
                            || targetCandidates.Any(candidate => ProducesCandidate(artifact, candidate)))
                        .Select(artifact => GetFieldSegments(artifact.Target).Length)
                        .DefaultIfEmpty(-1)
                        .Max()
                })
                .Where(owner => owner.TargetDepth >= 0
                    && targetCandidates.All(candidate => owner.Artifacts.Any(artifact => ProducesCandidate(
                        artifact,
                        candidate))))
                .OrderByDescending(owner => owner.TargetDepth)
                .ThenBy(owner => owner.OwnerId)
                .ToArray();
            if (producerOwners.Length == 0)
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "A policy execution node has no guarded producer.");
            }

            var producerId = producerOwners[0].OwnerId;
            if (!claimedProducerIds.Add(producerId))
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "A guarded producer must own exactly one policy execution node.");
            }

            var expectedDependencies = expectedProviders
                .Append(producerId)
                .Distinct()
                .Order()
                .ToArray();
            var actualDependencies = policyNode.Dependencies.ToArray()
                .Select(dependency => dependency.Id)
                .ToArray();
            if (!actualDependencies.SequenceEqual(expectedDependencies))
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "A policy execution node dependencies must exactly match its producer and requirement providers.");
            }

            var expectedParentDependencies = FindParentRequirementProviders(
                targets,
                operationOwners,
                parentScope);
            var actualParentDependencies = policyNode.ParentDependencies.ToArray();
            if (!actualParentDependencies.SequenceEqual(expectedParentDependencies))
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "A policy execution node parent dependencies must exactly match its parent requirement providers.");
            }

            var expectedDependents = new HashSet<int>();
            foreach (var (ownerId, artifacts) in operationOwners)
            {
                if (ownerId == producerId || expectedProviders.Contains(ownerId))
                {
                    continue;
                }

                if (artifacts.Any(artifact => targets.Any(target =>
                    target.Path.IsParentOfOrSame(artifact.Target))))
                {
                    expectedDependents.Add(ownerId);
                }
            }

            var expectedDependentIds = expectedDependents.Order().ToArray();
            var actualDependents = policyNode.Dependents.ToArray()
                .Select(dependent => dependent.Id)
                .ToArray();
            if (!actualDependents.SequenceEqual(expectedDependentIds))
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "A policy execution node dependents must exactly match its guarded-data consumers.");
            }

            foreach (var dependency in policyNode.Dependencies)
            {
                if (dependency is not ExecutionNode dependencyNode
                    || !dependencyNode.Dependents.ToArray().Any(dependent =>
                        dependent.Id == policyNode.Id))
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "Policy execution dependencies and dependents must be reciprocal.");
                }
            }

            foreach (var dependent in policyNode.Dependents)
            {
                if (dependent is not ExecutionNode dependentNode
                    || !dependentNode.Dependencies.ToArray().Any(dependency =>
                        dependency.Id == policyNode.Id)
                    && !dependentNode.OptionalDependencies.ToArray().Any(dependency =>
                        dependency.Id == policyNode.Id))
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "Policy execution dependencies and dependents must be reciprocal.");
                }
            }
        }
    }

    private static HashSet<int> FindRequirementProviders(
        ReadOnlySpan<PolicyExecutionTarget> targets,
        IReadOnlyDictionary<int, OperationArtifact[]> operationOwners)
    {
        var result = new HashSet<int>();

        foreach (var target in targets)
        {
            foreach (var leaf in GetRequirementLeaves(target))
            {
                result.UnionWith(FindRequirementProviders(leaf, operationOwners));
            }
        }

        return result;
    }

    private static IReadOnlyDictionary<int, OperationArtifact[]> CreateOperationOwners(
        ImmutableArray<ExecutionNode> nodes)
    {
        var artifacts = new List<OperationArtifact>();
        foreach (var node in nodes)
        {
            artifacts.AddRange(CreateOperationArtifacts(node));
        }

        return artifacts
            .GroupBy(artifact => artifact.Owner.Id)
            .ToDictionary(group => group.Key, group => group.ToArray());
    }

    private static HashSet<int> FindRequirementProviders(
        string[] leaf,
        IReadOnlyDictionary<int, OperationArtifact[]> operationOwners)
    {
        var result = new HashSet<int>();

        foreach (var (ownerId, artifacts) in operationOwners)
        {
            if (artifacts.Any(artifact => ProvidesPath(artifact, leaf)))
            {
                result.Add(ownerId);
            }
        }

        return result;
    }

    private static List<string[]> GetRequirementLeaves(PolicyExecutionTarget target)
    {
        var entityPath = target.Kind is PolicyTargetKind.Field
            ? target.Path.Parent ?? SelectionPath.Root
            : target.Path;
        var leaves = new List<string[]>();
        var path = GetFieldSegments(entityPath).ToList();

        foreach (var requirement in target.Requirements)
        {
            AddRequirementLeaves(requirement.SelectionSet, path, leaves);
        }

        return leaves;
    }

    private static void AddRequirementLeaves(
        SelectionSetNode selectionSet,
        List<string> path,
        List<string[]> leaves)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is not FieldNode field)
            {
                continue;
            }

            path.Add(field.Alias?.Value ?? field.Name.Value);
            if (field.SelectionSet is { } child)
            {
                AddRequirementLeaves(child, path, leaves);
            }
            else
            {
                leaves.Add(path.ToArray());
            }

            path.RemoveAt(path.Count - 1);
        }
    }

    private static bool ProvidesPath(OperationArtifact artifact, string[] path)
    {
        var targetFields = GetFieldSegments(artifact.Target);
        if (targetFields.Length > path.Length
            || !path.AsSpan(0, targetFields.Length).SequenceEqual(targetFields))
        {
            return false;
        }

        var selectionSet = artifact.SelectionSet;
        foreach (var sourceField in GetFieldSegments(artifact.Source))
        {
            if (!TryGetFieldSelection(selectionSet, sourceField, artifact.Fragments, out selectionSet))
            {
                return false;
            }
        }

        for (var i = targetFields.Length; i < path.Length; i++)
        {
            if (!TryGetFieldSelection(selectionSet, path[i], artifact.Fragments, out selectionSet))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryGetFieldSelection(
        SelectionSetNode selectionSet,
        string responseName,
        IReadOnlyDictionary<string, FragmentDefinitionNode> fragments,
        out SelectionSetNode childSelectionSet)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field
                    when (field.Alias?.Value ?? field.Name.Value).Equals(responseName, StringComparison.Ordinal):
                    childSelectionSet = field.SelectionSet!;
                    return true;

                case InlineFragmentNode fragment
                    when TryGetFieldSelection(fragment.SelectionSet, responseName, fragments, out childSelectionSet):
                    return true;

                case FragmentSpreadNode spread
                    when fragments.TryGetValue(spread.Name.Value, out var fragment)
                        && TryGetFieldSelection(fragment.SelectionSet, responseName, fragments, out childSelectionSet):
                    return true;
            }
        }

        childSelectionSet = default!;
        return false;
    }

    private static int[] FindParentRequirementProviders(
        ReadOnlySpan<PolicyExecutionTarget> targets,
        IReadOnlyDictionary<int, OperationArtifact[]> operationOwners,
        ParentPlanScope parentScope)
    {
        if (TryFindParentRequirementProviders(
                targets,
                operationOwners,
                parentScope,
                out var dependencies,
                out _,
                out var error))
        {
            return dependencies.Dependencies;
        }

        throw ThrowHelper.InvalidOperationPlan(error!);
    }

    private static bool TryFindParentRequirementProviders(
        ReadOnlySpan<PolicyExecutionTarget> targets,
        IReadOnlyDictionary<int, OperationArtifact[]> operationOwners,
        ParentPlanScope parentScope,
        out ParentRequirementProviders dependencies,
        out PolicyExecutionTarget? failingTarget,
        out string? error)
    {
        var parentOwners = parentScope.Pieces
            .SelectMany(piece => CreateOperationOwners(piece.Nodes)
                .Select(owner => new ParentOperationOwner(
                    piece,
                    owner.Key,
                    owner.Value)))
            .ToArray();
        var selected = new Dictionary<ParentNodeKey, ExecutionNode>();
        var selectedById = new Dictionary<int, HashSet<int>>();
        var targetDependencies = new List<ParentRequirementProviderTarget>();
        string? failure = null;

        foreach (var target in targets)
        {
            var targetSelected = new HashSet<int>();
            foreach (var leaf in GetRequirementLeaves(target))
            {
                if (FindRequirementProviders(leaf, operationOwners).Count != 0)
                {
                    continue;
                }

                var providers = parentOwners
                    .Where(owner => owner.Artifacts.Any(artifact => ProvidesPath(artifact, leaf)))
                    .ToArray();
                if (providers.Length == 0)
                {
                    if (parentScope.HasImmediateParentScope)
                    {
                        dependencies = ParentRequirementProviders.Empty;
                        failingTarget = target;
                        error = "A policy parent requirement provider cannot be resolved from the immediate parent scope.";
                        return false;
                    }

                    continue;
                }

                foreach (var provider in providers)
                {
                    if (!TryAddParentDependencyClosure(provider.Piece, provider.NodeId, targetSelected))
                    {
                        dependencies = ParentRequirementProviders.Empty;
                        failingTarget = target;
                        error = failure;
                        return false;
                    }
                }
            }

            if (selectedById.Any(entry => entry.Value.Count > 1))
            {
                dependencies = ParentRequirementProviders.Empty;
                failingTarget = target;
                error = "A policy parent requirement provider is ambiguous across immediate parent plan pieces.";
                return false;
            }

            targetDependencies.Add(new ParentRequirementProviderTarget(
                target,
                [.. targetSelected.Order()]));
        }

        dependencies = new ParentRequirementProviders(
            [.. selectedById.Keys.Order()],
            [.. targetDependencies]);
        failingTarget = null;
        error = null;
        return true;

        bool TryAddParentDependencyClosure(
            ParentPlanPiece piece,
            int nodeId,
            HashSet<int> selectedForTarget)
        {
            selectedForTarget.Add(nodeId);
            var key = new ParentNodeKey(piece.Id, nodeId);
            if (selected.ContainsKey(key))
            {
                return true;
            }

            var nodes = piece.Nodes.Where(node => node.Id == nodeId).ToArray();
            if (nodes.Length != 1)
            {
                failure = "A policy parent requirement provider is ambiguous across immediate parent plan pieces.";
                return false;
            }

            var node = nodes[0];
            selected.Add(key, node);
            if (!selectedById.TryGetValue(nodeId, out var pieces))
            {
                pieces = [];
                selectedById.Add(nodeId, pieces);
            }

            pieces.Add(piece.Id);

            if (!node.ParentDependencies.IsEmpty)
            {
                failure = "A policy parent requirement provider cannot reference another parent scope.";
                return false;
            }

            foreach (var dependency in node.Dependencies)
            {
                if (!TryAddParentDependencyClosure(piece, dependency.Id, selectedForTarget))
                {
                    return false;
                }
            }

            foreach (var requirement in GetRequirements(node))
            {
                var providers = parentOwners
                    .Where(owner => owner.Artifacts.Any(artifact => ProvidesPath(
                        artifact,
                        CreateRequirementPath(requirement))))
                    .ToArray();
                if (providers.Length == 0)
                {
                    failure = "A policy parent requirement provider cannot be resolved from the immediate parent scope.";
                    return false;
                }

                foreach (var provider in providers)
                {
                    if (!TryAddParentDependencyClosure(
                            provider.Piece,
                            provider.NodeId,
                            selectedForTarget))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    private static PolicyExecutionTarget? FindTargetForMissingParentDependency(
        ParentRequirementProviders expected,
        ReadOnlySpan<int> actualDependencies)
    {
        var missing = expected.Dependencies
            .Except(actualDependencies.ToArray())
            .ToHashSet();
        if (missing.Count == 0)
        {
            return null;
        }

        return expected.Targets
            .FirstOrDefault(target => target.Dependencies.Any(missing.Contains))
            ?.Target;
    }

    private static IEnumerable<OperationRequirement> GetRequirements(ExecutionNode node)
    {
        switch (node)
        {
            case OperationExecutionNode operation:
                return operation.GetRequirementsArray();

            case ApolloOperationExecutionNode operation:
                return operation.GetRequirementsArray();

            case OperationBatchExecutionNode batch:
                return GetBatchRequirements(batch.Operations.ToArray());

            case ApolloOperationBatchExecutionNode batch:
                return GetBatchRequirements(batch.Operations.ToArray());

            default:
                return [];
        }
    }

    private static ImmutableArray<OperationRequirement> GetBatchRequirements(
        IReadOnlyList<OperationDefinition> operations)
    {
        var requirements = ImmutableArray.CreateBuilder<OperationRequirement>();

        foreach (var operation in operations)
        {
            requirements.AddRange(operation.GetRequirementsArray());
        }

        return requirements.MoveToImmutable();
    }

    private static string[] CreateRequirementPath(OperationRequirement requirement)
    {
        var path = GetFieldSegments(requirement.Path).ToList();
        var map = requirement.Map.ToString();
        var fieldStart = 0;

        while (fieldStart < map.Length
            && !char.IsLetter(map[fieldStart])
            && map[fieldStart] != '_')
        {
            fieldStart++;
        }

        var fieldEnd = fieldStart;
        while (fieldEnd < map.Length
            && (char.IsLetterOrDigit(map[fieldEnd]) || map[fieldEnd] == '_'))
        {
            fieldEnd++;
        }

        if (fieldEnd > fieldStart)
        {
            path.Add(requirement.InternalAlias ?? map[fieldStart..fieldEnd]);
        }

        return [.. path];
    }

    private static ImmutableArray<OperationArtifact> CreateOperationArtifacts(ExecutionNode node)
    {
        var artifacts = ImmutableArray.CreateBuilder<OperationArtifact>();

        switch (node)
        {
            case OperationExecutionNode operation:
                artifacts.Add(CreateOperationArtifact(
                    node,
                    operation.Target,
                    operation.Source,
                    operation.Operation.Value,
                    operation.ResultSelectionSet));
                break;
            case ApolloOperationExecutionNode operation:
                artifacts.Add(CreateOperationArtifact(
                    node,
                    operation.Target,
                    operation.Source,
                    operation.Operation.Value,
                    operation.ResultSelectionSet));
                break;
            case OperationBatchExecutionNode batch:
                foreach (var operation in batch.Operations)
                {
                    switch (operation)
                    {
                        case SingleOperationDefinition single:
                            artifacts.Add(CreateOperationArtifact(
                                node,
                                single.Target,
                                single.Source,
                                single.SourceText.Value,
                                single.ResultSelectionSet));
                            break;
                        case BatchOperationDefinition merged:
                            foreach (var target in merged.Targets)
                            {
                                artifacts.Add(CreateOperationArtifact(
                                    node,
                                    target,
                                    merged.Source,
                                    merged.SourceText.Value,
                                    merged.ResultSelectionSet));
                            }
                            break;
                    }
                }
                break;
            case ApolloOperationBatchExecutionNode batch:
                foreach (var operation in batch.Operations)
                {
                    artifacts.Add(CreateOperationArtifact(
                        node,
                        operation.Target,
                        operation.Source,
                        operation.SourceText.Value,
                        operation.ResultSelectionSet));
                }
                break;
        }

        return artifacts.ToImmutable();

        static OperationArtifact CreateOperationArtifact(
            ExecutionNode owner,
            SelectionPath target,
            SelectionPath source,
            ReadOnlyMemory<byte> sourceText,
            ResultSelectionSet resultSelectionSet)
        {
            var document = Utf8GraphQLParser.Parse(sourceText.Span, ParserOptions.Trusted);
            return new OperationArtifact(
                owner,
                target,
                source,
                document.Definitions.OfType<OperationDefinitionNode>().Single().SelectionSet,
                document.Definitions
                    .OfType<FragmentDefinitionNode>()
                    .ToDictionary(fragment => fragment.Name.Value, StringComparer.Ordinal),
                resultSelectionSet);
        }
    }

    private static ReconstructedArtifacts ReconstructArtifacts(
        ImmutableArray<Candidate> candidates)
    {
        var expressionBuilder = ImmutableArray.CreateBuilder<PolicyConditionExpression>();
        var expressionOrdinals = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var candidate in candidates.Where(candidate =>
            candidate.Reference.Facet is PolicyOccurrenceFacet.SlotGate))
        {
            var key = PolicyNameGroups.CreateCanonicalKey(candidate.Policy.Groups);
            if (expressionOrdinals.ContainsKey(key))
            {
                continue;
            }

            var ordinal = expressionBuilder.Count;
            expressionOrdinals.Add(key, ordinal);
            expressionBuilder.Add(new PolicyConditionExpression
            {
                Ordinal = ordinal,
                Groups = candidate.Policy.Groups,
                Text = PolicyNameGroups.Format(candidate.Policy.Groups)
            });
        }

        var expressions = expressionBuilder.ToImmutable();
        var positions = candidates
            .GroupBy(candidate => PolicyOccurrencePosition.Create(candidate.Reference))
            .Select(group => new ReconstructedOccurrence(
                group.Key,
                group.OrderBy(candidate => candidate.Reference.Facet)
                    .ThenBy(candidate => candidate.Reference.ApplicationOrdinal)
                    .ToImmutableArray()))
            .Where(occurrence => occurrence.Candidates.Any(candidate =>
                candidate.Reference.Facet is PolicyOccurrenceFacet.SlotGate))
            .OrderBy(occurrence => occurrence.Position.PlanPart)
            .ThenBy(occurrence => occurrence.Position.SelectionSetId)
            .ThenBy(occurrence => occurrence.Position.SelectionId)
            .ThenBy(occurrence => occurrence.Position.OccurrenceOrdinal)
            .ToArray();
        var gateKeys = new List<string>();

        foreach (var occurrence in positions)
        {
            var slotCandidates = occurrence.Candidates.Where(candidate =>
                candidate.Reference.Facet is PolicyOccurrenceFacet.SlotGate);
            var key = CreateGateKey(slotCandidates, GetRmax(occurrence.Candidates));
            if (!gateKeys.Contains(key, StringComparer.Ordinal))
            {
                gateKeys.Add(key);
            }
        }

        var slots = ImmutableArray.CreateBuilder<PolicyConditionSlot>(gateKeys.Count);
        for (var slotOrdinal = 0; slotOrdinal < gateKeys.Count; slotOrdinal++)
        {
            var gateKey = gateKeys[slotOrdinal];
            var slotOccurrences = positions.Where(occurrence =>
                CreateGateKey(
                    occurrence.Candidates.Where(candidate =>
                        candidate.Reference.Facet is PolicyOccurrenceFacet.SlotGate),
                    GetRmax(occurrence.Candidates)).Equals(gateKey, StringComparison.Ordinal))
                .ToArray();
            var slotCandidates = slotOccurrences
                .SelectMany(occurrence => occurrence.Candidates)
                .Where(candidate =>
                    candidate.Reference.Facet is PolicyOccurrenceFacet.SlotGate)
                .ToArray();
            var rmax = GetRmax(slotOccurrences[0].Candidates);
            var applications = slotCandidates
                .Select(candidate => new PolicyConditionApplication
                {
                    ExpressionOrdinal = expressionOrdinals[
                        PolicyNameGroups.CreateCanonicalKey(candidate.Policy.Groups)],
                    OnDenied = candidate.Policy.OnDenied
                })
                .Distinct()
                .OrderBy(application => application.ExpressionOrdinal)
                .ThenBy(application => application.OnDenied)
                .ToImmutableArray();
            var coordinates = ImmutableArray.CreateBuilder<PolicyConditionCoordinate>();

            foreach (var coordinateGroup in slotOccurrences
                .GroupBy(occurrence =>
                {
                    var candidate = occurrence.Candidates.First(candidate =>
                        candidate.Reference.Facet is PolicyOccurrenceFacet.SlotGate);
                    return new CoordinateKey(
                        candidate.TypeName,
                        candidate.FieldName,
                        candidate.IsRoot);
                })
                .OrderBy(group => group.Min(occurrence => occurrence.Position.PlanPart))
                .ThenBy(group => group.Min(occurrence => occurrence.Position.SelectionSetId))
                .ThenBy(group => group.Min(occurrence => occurrence.Position.SelectionId)))
            {
                var coordinateCandidates = coordinateGroup
                    .SelectMany(occurrence => occurrence.Candidates)
                    .Where(candidate =>
                        candidate.Reference.Facet is PolicyOccurrenceFacet.SlotGate)
                    .OrderBy(candidate => candidate.Reference.PlanPart)
                    .ThenBy(candidate => candidate.Reference.SelectionSetId)
                    .ThenBy(candidate => candidate.Reference.SelectionId)
                    .ThenBy(candidate => candidate.Reference.OccurrenceOrdinal)
                    .ThenBy(candidate => candidate.Reference.ApplicationOrdinal)
                    .ToArray();
                var coordinateApplications = coordinateCandidates
                    .GroupBy(candidate => candidate.Reference.ApplicationOrdinal)
                    .OrderBy(group => group.Key)
                    .Select(group =>
                    {
                        var candidate = group.First();
                        return new PolicyConditionApplication
                        {
                            ExpressionOrdinal = expressionOrdinals[
                                PolicyNameGroups.CreateCanonicalKey(candidate.Policy.Groups)],
                            OnDenied = candidate.Policy.OnDenied
                        };
                    })
                    .ToImmutableArray();
                var key = coordinateGroup.Key;

                coordinates.Add(new PolicyConditionCoordinate
                {
                    Occurrences = coordinateCandidates
                        .Select(candidate => candidate.Reference)
                        .ToImmutableArray(),
                    TypeName = key.TypeName,
                    FieldName = key.FieldName,
                    ResponseNames = [.. coordinateCandidates
                        .Select(candidate => candidate.ResponseName)
                        .Where(responseName => responseName is not null)
                        .Cast<string>()
                        .Distinct(StringComparer.Ordinal)
                        .Order(StringComparer.Ordinal)],
                    Applications = coordinateApplications,
                    IsRoot = key.IsRoot,
                    LiveGuardMasks = CanonicalizeMasks(coordinateCandidates.Select(candidate =>
                        candidate.GuardMask)),
                    GateGuardMasks = CanonicalizeMasks(coordinateCandidates
                        .Where(candidate => candidate.GateEligible)
                        .Select(candidate => candidate.GuardMask))
                });
            }

            slots.Add(new PolicyConditionSlot
            {
                Ordinal = slotOrdinal,
                Applications = applications,
                Rmax = rmax,
                GuardMasks = CanonicalizeMasks(slotCandidates.Select(candidate =>
                    candidate.GuardMask)),
                Coordinates = coordinates.ToImmutable()
            });
        }

        return new ReconstructedArtifacts(expressions, slots.ToImmutable());

        static PolicyDenialBehavior GetRmax(ImmutableArray<Candidate> occurrenceCandidates)
            => occurrenceCandidates
                .Where(candidate =>
                    candidate.Reference.Facet is PolicyOccurrenceFacet.ResidualEvaluation)
                .Select(candidate => candidate.Policy.OnDenied)
                .DefaultIfEmpty(PolicyDenialBehavior.Null)
                .Max();
    }

    private static void ValidateArtifactTables(
        ImmutableArray<PolicyConditionExpression> expressions,
        ImmutableArray<PolicyConditionSlot> slots,
        ReconstructedArtifacts expected,
        bool allowUnboundOccurrences)
    {
        if (!expressions.SequenceEqual(expected.Expressions))
        {
            throw ThrowHelper.InvalidOperationPlan(
                "The policy expression table does not match the compiled policy occurrences.");
        }

        var actualSlots = allowUnboundOccurrences
            ? ClearOccurrences(slots)
            : slots;
        var expectedSlots = allowUnboundOccurrences
            ? ClearOccurrences(expected.Slots)
            : expected.Slots;
        if (!actualSlots.SequenceEqual(expectedSlots))
        {
            throw ThrowHelper.InvalidOperationPlan(
                "The policy gate table does not match the compiled policy occurrences.");
        }

        static ImmutableArray<PolicyConditionSlot> ClearOccurrences(
            ImmutableArray<PolicyConditionSlot> source)
            => [.. source.Select(slot => slot with
            {
                Coordinates = [.. slot.Coordinates.Select(coordinate => coordinate with
                {
                    Occurrences = []
                })]
            })];
    }
    private static void BindTargets(
        ImmutableArray<ExecutionNode> nodes,
        int planPart,
        Operation operation,
        ImmutableArray<PolicyConditionSlot> slots,
        ImmutableArray<Candidate> candidates)
    {
        var used = new HashSet<PolicyOccurrenceReference>();
        foreach (var policyNode in nodes.OfType<PolicyExecutionNode>())
        {
            var targets = policyNode.Targets.ToArray();

            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                var matches = candidates
                    .Where(candidate =>
                        candidate.Reference.PlanPart == planPart
                        && candidate.Reference.Facet is PolicyOccurrenceFacet.ResidualEvaluation
                        && MatchesTargetPosition(target, candidate))
                    .OrderBy(candidate => candidate.Reference.SelectionSetId)
                    .ThenBy(candidate => candidate.Reference.SelectionId)
                    .ThenBy(candidate => candidate.Reference.OccurrenceOrdinal)
                    .ThenBy(candidate => candidate.Reference.ApplicationOrdinal)
                    .ToArray();
                var applicationCandidates = GetApplicationCandidates(matches);
                var applicationsMatch = ApplicationsMatch(target.Policies, applicationCandidates);
                var duplicateClaim = matches.Any(match => used.Contains(match.Reference));
                if (matches.Length == 0
                    || !applicationsMatch
                    || duplicateClaim)
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "A residual policy target has no unique compiled occurrence.");
                }

                foreach (var match in matches)
                {
                    used.Add(match.Reference);
                }

                targets[i] = target with
                {
                    Occurrences = matches.Select(match => match.Reference).ToImmutableArray(),
                    Conditions = CreateExpectedTargetConditions(operation, slots, matches)
                };
            }

            Array.Sort(targets, static (left, right) =>
                CompareOccurrencePosition(left.Occurrences[0], right.Occurrences[0]));

            policyNode.SetTargets(targets);
            policyNode.SetConditions(CreateExpectedNodeConditions(
                targets.Select(target => target.Conditions).ToArray()));
        }
    }

    private static void ValidateTargets(
        ImmutableArray<ExecutionNode> nodes,
        int planPart,
        Operation operation,
        ImmutableArray<PolicyConditionSlot> slots,
        IReadOnlyDictionary<PolicyOccurrenceReference, Candidate> candidates,
        HashSet<PolicyOccurrenceReference> claimed)
    {
        foreach (var policyNode in nodes.OfType<PolicyExecutionNode>())
        {
            var expectedTargetConditions = new List<ExecutionNodeCondition[]>();
            PolicyOccurrenceReference? previousOccurrence = null;

            foreach (var target in policyNode.Targets)
            {
                if (target.Occurrences.IsDefaultOrEmpty)
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "A residual policy target must reference its compiled occurrence facets.");
                }

                foreach (var occurrence in target.Occurrences)
                {
                    if (occurrence.PlanPart != planPart
                        || occurrence.Facet is not PolicyOccurrenceFacet.ResidualEvaluation
                        || !candidates.TryGetValue(occurrence, out var candidate)
                        || !MatchesTargetPosition(target, candidate))
                    {
                        throw ThrowHelper.InvalidOperationPlan(
                            "A residual policy target does not match its compiled occurrence.");
                    }

                    if (!claimed.Add(occurrence))
                    {
                        throw ThrowHelper.InvalidOperationPlan(
                            "A compiled residual policy occurrence cannot be claimed more than once in a plan part.");
                    }
                }

                var expected = candidates.Values
                    .Where(candidate =>
                        candidate.Reference.PlanPart == planPart
                        && candidate.Reference.Facet is PolicyOccurrenceFacet.ResidualEvaluation
                        && MatchesTargetPosition(target, candidate))
                    .Select(candidate => candidate.Reference)
                    .OrderBy(reference => reference.SelectionSetId)
                    .ThenBy(reference => reference.SelectionId)
                    .ThenBy(reference => reference.OccurrenceOrdinal)
                    .ThenBy(reference => reference.ApplicationOrdinal)
                    .ToImmutableArray();
                if (!target.Occurrences.SequenceEqual(expected))
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "A residual policy target must claim exactly its compiled occurrence facets.");
                }

                if (previousOccurrence is { } previous
                    && CompareOccurrencePosition(previous, expected[0]) >= 0)
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "Residual policy targets must follow compiled occurrence order.");
                }

                previousOccurrence = expected[0];
                var expectedCandidates = expected
                    .Select(reference => candidates[reference])
                    .ToArray();
                if (!ApplicationsMatch(
                    target.Policies,
                    GetApplicationCandidates(expectedCandidates)))
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "A residual policy target applications do not match declaration order.");
                }

                ValidateTargetRequirements(operation, target);

                var expectedConditions = CreateExpectedTargetConditions(
                    operation,
                    slots,
                    expectedCandidates);
                if (!ConditionsMatch(target.Conditions, expectedConditions))
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "A residual policy target conditions do not match its compiled occurrence facets.");
                }

                expectedTargetConditions.Add(expectedConditions);
            }

            var expectedNodeConditions = CreateExpectedNodeConditions(expectedTargetConditions);
            if (!ConditionsMatch(policyNode.Conditions, expectedNodeConditions))
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "A policy execution node conditions do not match its compiled residual targets.");
            }
        }
    }

    private static void ValidateTargetRequirements(
        Operation operation,
        PolicyExecutionTarget target)
    {
        var policies = ((FusionSchemaDefinition)operation.Schema).Policies.GetSnapshot()
            .ToDictionary(policy => policy.Name, StringComparer.Ordinal);
        var expected = new List<(string Name, SelectionSetNode SelectionSet)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var application in target.Policies)
        {
            foreach (var group in application.Groups)
            {
                foreach (var name in group)
                {
                    if (seen.Add(name)
                        && policies.TryGetValue(name, out var policy)
                        && policy.Requirements.Resource is { } selectionSet)
                    {
                        expected.Add((name, selectionSet));
                    }
                }
            }
        }

        if (target.Requirements.Length != expected.Count)
        {
            throw ThrowHelper.InvalidOperationPlan(
                "A residual policy target requirements do not match the policy snapshot.");
        }

        for (var i = 0; i < expected.Count; i++)
        {
            if (!target.Requirements[i].PolicyName.Equals(
                    expected[i].Name,
                    StringComparison.Ordinal)
                || !SyntaxComparer.BySyntax.Equals(
                    target.Requirements[i].SelectionSet,
                    expected[i].SelectionSet))
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "A residual policy target requirements do not match the policy snapshot.");
            }
        }
    }

    private static ImmutableArray<PolicyConditionApplication> CreateExpectedApplications(
        IReadOnlyList<Candidate> candidates,
        ImmutableArray<PolicyConditionExpression> expressions)
    {
        var applications = new List<PolicyConditionApplication>();
        var applicationOrdinals = new HashSet<int>();

        foreach (var candidate in candidates.OrderBy(candidate => candidate.Reference.ApplicationOrdinal))
        {
            if (!applicationOrdinals.Add(candidate.Reference.ApplicationOrdinal))
            {
                continue;
            }

            var expressionKey = PolicyNameGroups.CreateCanonicalKey(candidate.Policy.Groups);
            var expressionOrdinal = -1;
            for (var i = 0; i < expressions.Length; i++)
            {
                if (PolicyNameGroups.CreateCanonicalKey(expressions[i].Groups).Equals(
                    expressionKey,
                    StringComparison.Ordinal))
                {
                    expressionOrdinal = i;
                    break;
                }
            }

            if (expressionOrdinal < 0)
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "A compiled policy gate application has no canonical expression.");
            }

            applications.Add(new PolicyConditionApplication
            {
                ExpressionOrdinal = expressionOrdinal,
                OnDenied = candidate.Policy.OnDenied
            });
        }

        return [.. applications];
    }

    private static ExecutionNodeCondition[] CreateExpectedTargetConditions(
        Operation operation,
        ImmutableArray<PolicyConditionSlot> slots,
        IReadOnlyList<Candidate> candidates)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        var conditions = new List<ExecutionNodeCondition>();
        var guardMask = ulong.MaxValue;
        foreach (var candidate in candidates)
        {
            guardMask &= candidate.GuardMask;
        }

        for (var i = 0; i < operation.IncludeConditions.Count; i++)
        {
            if ((guardMask & (1UL << i)) == 0)
            {
                continue;
            }

            var includeCondition = operation.IncludeConditions[i];
            if (includeCondition.Skip is { } skip)
            {
                AddCondition(skip, passingValue: false);
            }

            if (includeCondition.Include is { } include)
            {
                AddCondition(include, passingValue: true);
            }
        }

        foreach (var slot in slots)
        {
            var claimsOccurrence = false;
            foreach (var coordinate in slot.Coordinates)
            {
                foreach (var candidate in candidates)
                {
                    var slotOccurrence = candidate.Reference with
                    {
                        Facet = PolicyOccurrenceFacet.SlotGate
                    };
                    if (coordinate.Occurrences.Contains(slotOccurrence))
                    {
                        claimsOccurrence = true;
                        break;
                    }
                }

                if (claimsOccurrence)
                {
                    break;
                }
            }

            if (claimsOccurrence)
            {
                AddCondition(slot.VariableName, passingValue: true);
            }
        }

        return [.. conditions];

        void AddCondition(string variableName, bool passingValue)
        {
            if (!conditions.Any(condition =>
                condition.PassingValue == passingValue
                && condition.VariableName.Equals(variableName, StringComparison.Ordinal)))
            {
                conditions.Add(new ExecutionNodeCondition
                {
                    VariableName = variableName,
                    PassingValue = passingValue
                });
            }
        }
    }

    private static ExecutionNodeCondition[] CreateExpectedNodeConditions(
        IReadOnlyList<ExecutionNodeCondition[]> targetConditions)
    {
        if (targetConditions.Count == 0)
        {
            return [];
        }

        var common = new List<ExecutionNodeCondition>(targetConditions[0]);
        for (var i = common.Count - 1; i >= 0; i--)
        {
            for (var j = 1; j < targetConditions.Count; j++)
            {
                if (!targetConditions[j].Contains(common[i]))
                {
                    common.RemoveAt(i);
                    break;
                }
            }
        }

        return [.. common];
    }

    private static bool ConditionsMatch(
        ReadOnlySpan<ExecutionNodeCondition> actual,
        ReadOnlySpan<ExecutionNodeCondition> expected)
    {
        if (actual.Length != expected.Length)
        {
            return false;
        }

        for (var i = 0; i < actual.Length; i++)
        {
            if (!actual[i].Equals(expected[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static int CompareOccurrencePosition(
        PolicyOccurrenceReference left,
        PolicyOccurrenceReference right)
    {
        var comparison = left.PlanPart.CompareTo(right.PlanPart);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.SelectionSetId.CompareTo(right.SelectionSetId);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.SelectionId.CompareTo(right.SelectionId);
        if (comparison != 0)
        {
            return comparison;
        }

        return left.OccurrenceOrdinal.CompareTo(right.OccurrenceOrdinal);
    }

    private static int CompareCoordinates(
        PolicyOccurrenceReference leftOccurrence,
        PolicyConditionCoordinate left,
        PolicyOccurrenceReference rightOccurrence,
        PolicyConditionCoordinate right)
    {
        var comparison = CompareOccurrencePosition(leftOccurrence, rightOccurrence);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = StringComparer.Ordinal.Compare(left.TypeName, right.TypeName);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = StringComparer.Ordinal.Compare(left.FieldName, right.FieldName);
        if (comparison != 0)
        {
            return comparison;
        }

        return left.IsRoot.CompareTo(right.IsRoot);
    }

    private static bool ApplicationsMatch(
        IReadOnlyList<PolicyApplication> applications,
        IReadOnlyList<Candidate> candidates)
    {
        if (applications.Count != candidates.Count)
        {
            return false;
        }

        for (var i = 0; i < applications.Count; i++)
        {
            if (!PolicyApplicationKey.Create(applications[i]).Matches(candidates[i].Policy))
            {
                return false;
            }
        }

        return true;
    }

    private static Candidate[] GetApplicationCandidates(
        IReadOnlyList<Candidate> candidates)
        => candidates
            .GroupBy(candidate => candidate.Reference.ApplicationOrdinal)
            .OrderBy(group => group.Key)
            .Select(group => group.First())
            .ToArray();

    private static bool MatchesCoordinate(
        PolicyConditionCoordinate coordinate,
        ImmutableArray<PolicyConditionExpression> expressions,
        Candidate candidate)
    {
        if ((candidate.Kind is PolicyTargetKind.Object) != (coordinate.FieldName is null)
            || !candidate.TypeName.Equals(coordinate.TypeName, StringComparison.Ordinal)
            || !string.Equals(candidate.FieldName, coordinate.FieldName, StringComparison.Ordinal)
            || coordinate.IsRoot != candidate.IsRoot
            || candidate.Reference.Facet is not PolicyOccurrenceFacet.SlotGate)
        {
            return false;
        }

        var applications = coordinate.Applications
            .Select(application => new PolicyApplicationKey(
                expressions[application.ExpressionOrdinal].Groups,
                application.OnDenied))
            .ToArray();
        return applications.Any(application => application.Matches(candidate.Policy));
    }

    private static bool MatchesCoordinatePosition(
        PolicyConditionCoordinate coordinate,
        Candidate candidate)
        => (candidate.Kind is PolicyTargetKind.Object) == (coordinate.FieldName is null)
            && candidate.TypeName.Equals(coordinate.TypeName, StringComparison.Ordinal)
            && string.Equals(candidate.FieldName, coordinate.FieldName, StringComparison.Ordinal)
            && coordinate.IsRoot == candidate.IsRoot;

    private static bool IsFetchGated(
        Candidate candidate,
        string variableName,
        Operation operation,
        ImmutableArray<ExecutionNode> rootNodes,
        ImmutableArray<IncrementalPlan> incrementalPlans)
    {
        var nodes = candidate.Reference.PlanPart == 0
            ? rootNodes
            : incrementalPlans[candidate.Reference.PlanPart - 1].AllNodes;
        var includeConditions = candidate.Reference.PlanPart == 0
            ? operation.IncludeConditions
            : incrementalPlans[candidate.Reference.PlanPart - 1].Operation.IncludeConditions;

        foreach (var node in nodes)
        {
            switch (node)
            {
                case OperationExecutionNode regularOperation
                    when OperationGatesCandidate(
                        regularOperation.Target,
                        regularOperation.Source,
                        regularOperation.Operation.Value,
                        regularOperation.ResultSelectionSet,
                        regularOperation.ForwardedVariables,
                        regularOperation.Conditions,
                        includeConditions,
                        candidate,
                        variableName):
                case ApolloOperationExecutionNode apolloOperation
                    when OperationGatesCandidate(
                        apolloOperation.Target,
                        apolloOperation.Source,
                        apolloOperation.Operation.Value,
                        apolloOperation.ResultSelectionSet,
                        apolloOperation.ForwardedVariables,
                        apolloOperation.Conditions,
                        includeConditions,
                        candidate,
                        variableName):
                    return true;

                case OperationBatchExecutionNode batch:
                    foreach (var definition in batch.Operations)
                    {
                        if (DefinitionGatesCandidate(
                            definition,
                            candidate,
                            variableName,
                            includeConditions))
                        {
                            return true;
                        }
                    }
                    break;

                case ApolloOperationBatchExecutionNode batch:
                    foreach (var definition in batch.Operations)
                    {
                        if (DefinitionGatesCandidate(
                            definition,
                            candidate,
                            variableName,
                            includeConditions))
                        {
                            return true;
                        }
                    }
                    break;
            }
        }

        return false;
    }

    private static bool DefinitionGatesCandidate(
        OperationDefinition definition,
        Candidate candidate,
        string variableName,
        IncludeConditionCollection includeConditions)
    {
        switch (definition)
        {
            case SingleOperationDefinition single:
                return OperationGatesCandidate(
                    single.Target,
                    single.Source,
                    single.SourceText.Value,
                    single.ResultSelectionSet,
                    single.ForwardedVariables,
                    single.Conditions,
                    includeConditions,
                    candidate,
                    variableName);

            case BatchOperationDefinition batch:
                foreach (var target in batch.Targets)
                {
                    if (OperationGatesCandidate(
                        target,
                        batch.Source,
                        batch.SourceText.Value,
                        batch.ResultSelectionSet,
                        batch.ForwardedVariables,
                        batch.Conditions,
                        includeConditions,
                        candidate,
                        variableName))
                    {
                        return true;
                    }
                }

                return false;

            default:
                return false;
        }
    }

    private static bool OperationGatesCandidate(
        SelectionPath target,
        SelectionPath source,
        ReadOnlyMemory<byte> operationSource,
        ResultSelectionSet resultSelectionSet,
        ReadOnlySpan<string> forwardedVariables,
        ReadOnlySpan<ExecutionNodeCondition> conditions,
        IncludeConditionCollection includeConditions,
        Candidate candidate,
        string variableName)
    {
        if (!ResolvesCandidate(target, resultSelectionSet, candidate.Path))
        {
            return false;
        }

        var operationGuardMask = CreateGuardMask(conditions, includeConditions);

        foreach (var condition in conditions)
        {
            if (condition.PassingValue
                && condition.VariableName.Equals(variableName, StringComparison.Ordinal)
                && (candidate.GuardMask & operationGuardMask) == operationGuardMask)
            {
                return true;
            }
        }

        var forwardsVariable = false;
        foreach (var forwardedVariable in forwardedVariables)
        {
            if (forwardedVariable.Equals(variableName, StringComparison.Ordinal))
            {
                forwardsVariable = true;
                break;
            }
        }

        return forwardsVariable
            && OperationContainsPolicyGate(
                operationSource,
                source,
                target,
                candidate,
                variableName,
                operationGuardMask,
                includeConditions);
    }

    private static bool OperationContainsPolicyGate(
        ReadOnlyMemory<byte> operationSource,
        SelectionPath source,
        SelectionPath target,
        Candidate candidate,
        string policyVariable,
        ulong operationGuardMask,
        IncludeConditionCollection includeConditions)
    {
        var targetFields = GetFieldSegments(target);
        var candidateFields = GetFieldSegments(candidate.Path);
        if (targetFields.Length > candidateFields.Length
            || !candidateFields.AsSpan(0, targetFields.Length).SequenceEqual(targetFields))
        {
            return false;
        }

        var relativeCandidate = candidateFields[targetFields.Length..];
        var sourceFields = GetFieldSegments(source);
        var document = Utf8GraphQLParser.Parse(operationSource.Span);
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        var fragments = document.Definitions
            .OfType<FragmentDefinitionNode>()
            .ToDictionary(fragment => fragment.Name.Value, StringComparer.Ordinal);
        var path = new List<string>();
        return VisitSelectionSet(
            operation.SelectionSet,
            operationGuardMask,
            policyGateActive: false);

        bool VisitSelectionSet(
            SelectionSetNode selectionSet,
            ulong inheritedMask,
            bool policyGateActive)
        {
            if (policyGateActive
                && candidate.Kind is PolicyTargetKind.Object
                && MatchesRelativePath(path, sourceFields, relativeCandidate)
                && inheritedMask == candidate.GuardMask)
            {
                return true;
            }

            foreach (var selection in selectionSet.Selections)
            {
                switch (selection)
                {
                    case FieldNode field:
                    {
                        var (mask, active) = ApplyDirectives(
                            field.Directives,
                            inheritedMask,
                            policyGateActive);
                        path.Add(field.Alias?.Value ?? field.Name.Value);
                        var matches = active
                            && candidate.Kind is PolicyTargetKind.Field
                            && MatchesRelativePath(path, sourceFields, relativeCandidate)
                            && mask == candidate.GuardMask;
                        if (!matches
                            && field.SelectionSet is { } childSelectionSet)
                        {
                            matches = VisitSelectionSet(childSelectionSet, mask, active);
                        }

                        path.RemoveAt(path.Count - 1);
                        if (matches)
                        {
                            return true;
                        }

                        break;
                    }

                    case InlineFragmentNode inlineFragment:
                    {
                        var (mask, active) = ApplyDirectives(
                            inlineFragment.Directives,
                            inheritedMask,
                            policyGateActive);
                        if (VisitSelectionSet(inlineFragment.SelectionSet, mask, active))
                        {
                            return true;
                        }

                        break;
                    }

                    case FragmentSpreadNode spread
                        when fragments.TryGetValue(spread.Name.Value, out var fragment):
                    {
                        var (mask, active) = ApplyDirectives(
                            spread.Directives,
                            inheritedMask,
                            policyGateActive);
                        var applied = ApplyDirectives(fragment.Directives, mask, active);
                        if (VisitSelectionSet(fragment.SelectionSet, applied.Mask, applied.Active))
                        {
                            return true;
                        }

                        break;
                    }
                }
            }

            return false;
        }

        (ulong Mask, bool Active) ApplyDirectives(
            IReadOnlyList<DirectiveNode> directives,
            ulong inheritedMask,
            bool policyGateActive)
        {
            var mask = inheritedMask;
            var active = policyGateActive;

            foreach (var directive in directives)
            {
                if (directive.Arguments.FirstOrDefault(argument =>
                        argument.Name.Value.Equals("if", StringComparison.Ordinal))?.Value
                    is not VariableNode variable)
                {
                    continue;
                }

                if (variable.Name.Value.Equals(policyVariable, StringComparison.Ordinal))
                {
                    active = directive.Name.Value.Equals("include", StringComparison.Ordinal);
                    continue;
                }

                for (var i = 0; i < includeConditions.Count; i++)
                {
                    var condition = includeConditions[i];
                    if ((directive.Name.Value.Equals("include", StringComparison.Ordinal)
                            && condition.Include?.Equals(
                                variable.Name.Value,
                                StringComparison.Ordinal) == true)
                        || (directive.Name.Value.Equals("skip", StringComparison.Ordinal)
                            && condition.Skip?.Equals(
                                variable.Name.Value,
                                StringComparison.Ordinal) == true))
                    {
                        mask |= 1UL << i;
                    }
                }
            }

            return (mask, active);
        }
    }

    private static bool MatchesRelativePath(
        List<string> operationPath,
        string[] sourceFields,
        string[] candidateFields)
    {
        if (operationPath.Count != sourceFields.Length + candidateFields.Length)
        {
            return false;
        }

        for (var i = 0; i < sourceFields.Length; i++)
        {
            if (!operationPath[i].Equals(sourceFields[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        for (var i = 0; i < candidateFields.Length; i++)
        {
            if (!operationPath[sourceFields.Length + i].Equals(
                candidateFields[i],
                StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ResolvesCandidate(
        SelectionPath target,
        ResultSelectionSet resultSelectionSet,
        SelectionPath candidatePath)
    {
        var targetFields = GetFieldSegments(target);
        var candidateFields = GetFieldSegments(candidatePath);
        if (targetFields.Length > candidateFields.Length
            || !candidateFields.AsSpan(0, targetFields.Length).SequenceEqual(targetFields))
        {
            return false;
        }

        var current = resultSelectionSet;
        for (var i = targetFields.Length; i < candidateFields.Length; i++)
        {
            var responseName = candidateFields[i];
            if (!Contains(current.ResponseNames, responseName))
            {
                return false;
            }

            if (i + 1 < candidateFields.Length)
            {
                current = current.TryGetChild(responseName);
                if (current is null)
                {
                    return true;
                }
            }
        }

        return true;
    }

    private static bool ProducesCandidate(
        OperationArtifact artifact,
        Candidate candidate)
    {
        if (!ResolvesCandidate(
            artifact.Target,
            artifact.ResultSelectionSet,
            candidate.Path))
        {
            return false;
        }

        return candidate.Kind is not PolicyTargetKind.Object
            || candidate.Path.IsRoot
            || GetFieldSegments(artifact.Target).Length
                < GetFieldSegments(candidate.Path).Length;
    }

    private static string[] GetFieldSegments(SelectionPath path)
    {
        var fields = new List<string>(path.Length);
        for (var i = 0; i < path.Length; i++)
        {
            if (path[i].Kind is SelectionPathSegmentKind.Field)
            {
                fields.Add(path[i].Name);
            }
        }

        return fields.ToArray();
    }

    private static ImmutableArray<ulong> CanonicalizeMasks(IEnumerable<ulong> masks)
    {
        var ordered = masks.Distinct().Order().ToArray();
        if (Array.IndexOf(ordered, 0UL) >= 0)
        {
            return [0];
        }

        var builder = ImmutableArray.CreateBuilder<ulong>(ordered.Length);
        for (var i = 0; i < ordered.Length; i++)
        {
            if (!ordered.Any(other => other != ordered[i] && (ordered[i] & other) == other))
            {
                builder.Add(ordered[i]);
            }
        }

        return builder.ToImmutable();
    }

    private static bool Contains(ReadOnlySpan<string> values, string value)
    {
        foreach (var current in values)
        {
            if (current.Equals(value, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesTargetPosition(
        PolicyExecutionTarget target,
        Candidate candidate)
        => target.Kind == candidate.Kind
            && target.TypeName.Equals(candidate.TypeName, StringComparison.Ordinal)
            && GetFieldPathKey(target.Path).Equals(
                GetFieldPathKey(candidate.Path),
                StringComparison.Ordinal);

    private static ulong CreateGuardMask(
        ReadOnlySpan<ExecutionNodeCondition> conditions,
        IncludeConditionCollection includeConditions)
    {
        var mask = 0UL;

        foreach (var condition in conditions)
        {
            if (condition.VariableName.StartsWith("__fusion_policy_", StringComparison.Ordinal))
            {
                continue;
            }

            for (var i = 0; i < includeConditions.Count; i++)
            {
                var includeCondition = includeConditions[i];
                if ((condition.PassingValue
                        && includeCondition.Include?.Equals(
                            condition.VariableName,
                            StringComparison.Ordinal) == true)
                    || (!condition.PassingValue
                        && includeCondition.Skip?.Equals(
                            condition.VariableName,
                            StringComparison.Ordinal) == true))
                {
                    mask |= 1UL << i;
                }
            }
        }

        return mask;
    }

    private static ImmutableArray<Candidate> CreateCandidates(
        Operation operation,
        ImmutableArray<IncrementalPlan> incrementalPlans,
        ImmutableArray<PolicyPlanEntry> policies,
        PolicyArtifactPolicySnapshot policySnapshot)
    {
        var builder = ImmutableArray.CreateBuilder<Candidate>();
        AddOperation(
            operation,
            planPart: 0,
            policySnapshot.RequestCacheability,
            CreateRequirementFeedPaths(operation, policySnapshot.Requirements),
            activeDeliveryGroups: [],
            builder: builder);

        for (var i = 0; i < incrementalPlans.Length; i++)
        {
            AddOperation(
                incrementalPlans[i].Operation,
                i + 1,
                policySnapshot.RequestCacheability,
                CreateRequirementFeedPaths(
                    incrementalPlans[i].Operation,
                    policySnapshot.Requirements),
                incrementalPlans[i].DeliveryGroups,
                builder: builder);
        }

        var capacityApplied = ApplyCapacity(builder.ToImmutable());
        var expectedPolicies = capacityApplied
            .SelectMany(candidate => candidate.Policy.Groups)
            .SelectMany(group => group)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(name => new PolicyPlanEntry
            {
                PolicyName = name,
                RequirementHash = policySnapshot.RequirementHashes.GetValueOrDefault(
                    name,
                    PolicyPlanEntry.ComputeRequirementHash(null))
            })
            .ToImmutableArray();
        if (!policies.SequenceEqual(expectedPolicies))
        {
            throw ThrowHelper.InvalidOperationPlan(
                "The policy inventory does not match the compiled policy occurrences.");
        }

        var result = capacityApplied
            .DistinctBy(candidate => candidate.Reference)
            .OrderBy(candidate => candidate.Reference.PlanPart)
            .ThenBy(candidate => candidate.Reference.SelectionSetId)
            .ThenBy(candidate => candidate.Reference.SelectionId)
            .ThenBy(candidate => candidate.Reference.OccurrenceOrdinal)
            .ThenBy(candidate => candidate.Reference.ApplicationOrdinal)
            .ThenBy(candidate => candidate.Reference.Facet)
            .ToImmutableArray();
        if (result.Length != capacityApplied.Length)
        {
            throw ThrowHelper.InvalidOperationPlan(
                "Compiled policy occurrences must have unique references.");
        }

        return result;
    }

    internal static PolicyArtifactPolicySnapshot CreatePolicySnapshot(Operation operation)
    {
        var policies = ((FusionSchemaDefinition)operation.Schema).Policies.GetSnapshot();
        var requestCacheability = new Dictionary<string, bool>(StringComparer.Ordinal);
        var requirements = new Dictionary<string, SelectionSetNode>(StringComparer.Ordinal);
        var requirementHashes = new Dictionary<string, ulong>(StringComparer.Ordinal);

        foreach (var policy in policies)
        {
            var policyRequirements = policy.Requirements;
            requestCacheability.Add(policy.Name, policyRequirements.IsRequestCacheable);
            requirementHashes.Add(
                policy.Name,
                PolicyPlanEntry.ComputeRequirementHash(policyRequirements.Resource));
            if (policyRequirements.Resource is { } resource)
            {
                requirements.Add(policy.Name, resource);
            }
        }

        return new PolicyArtifactPolicySnapshot(
            requestCacheability,
            requirements,
            requirementHashes);
    }

    private static ImmutableArray<Candidate> ApplyCapacity(
        ImmutableArray<Candidate> candidates)
    {
        var allocationWalk = new OperationPlanner.PolicySlotAllocationWalk([], []);
        var allocationByOccurrence = new Dictionary<
            PolicyOccurrencePosition,
            OperationPlanner.PolicySlotAllocationResult>();

        foreach (var occurrence in candidates
            .GroupBy(candidate => PolicyOccurrencePosition.Create(candidate.Reference))
            .OrderBy(group => group.Key.PlanPart)
            .ThenBy(group => group.Key.SelectionSetId)
            .ThenBy(group => group.Key.SelectionId)
            .ThenBy(group => group.Key.OccurrenceOrdinal))
        {
            var slotCandidates = occurrence
                .Where(candidate => candidate.Reference.Facet is PolicyOccurrenceFacet.SlotGate)
                .ToArray();
            if (slotCandidates.Length == 0)
            {
                continue;
            }

            var rmax = occurrence
                .Where(candidate =>
                    candidate.Reference.Facet is PolicyOccurrenceFacet.ResidualEvaluation)
                .Select(candidate => candidate.Policy.OnDenied)
                .DefaultIfEmpty(PolicyDenialBehavior.Null)
                .Max();
            var applications = slotCandidates
                .OrderBy(candidate => candidate.Reference.ApplicationOrdinal)
                .Select(candidate => new OperationPlanner.PolicyGateApplication(
                    candidate.Policy.Groups,
                    candidate.Policy.OnDenied))
                .ToImmutableArray();
            var applicationClasses = slotCandidates
                .OrderBy(candidate => candidate.Reference.ApplicationOrdinal)
                .Select(candidate => candidate.ApplicationClass is PolicyApplicationClass.S
                    ? OperationPlanner.PolicySlotApplicationClass.SlotOnly
                    : OperationPlanner.PolicySlotApplicationClass.SlotAndResidual)
                .ToImmutableArray();
            allocationByOccurrence.Add(
                occurrence.Key,
                allocationWalk.Visit(applications, rmax, applicationClasses));
        }

        var builder = ImmutableArray.CreateBuilder<Candidate>(candidates.Length);
        foreach (var candidate in candidates)
        {
            var position = PolicyOccurrencePosition.Create(candidate.Reference);
            if (candidate.Reference.Facet is not PolicyOccurrenceFacet.SlotGate)
            {
                builder.Add(candidate);
                continue;
            }

            var allocation = allocationByOccurrence[position];
            var slotCandidates = candidates
                .Where(item => PolicyOccurrencePosition.Create(item.Reference) == position
                    && item.Reference.Facet is PolicyOccurrenceFacet.SlotGate)
                .OrderBy(item => item.Reference.ApplicationOrdinal)
                .ToArray();
            var applicationIndex = Array.FindIndex(
                slotCandidates,
                item => item.Reference.ApplicationOrdinal == candidate.Reference.ApplicationOrdinal);
            var facets = allocation.ApplicationFacets[applicationIndex];
            if ((facets & OperationPlanner.PolicySlotApplicationFacets.SlotGate) != 0)
            {
                builder.Add(candidate);
            }
            else if ((facets & OperationPlanner.PolicySlotApplicationFacets.ResidualEvaluation) != 0
                && candidate.ApplicationClass is PolicyApplicationClass.S)
            {
                builder.Add(candidate with
                {
                    Reference = candidate.Reference with
                    {
                        Facet = PolicyOccurrenceFacet.ResidualEvaluation
                    }
                });
            }
        }

        return builder.ToImmutable();
    }

    private static string CreateGateKey(
        IEnumerable<Candidate> candidates,
        PolicyDenialBehavior rmax)
        => OperationPlanner.PolicySlotRegistry.CreateIdentity(
            candidates
            .Select(candidate => new OperationPlanner.PolicyGateApplication(
                candidate.Policy.Groups,
                candidate.Policy.OnDenied)),
            rmax);

    private static void AddOperation(
        Operation operation,
        int planPart,
        IReadOnlyDictionary<string, bool> requestCacheability,
        IReadOnlySet<string> requirementFeedPaths,
        ImmutableArray<DeliveryGroup> activeDeliveryGroups,
        ImmutableArray<Candidate>.Builder builder)
    {
        var activeDeferFlags = 0UL;
        foreach (var deliveryGroup in activeDeliveryGroups)
        {
            activeDeferFlags |= 1UL << deliveryGroup.DeferConditionIndex;
        }

        var visited = new HashSet<int>();
        VisitSelectionSet(
            operation.RootSelectionSet,
            SelectionPath.Root,
            isRoot: true,
            isConcreteBranch: false);

        void VisitSelectionSet(
            SelectionSet selectionSet,
            SelectionPath path,
            bool isRoot,
            bool isConcreteBranch)
        {
            if (!visited.Add(selectionSet.Id))
            {
                return;
            }

            var objectMasks = selectionSet.DeclaringSelection is { } declaringSelection
                ? GetMasks(declaringSelection)
                : new ulong[] { 0 };
            if (selectionSet.Type is FusionObjectTypeDefinition
                {
                    PolicyApplications.IsDefaultOrEmpty: false
                } objectType)
            {
                for (var occurrenceOrdinal = 0;
                    occurrenceOrdinal < objectMasks.Length;
                    occurrenceOrdinal++)
                {
                    var mask = objectMasks[occurrenceOrdinal];
                    AddApplications(
                        objectType.PolicyApplications,
                        mask,
                        PolicyTargetKind.Object,
                        path,
                        objectType.Name,
                        fieldName: null,
                        responseName: null,
                        isRoot,
                        selectionSet.Id,
                        selectionId: -1,
                        occurrenceOrdinal,
                        gateEligible: !isConcreteBranch
                            && !IsPolicyRequirementFeed(
                                PolicyTargetKind.Object,
                                path,
                                requirementFeedPaths),
                        requiresFetchGateWitness: HasSelectionInOwningPlanScope(selectionSet)
                            && !isConcreteBranch
                            && !IsPolicyRequirementFeed(
                                PolicyTargetKind.Object,
                                path,
                                requirementFeedPaths));
                }
            }

            foreach (var selection in selectionSet.Selections)
            {
                var fieldPath = path.AppendField(selection.ResponseName);
                if (selection.Field is FusionOutputFieldDefinition
                    {
                        PolicyApplications.IsDefaultOrEmpty: false
                    } field)
                {
                    var fieldMasks = GetMasks(selection);
                    for (var occurrenceOrdinal = 0;
                        occurrenceOrdinal < fieldMasks.Length;
                        occurrenceOrdinal++)
                    {
                        var mask = fieldMasks[occurrenceOrdinal];
                        AddApplications(
                            field.PolicyApplications,
                            mask,
                            PolicyTargetKind.Field,
                            fieldPath,
                            selectionSet.Type.Name,
                            field.Name,
                            selection.ResponseName,
                            isRoot: false,
                            selectionSet.Id,
                            selection.Id,
                            occurrenceOrdinal,
                            gateEligible: !isConcreteBranch
                                && !IsPolicyRequirementFeed(
                                    PolicyTargetKind.Field,
                                    fieldPath,
                                    requirementFeedPaths),
                            requiresFetchGateWitness: IsInOwningPlanScope(selection)
                                && !isConcreteBranch
                                && !IsPolicyRequirementFeed(
                                    PolicyTargetKind.Field,
                                    fieldPath,
                                    requirementFeedPaths));
                    }
                }

                if (selection.IsLeaf)
                {
                    continue;
                }

                if (selection.NamedType is FusionObjectTypeDefinition childObjectType)
                {
                    var child = selection.GetSelectionSet(childObjectType);
                    if (child is not null)
                    {
                        VisitSelectionSet(
                            child,
                            fieldPath,
                            isRoot: false,
                            isConcreteBranch);
                    }
                }
                else
                {
                    var schema = (FusionSchemaDefinition)operation.Schema;
                    foreach (var possibleType in schema
                        .GetPossibleTypes(selection.NamedType, includeInaccessible: true)
                        .OrderBy(type => type.Name, StringComparer.Ordinal))
                    {
                        var child = selection.GetSelectionSet(possibleType);
                        if (child is not null)
                        {
                            VisitSelectionSet(
                                child,
                                fieldPath,
                                isRoot: false,
                                isConcreteBranch: true);
                        }
                    }
                }
            }
        }

        void AddApplications(
            ImmutableArray<PolicyApplication> applications,
            ulong guardMask,
            PolicyTargetKind kind,
            SelectionPath path,
            string typeName,
            string? fieldName,
            string? responseName,
            bool isRoot,
            int selectionSetId,
            int selectionId,
            int occurrenceOrdinal,
            bool gateEligible,
            bool requiresFetchGateWitness)
        {
            for (var applicationOrdinal = 0;
                applicationOrdinal < applications.Length;
                applicationOrdinal++)
            {
                var application = applications[applicationOrdinal];
                var applicationClass = ClassifyApplication(application, requestCacheability);

                if (applicationClass is PolicyApplicationClass.S or PolicyApplicationClass.M)
                {
                    var slotApplication = CreateRequestCacheableApplication(
                        application,
                        requestCacheability);
                    AddCandidate(
                        PolicyOccurrenceFacet.SlotGate,
                        slotApplication,
                        applicationOrdinal,
                        applicationClass);
                }

                if (applicationClass is PolicyApplicationClass.M or PolicyApplicationClass.ResidualOnly)
                {
                    AddCandidate(
                        PolicyOccurrenceFacet.ResidualEvaluation,
                        application,
                        applicationOrdinal,
                        applicationClass);
                }
            }

            void AddCandidate(
                PolicyOccurrenceFacet facet,
                PolicyApplication application,
                int applicationOrdinal,
                PolicyApplicationClass applicationClass)
                => builder.Add(new Candidate(
                    new PolicyOccurrenceReference
                    {
                        PlanPart = planPart,
                        SelectionSetId = selectionSetId,
                        SelectionId = selectionId,
                        OccurrenceOrdinal = occurrenceOrdinal,
                        ApplicationOrdinal = applicationOrdinal,
                        Facet = facet
                    },
                    guardMask,
                    kind,
                    path,
                    typeName,
                    fieldName,
                    responseName,
                    isRoot,
                    application,
                    applicationClass,
                    gateEligible,
                    requiresFetchGateWitness));
        }

        static ulong[] GetMasks(Selection selection)
            => selection.IncludeFlags.IsEmpty ? [0] : selection.IncludeFlags.ToArray();

        bool IsInOwningPlanScope(Selection selection)
        {
            if (planPart == 0)
            {
                return true;
            }

            foreach (var deliveryGroup in activeDeliveryGroups)
            {
                if (selection.HasActiveDeliveryGroup(activeDeferFlags, deliveryGroup))
                {
                    return true;
                }
            }

            return false;
        }

        bool HasSelectionInOwningPlanScope(SelectionSet selectionSet)
        {
            if (planPart == 0)
            {
                return true;
            }

            if (selectionSet.DeclaringSelection is null)
            {
                return false;
            }

            foreach (var selection in selectionSet.Selections)
            {
                if (IsInOwningPlanScope(selection))
                {
                    return true;
                }

                if (!selection.IsLeaf
                    && selection.NamedType is FusionObjectTypeDefinition childObjectType
                    && selection.GetSelectionSet(childObjectType) is { } child
                    && HasSelectionInOwningPlanScope(child))
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsPolicyRequirementFeed(
            PolicyTargetKind kind,
            SelectionPath path,
            IReadOnlySet<string> requirementFeedPaths)
        {
            var key = CreatePathKey(path);
            if (kind is PolicyTargetKind.Field)
            {
                return requirementFeedPaths.Contains(key);
            }

            return requirementFeedPaths.Any(feed =>
                key.Length == 0
                || feed.Equals(key, StringComparison.Ordinal)
                || feed.StartsWith(key + '\u001f', StringComparison.Ordinal));
        }
    }

    private static HashSet<string> CreateRequirementFeedPaths(
        Operation operation,
        IReadOnlyDictionary<string, SelectionSetNode> requirements)
    {
        var feeds = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<(int SelectionSetId, string Path)>();
        VisitSelectionSet(operation.RootSelectionSet, SelectionPath.Root);
        return feeds;

        void VisitSelectionSet(SelectionSet selectionSet, SelectionPath path)
        {
            if (!visited.Add((selectionSet.Id, CreatePathKey(path))))
            {
                return;
            }

            if (selectionSet.Type is FusionObjectTypeDefinition objectType)
            {
                AddRequirements(objectType.PolicyApplications, path);
            }
            else
            {
                foreach (var possibleType in ((FusionSchemaDefinition)operation.Schema)
                    .GetPossibleTypes(selectionSet.Type, includeInaccessible: true))
                {
                    AddRequirements(possibleType.PolicyApplications, path);
                }
            }

            foreach (var selection in selectionSet.Selections)
            {
                if (selection.Field is FusionOutputFieldDefinition field)
                {
                    AddRequirements(field.PolicyApplications, path);
                }

                if (selection.IsLeaf)
                {
                    continue;
                }

                var fieldPath = path.AppendField(selection.ResponseName);
                if (selection.NamedType is FusionObjectTypeDefinition childObjectType)
                {
                    AddRequirements(childObjectType.PolicyApplications, fieldPath);
                    if (selection.GetSelectionSet(childObjectType) is { } child)
                    {
                        VisitSelectionSet(child, fieldPath);
                    }
                }
                else
                {
                    foreach (var possibleType in ((FusionSchemaDefinition)operation.Schema)
                        .GetPossibleTypes(selection.NamedType, includeInaccessible: true))
                    {
                        AddRequirements(possibleType.PolicyApplications, fieldPath);
                        if (selection.GetSelectionSet(possibleType) is { } child)
                        {
                            VisitSelectionSet(child, fieldPath);
                        }
                    }
                }
            }
        }

        void AddRequirements(
            ImmutableArray<PolicyApplication> applications,
            SelectionPath entityPath)
        {
            if (applications.IsDefaultOrEmpty)
            {
                return;
            }

            foreach (var application in applications)
            {
                foreach (var group in application.Groups)
                {
                    foreach (var name in group)
                    {
                        if (requirements.TryGetValue(name, out var requirement))
                        {
                            AddRequirementFields(requirement, entityPath);
                        }
                    }
                }
            }
        }

        void AddRequirementFields(SelectionSetNode selectionSet, SelectionPath entityPath)
        {
            foreach (var selection in selectionSet.Selections)
            {
                if (selection is not FieldNode field)
                {
                    continue;
                }

                var fieldPath = entityPath.AppendField(field.Alias?.Value ?? field.Name.Value);
                feeds.Add(CreatePathKey(fieldPath));
                if (field.SelectionSet is { } child)
                {
                    AddRequirementFields(child, fieldPath);
                }
            }
        }
    }

    private static string CreatePathKey(SelectionPath path)
        => string.Join('\u001f', GetFieldSegments(path));

    private static PolicyApplication CreateRequestCacheableApplication(
        PolicyApplication application,
        IReadOnlyDictionary<string, bool> requestCacheability)
    {
        return new PolicyApplication
        {
            Groups = PolicyNameGroups.Canonicalize(
                [.. application.Groups.Select(group => group
                    .Where(name => IsPolicyRequestCacheable(name, requestCacheability))
                    .ToImmutableArray())]),
            OnDenied = application.OnDenied
        };
    }

    private static PolicyApplicationClass ClassifyApplication(
        PolicyApplication application,
        IReadOnlyDictionary<string, bool> requestCacheability)
    {
        var hasRequestCacheable = false;
        var hasDataBearing = false;

        foreach (var group in application.Groups)
        {
            var groupHasRequestCacheable = false;
            foreach (var name in group)
            {
                if (IsPolicyRequestCacheable(name, requestCacheability))
                {
                    hasRequestCacheable = true;
                    groupHasRequestCacheable = true;
                }
                else
                {
                    hasDataBearing = true;
                }
            }

            if (!groupHasRequestCacheable)
            {
                return PolicyApplicationClass.ResidualOnly;
            }
        }

        return hasDataBearing
            ? hasRequestCacheable
                ? PolicyApplicationClass.M
                : PolicyApplicationClass.ResidualOnly
            : PolicyApplicationClass.S;
    }

    private static bool IsPolicyRequestCacheable(
        string policyName,
        IReadOnlyDictionary<string, bool> requestCacheability)
        => requestCacheability.TryGetValue(policyName, out var cacheable) && cacheable;

    private static string GetFieldPathKey(SelectionPath path)
    {
        var builder = new System.Text.StringBuilder();
        for (var i = 0; i < path.Length; i++)
        {
            var segment = path[i];
            if (segment.Kind is SelectionPathSegmentKind.Field)
            {
                builder.Append('/');
                builder.Append(segment.Name);
            }
        }

        return builder.ToString();
    }

    private sealed record Candidate(
        PolicyOccurrenceReference Reference,
        ulong GuardMask,
        PolicyTargetKind Kind,
        SelectionPath Path,
        string TypeName,
        string? FieldName,
        string? ResponseName,
        bool IsRoot,
        PolicyApplication Policy,
        PolicyApplicationClass ApplicationClass,
        bool GateEligible,
        bool RequiresFetchGateWitness);

    private readonly record struct PolicyOccurrencePosition(
        int PlanPart,
        int SelectionSetId,
        int SelectionId,
        int OccurrenceOrdinal)
    {
        public static PolicyOccurrencePosition Create(PolicyOccurrenceReference reference)
            => new(
                reference.PlanPart,
                reference.SelectionSetId,
                reference.SelectionId,
                reference.OccurrenceOrdinal);
    }

    private readonly record struct CoordinateKey(
        string TypeName,
        string? FieldName,
        bool IsRoot);

    private sealed record ReconstructedOccurrence(
        PolicyOccurrencePosition Position,
        ImmutableArray<Candidate> Candidates);

    private sealed record ReconstructedArtifacts(
        ImmutableArray<PolicyConditionExpression> Expressions,
        ImmutableArray<PolicyConditionSlot> Slots);

    private sealed record OperationArtifact(
        ExecutionNode Owner,
        SelectionPath Target,
        SelectionPath Source,
        SelectionSetNode SelectionSet,
        IReadOnlyDictionary<string, FragmentDefinitionNode> Fragments,
        ResultSelectionSet ResultSelectionSet);

    private sealed record ParentPlanScope(
        ImmutableArray<ParentPlanPiece> Pieces,
        bool HasImmediateParentScope)
    {
        public static ParentPlanScope Empty { get; } = new([], HasImmediateParentScope: false);
    }

    private sealed record ParentPlanPiece(int Id, ImmutableArray<ExecutionNode> Nodes);

    private sealed record ParentOperationOwner(
        ParentPlanPiece Piece,
        int NodeId,
        OperationArtifact[] Artifacts);

    private readonly record struct ParentNodeKey(int PieceId, int NodeId);

    private sealed record ParentRequirementProviders(
        int[] Dependencies,
        ParentRequirementProviderTarget[] Targets)
    {
        public static ParentRequirementProviders Empty { get; } = new([], []);
    }

    private sealed record ParentRequirementProviderTarget(
        PolicyExecutionTarget Target,
        int[] Dependencies);

    private enum PolicyApplicationClass
    {
        S,
        M,
        ResidualOnly
    }

    private readonly record struct PolicyApplicationKey(
        ImmutableArray<ImmutableArray<string>> Groups,
        PolicyDenialBehavior OnDenied)
    {
        public static PolicyApplicationKey Create(PolicyApplication application)
            => new(application.Groups, application.OnDenied);

        public bool Matches(PolicyApplication application)
            => OnDenied == application.OnDenied
                && PolicyNameGroups.CreateCanonicalKey(Groups).Equals(
                    PolicyNameGroups.CreateCanonicalKey(application.Groups),
                    StringComparison.Ordinal);
    }
}

internal sealed record PolicyArtifactPolicySnapshot(
    IReadOnlyDictionary<string, bool> RequestCacheability,
    IReadOnlyDictionary<string, SelectionSetNode> Requirements,
    IReadOnlyDictionary<string, ulong> RequirementHashes);
