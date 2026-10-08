using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types.Rewriters;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed partial class OperationPlanner
{
    /// <summary>
    /// Moves the prerequisite steps of a deferred requirement that the enclosing scope can only
    /// partly host into that scope, so the remaining deferred steps read every requirement from it.
    /// Returns <see langword="false"/> and leaves everything unchanged when any step cannot be moved
    /// or when the move would extend the enclosing scope's dependency depth.
    /// </summary>
    private bool TryLiftDeferRequirementChain(
        IncrementalPlanDescriptor descriptor,
        ImmutableList<PlanStep> incrementalPlanSteps,
        ImmutableArray<OperationPlanStep> producers,
        ParentPlanContext parentContext,
        ValueSelectionToSelectionSetRewriter resolver,
        out ScopeState liftedScope,
        out ImmutableList<PlanStep> liftedPlan)
    {
        liftedScope = null!;
        liftedPlan = incrementalPlanSteps;

        var deferredLeaves = new HashSet<string>(StringComparer.Ordinal);

        if (!DeferChainLift.TryCollectLeafPaths(
            descriptor.Operation.SelectionSet,
            "$",
            deferredLeaves,
            allowArguments: true))
        {
            return false;
        }

        var relayIds = new HashSet<int>();
        string? relaySchemaName = null;

        foreach (var producer in producers)
        {
            if (producer.Lookup is null
                || SyntaxComparer.BySyntax.Equals(GetStepEntitySelectionSet(producer), producer.Lookup.Requirements))
            {
                relayIds.Add(producer.Id);
                relaySchemaName ??= producer.SchemaName;
            }
        }

        var lift = new DeferChainLift(
            this,
            incrementalPlanSteps,
            relaySchemaName,
            relayIds,
            deferredLeaves,
            resolver,
            new ScopeState(parentContext.ParentSteps, parentContext.ParentInternalOperation));

        if (!lift.TryClassify() || !lift.TryLift(out var rewrittenPlan))
        {
            return false;
        }

        if (GetDependencyDepth(lift.Scope.Steps) > GetDependencyDepth(parentContext.ParentSteps))
        {
            return false;
        }

        liftedScope = lift.Scope;
        liftedPlan = rewrittenPlan;
        return true;
    }

    private static int GetDependencyDepth(ImmutableList<PlanStep> steps)
    {
        var depths = new Dictionary<int, int>();
        var depth = 0;

        foreach (var step in steps)
        {
            depth = Math.Max(depth, GetStepDepth(step, steps, depths));
        }

        return depth;

        static int GetStepDepth(PlanStep step, ImmutableList<PlanStep> steps, Dictionary<int, int> depths)
        {
            if (step is not OperationPlanStep operationStep)
            {
                return 1;
            }

            if (depths.TryGetValue(operationStep.Id, out var cached))
            {
                return cached;
            }

            var longest = 0;

            foreach (var dependentId in operationStep.Dependents)
            {
                if (steps.ById(dependentId) is { } dependent)
                {
                    longest = Math.Max(longest, GetStepDepth(dependent, steps, depths));
                }
            }

            depths[operationStep.Id] = longest + 1;
            return longest + 1;
        }
    }

    /// <summary>
    /// Holds the state of one attempt to lift the prerequisite steps of a deferred plan into
    /// its enclosing scope.
    /// </summary>
    private sealed class DeferChainLift(
        OperationPlanner planner,
        ImmutableList<PlanStep> steps,
        string? fallbackSchemaName,
        HashSet<int> relayIds,
        HashSet<string> deferredLeaves,
        ValueSelectionToSelectionSetRewriter resolver,
        ScopeState scope)
    {
        private readonly Dictionary<int, HashSet<string>> _chainLeaves = [];
        private readonly Dictionary<int, int> _parentStepIdByChainStepId = [];
        private readonly Dictionary<string, int> _inlinedParentStepIds = new(StringComparer.Ordinal);
        private readonly List<OperationPlanStep> _chainSteps = [];
        private readonly List<OperationPlanStep> _keptSteps = [];

        public ScopeState Scope => scope;

        /// <summary>
        /// Splits the non-producer steps into chain steps, whose every selected leaf is consumed by
        /// a dependent's requirement, and kept steps, which carry deferred output of their own.
        /// </summary>
        public bool TryClassify()
        {
            foreach (var step in steps)
            {
                if (step is not OperationPlanStep operationStep)
                {
                    return false;
                }

                if (relayIds.Contains(operationStep.Id))
                {
                    continue;
                }

                if (!TryCollectEntityLeafPaths(operationStep, out var leaves))
                {
                    _keptSteps.Add(operationStep);
                    continue;
                }

                var consumed = new HashSet<string>(StringComparer.Ordinal);

                foreach (var dependentId in operationStep.Dependents)
                {
                    if (steps.ById(dependentId) is not OperationPlanStep dependent)
                    {
                        return false;
                    }

                    foreach (var (_, requirement) in dependent.Requirements)
                    {
                        if (!TryCollectRequirementLeafPaths(requirement, out var requirementLeaves))
                        {
                            return false;
                        }

                        consumed.UnionWith(requirementLeaves);
                    }
                }

                if (leaves.Count > 0 && leaves.IsSubsetOf(consumed))
                {
                    if (!IsLiftable(operationStep) || leaves.Overlaps(deferredLeaves))
                    {
                        return false;
                    }

                    _chainLeaves[operationStep.Id] = leaves;
                    _chainSteps.Add(operationStep);
                }
                else
                {
                    _keptSteps.Add(operationStep);
                }
            }

            if (_chainSteps.Count == 0 || _keptSteps.Count == 0)
            {
                return false;
            }

            foreach (var keptStep in _keptSteps)
            {
                if (!keptStep.Dependents.IsEmpty)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Hosts or promotes every chain step into the enclosing scope and rewrites the kept steps
        /// to read all of their requirements from it.
        /// </summary>
        public bool TryLift(out ImmutableList<PlanStep> rewrittenPlan)
        {
            rewrittenPlan = steps;

            foreach (var chainStep in OrderChainSteps())
            {
                if (!TryResolveSuppliers(chainStep, out var suppliers))
                {
                    return false;
                }

                if (TryHost(chainStep, out var hostStepId))
                {
                    if (!TryMirrorIntoInternalOperation(chainStep))
                    {
                        return false;
                    }

                    _parentStepIdByChainStepId[chainStep.Id] = hostStepId;
                    continue;
                }

                var promotedStepId = scope.Steps.NextId();
                var index = SelectionSetIndexer.Create(chainStep.Definition);
                var promotedStep = chainStep with
                {
                    Id = promotedStepId,
                    Dependents = [],
                    ParentDependencies = [],
                    SelectionSets = SelectionSetIndexer.CreateIdSet(chainStep.Definition.SelectionSet, index),
                    RootSelectionSetId = index.GetId(chainStep.Definition.SelectionSet)
                };

                scope.Steps = scope.Steps.Add(promotedStep);

                foreach (var (_, supplierId) in suppliers)
                {
                    AddDependent(supplierId, promotedStepId);
                }

                if (!TryMirrorIntoInternalOperation(chainStep))
                {
                    return false;
                }

                _parentStepIdByChainStepId[chainStep.Id] = promotedStepId;
            }

            var lifted = new List<LiftedDeferRequirement>();

            foreach (var keptStep in _keptSteps)
            {
                if (!TryResolveSuppliers(keptStep, out var suppliers))
                {
                    return false;
                }

                foreach (var (requirement, supplierId) in suppliers)
                {
                    lifted.Add(new LiftedDeferRequirement(requirement, keptStep.Id, supplierId));
                }
            }

            var droppedStepIds = new HashSet<int>(relayIds);

            foreach (var chainStep in _chainSteps)
            {
                droppedStepIds.Add(chainStep.Id);
            }

            rewrittenPlan = RewriteIncrementalPlanAfterDeferRequirementRouting(steps, lifted, droppedStepIds);
            return true;
        }

        private List<OperationPlanStep> OrderChainSteps()
        {
            var ordered = new List<OperationPlanStep>(_chainSteps.Count);
            var visited = new HashSet<int>();

            foreach (var chainStep in _chainSteps)
            {
                Visit(chainStep);
            }

            return ordered;

            void Visit(OperationPlanStep chainStep)
            {
                if (!visited.Add(chainStep.Id))
                {
                    return;
                }

                foreach (var candidate in _chainSteps)
                {
                    if (candidate.Dependents.Contains(chainStep.Id))
                    {
                        Visit(candidate);
                    }
                }

                ordered.Add(chainStep);
            }
        }

        /// <summary>
        /// Resolves the enclosing-scope step that supplies each requirement of
        /// <paramref name="consumer"/>, either a lifted chain step or a step that hosts the
        /// requirement's key.
        /// </summary>
        private bool TryResolveSuppliers(
            OperationPlanStep consumer,
            out List<(OperationRequirement Requirement, int SupplierId)> suppliers)
        {
            suppliers = [];

            foreach (var (_, requirement) in consumer.Requirements)
            {
                if (!TryCollectRequirementLeafPaths(requirement, out var leaves))
                {
                    return false;
                }

                var uncovered = new HashSet<string>(leaves, StringComparer.Ordinal);

                foreach (var step in steps)
                {
                    if (step is OperationPlanStep provider
                        && provider.Dependents.Contains(consumer.Id)
                        && _chainLeaves.TryGetValue(provider.Id, out var providerLeaves)
                        && leaves.Overlaps(providerLeaves))
                    {
                        if (!_parentStepIdByChainStepId.TryGetValue(provider.Id, out var parentStepId))
                        {
                            return false;
                        }

                        suppliers.Add((requirement, parentStepId));
                        uncovered.ExceptWith(providerLeaves);
                    }
                }

                if (uncovered.Count == 0)
                {
                    continue;
                }

                if (uncovered.Count != leaves.Count
                    || !TryInlineKeyRequirement(consumer, requirement, out var inlinedStepId))
                {
                    return false;
                }

                suppliers.Add((requirement, inlinedStepId));
            }

            return true;
        }

        private bool TryInlineKeyRequirement(
            OperationPlanStep consumer,
            OperationRequirement requirement,
            out int parentStepId)
        {
            var (sourceStep, sourceRequirement) =
                ResolveDeferRequirementSource(steps, consumer, requirement, relayIds);

            var schemaName =
                (sourceStep is not null
                    ? TryFindDeferRequirementProvider(steps, sourceStep, sourceRequirement)?.SchemaName
                    : null)
                ?? fallbackSchemaName;

            parentStepId = 0;

            if (schemaName is null)
            {
                return false;
            }

            var key = $"{schemaName}|{sourceRequirement.Path}|{sourceRequirement.Map}";

            if (_inlinedParentStepIds.TryGetValue(key, out parentStepId))
            {
                return true;
            }

            if (!planner.TryInlineDeferRequirementInScope(
                sourceRequirement,
                schemaName,
                resolver,
                scope,
                out parentStepId,
                out var isPartiallyResolvable)
                || isPartiallyResolvable)
            {
                return false;
            }

            _inlinedParentStepIds[key] = parentStepId;
            return true;
        }

        private bool TryHost(OperationPlanStep chainStep, out int hostStepId)
        {
            hostStepId = 0;

            for (var i = 0; i < scope.Steps.Count; i++)
            {
                if (scope.Steps[i] is not OperationPlanStep parentStep
                    || !string.Equals(parentStep.SchemaName, chainStep.SchemaName, StringComparison.Ordinal)
                    || !parentStep.Target.IsParentOfOrSame(chainStep.Target)
                    || !planner.TryLocateDeferRequirementTarget(
                        parentStep,
                        chainStep.Target,
                        out _,
                        out var targetType))
                {
                    continue;
                }

                var injectionSelections = GetStepEntitySelectionSet(chainStep);
                var stepIndex = SelectionSetIndexer.Create(parentStep.Definition).ToBuilder();
                var targetId = stepIndex.GetId(
                    LocateSelectionSetAtPath(
                        GetStepEntitySelectionSet(parentStep),
                        chainStep.Target,
                        parentStep.Target.Length));
                var dependentsBeforeInline = parentStep.Dependents;

                RegisterRequirementSelectionSets(injectionSelections, stepIndex);

                if (!planner.TryInlineSelectionSetIntoStep(
                    parentStep,
                    targetId,
                    targetType,
                    chainStep.Target,
                    injectionSelections,
                    dependentStepId: 0,
                    stepIndex,
                    new RequirementAliasContext([], RequirementAliasRegistry.Empty),
                    out var updatedParentStep,
                    out var unresolvable,
                    out var fieldsWithRequirements)
                    || !unresolvable.IsEmpty
                    || !fieldsWithRequirements.IsEmpty)
                {
                    continue;
                }

                updatedParentStep = updatedParentStep with
                {
                    Dependents = dependentsBeforeInline,
                    SelectionSets = SelectionSetIndexer.CreateIdSet(updatedParentStep.Definition.SelectionSet, stepIndex)
                };

                scope.Steps = scope.Steps.SetItem(i, updatedParentStep);
                hostStepId = parentStep.Id;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds the fields the chain step produces to the enclosing scope's internal operation as
        /// requirement fields, so the result of the scope carries them.
        /// </summary>
        private bool TryMirrorIntoInternalOperation(OperationPlanStep chainStep)
        {
            var selections = GetStepEntitySelectionSet(chainStep);

            if (chainStep.Lookup is { } lookup && SyntaxComparer.BySyntax.Equals(selections, lookup.Requirements))
            {
                return true;
            }

            if (!TryRemoveRequirementArguments(selections, chainStep.Requirements, out var strippedSelections)
                || !TryLocateInternalSelectionSetAtPath(
                    scope.InternalOperation.SelectionSet,
                    chainStep.Target,
                    out var internalTargetSelectionSet))
            {
                return false;
            }

            foreach (var selection in strippedSelections.Selections)
            {
                if (selection is FieldNode field && HasFieldWithArguments(internalTargetSelectionSet, field))
                {
                    return false;
                }
            }

            var overallIndex = SelectionSetIndexer.Create(scope.InternalOperation).ToBuilder();
            scope.InternalOperation = planner.InlineSelectionsIntoOverallOperation(
                scope.InternalOperation,
                overallIndex,
                chainStep.Type,
                overallIndex.GetId(internalTargetSelectionSet),
                strippedSelections);
            return true;
        }

        private void AddDependent(int providerStepId, int dependentStepId)
        {
            for (var i = 0; i < scope.Steps.Count; i++)
            {
                if (scope.Steps[i] is OperationPlanStep provider && provider.Id == providerStepId)
                {
                    scope.Steps = scope.Steps.SetItem(
                        i,
                        provider with { Dependents = provider.Dependents.Add(dependentStepId) });
                    return;
                }
            }
        }

        private static bool IsLiftable(OperationPlanStep step)
        {
            if (step.SchemaName is null
                || step.Lookup is null
                || step.Conditions.Length > 0
                || step.EventStreamPlan is not null
                || step.Definition.Operation != OperationType.Query
                || ContainsUnknownVariable(step.Definition, step.Requirements))
            {
                return false;
            }

            for (var i = 0; i < step.Target.Length; i++)
            {
                if (step.Target[i].Kind != SelectionPathSegmentKind.Field)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsUnknownVariable(
            ISyntaxNode node,
            ImmutableDictionary<string, OperationRequirement> requirements)
        {
            if (node is VariableNode variable)
            {
                return !requirements.ContainsKey(variable.Name.Value);
            }

            foreach (var child in node.GetNodes())
            {
                if (ContainsUnknownVariable(child, requirements))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasFieldWithArguments(SelectionSetNode selectionSet, FieldNode field)
        {
            var responseName = field.Alias?.Value ?? field.Name.Value;

            foreach (var selection in selectionSet.Selections)
            {
                if (selection is FieldNode existing
                    && existing.Arguments.Count > 0
                    && string.Equals(existing.Alias?.Value ?? existing.Name.Value, responseName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryRemoveRequirementArguments(
            SelectionSetNode selectionSet,
            ImmutableDictionary<string, OperationRequirement> requirements,
            out SelectionSetNode result)
        {
            var selections = new List<ISelectionNode>(selectionSet.Selections.Count);

            foreach (var selection in selectionSet.Selections)
            {
                if (selection is not FieldNode field || field.Directives.Count > 0)
                {
                    result = selectionSet;
                    return false;
                }

                foreach (var argument in field.Arguments)
                {
                    if (argument.Value is not VariableNode variable
                        || !requirements.ContainsKey(variable.Name.Value))
                    {
                        result = selectionSet;
                        return false;
                    }
                }

                SelectionSetNode? nested = null;

                if (field.SelectionSet is { } fieldSelectionSet
                    && !TryRemoveRequirementArguments(fieldSelectionSet, requirements, out nested))
                {
                    result = selectionSet;
                    return false;
                }

                selections.Add(new FieldNode(field.Name, field.Alias, [], [], nested));
            }

            result = new SelectionSetNode(selections);
            return true;
        }

        /// <summary>
        /// Collects the absolute paths of the leaf fields a step selects. Returns
        /// <see langword="false"/> when the selection uses fragments or directives.
        /// </summary>
        private static bool TryCollectEntityLeafPaths(OperationPlanStep step, out HashSet<string> leaves)
        {
            leaves = new HashSet<string>(StringComparer.Ordinal);
            return TryCollectLeafPaths(
                GetStepEntitySelectionSet(step),
                step.Target.ToString(),
                leaves,
                allowArguments: true);
        }

        /// <summary>
        /// Collects the absolute paths of the leaf fields a requirement reads. Returns
        /// <see langword="false"/> when the requirement is aliased or its map uses fragments,
        /// arguments or directives.
        /// </summary>
        private static bool TryCollectRequirementLeafPaths(
            OperationRequirement requirement,
            out HashSet<string> leaves)
        {
            leaves = new HashSet<string>(StringComparer.Ordinal);

            if (requirement.InternalAlias is not null)
            {
                return false;
            }

            var selectionSet = ValueSelectionToSelectionSetRewriter.Rewrite([requirement.Map]);
            return TryCollectLeafPaths(selectionSet, requirement.Path.ToString(), leaves, allowArguments: false);
        }

        public static bool TryCollectLeafPaths(
            SelectionSetNode selectionSet,
            string basePath,
            HashSet<string> leaves,
            bool allowArguments)
        {
            foreach (var selection in selectionSet.Selections)
            {
                if (selection is InlineFragmentNode { TypeCondition: null, Directives.Count: 0 } group)
                {
                    if (!TryCollectLeafPaths(group.SelectionSet, basePath, leaves, allowArguments))
                    {
                        return false;
                    }

                    continue;
                }

                if (selection is not FieldNode field
                    || field.Directives.Count > 0
                    || (!allowArguments && field.Arguments.Count > 0))
                {
                    return false;
                }

                if (field.Name.Value.Equals("__typename", StringComparison.Ordinal))
                {
                    continue;
                }

                var path = basePath + "." + (field.Alias?.Value ?? field.Name.Value);

                if (field.SelectionSet is { } nested)
                {
                    if (!TryCollectLeafPaths(nested, path, leaves, allowArguments))
                    {
                        return false;
                    }
                }
                else
                {
                    leaves.Add(path);
                }
            }

            return true;
        }
    }
}
