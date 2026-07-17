using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using ThrowHelper = HotChocolate.Fusion.Execution.ThrowHelper;

namespace HotChocolate.Fusion.Planning;

public sealed partial class OperationPlanner
{
    private const string UploadScalarName = "Upload";
    private const string DynamicSchemaNameMarker = "__dynamic__";

    /// <summary>
    /// Converts the planner's intermediate plan steps into a final execution plan
    /// that the executor can run against the downstream source schemas.
    /// </summary>
    private OperationPlan BuildExecutionPlan(
        Operation operation,
        OperationDefinitionNode operationDefinition,
        ImmutableList<PlanStep> planSteps,
        ImmutableArray<DeliveryGroup> deliveryGroups,
        ImmutableArray<IncrementalPlan> incrementalPlans,
        int searchSpace,
        int expandedNodes,
        CancellationToken cancellationToken)
    {
        if (operation.IsIntrospectionOnly())
        {
            var introspectionNode = new IntrospectionExecutionNode(
                1,
                [.. operation.RootSelectionSet.Selections],
                []);
            introspectionNode.Seal();

            var nodes = ImmutableArray.Create<ExecutionNode>(introspectionNode);

            return OperationPlan.Create(operation, nodes, nodes, [], [], searchSpace, expandedNodes);
        }

        var ctx = new ExecutionPlanBuildContext();
        var hasVariables = operationDefinition.VariableDefinitions.Count > 0;

        planSteps = TransformPlanSteps(planSteps, operationDefinition);
        IndexDependencies(planSteps, ctx);
        BuildExecutionNodes(planSteps, ctx, _schema, hasVariables, cancellationToken);
        var policyRequirementProviders = OperationPlanner.AddPolicyRequirementDependencies(ctx);
        var policyGuards = CreatePolicyGuardLookup(ctx, policyRequirementProviders);
        MergeAndBatchOperations(ctx, _schema, _options.EnableRequestGrouping, _options.MergePolicy);
        ApplyPolicyGuards(ctx, policyGuards);
        WireExecutionDependencies(ctx);

        var rootNodes = planSteps
            .Where(t => !ctx.DependenciesByStepId.ContainsKey(t.Id) && ctx.ExecutionNodes.ContainsKey(t.Id))
            .Select(t => ctx.ExecutionNodes[t.Id])
            .ToImmutableArray();

        var allNodes = ctx.ExecutionNodes
            .OrderBy(t => t.Key)
            .Select(t => t.Value)
            .ToImmutableArray();

        if (operation.HasIntrospectionFields())
        {
            var introspectionNode = new IntrospectionExecutionNode(
                allNodes.Max(t => t.Id) + 1,
                operation.GetIntrospectionSelections(),
                []);
            rootNodes = rootNodes.Add(introspectionNode);
            allNodes = allNodes.Add(introspectionNode);
        }

        foreach (var node in allNodes)
        {
            node.Seal();
        }

        var operationPlan = OperationPlan.Create(
            operation,
            rootNodes,
            allNodes,
            deliveryGroups,
            incrementalPlans,
            searchSpace,
            expandedNodes);

        // Assign parent node ids and stable ids after the root plan and
        // incremental plan nodes have been built. Nested incremental plans are
        // associated with the plan that owns their parent delivery group.
        if (!incrementalPlans.IsDefaultOrEmpty)
        {
            // Plan-time id: the parent id is known here because the root plan
            // was just created above, and OperationPlan.Create's content hash
            // does not include incremental plan ids.
            for (var i = 0; i < incrementalPlans.Length; i++)
            {
                var incrementalPlan = incrementalPlans[i];
                incrementalPlan.Id = $"{operationPlan.Id}#{i}";

                var path = ResolveIncrementalPlanPath(incrementalPlan);
                var parent = ResolveIncrementalPlanParent(incrementalPlan, incrementalPlans);
                var owningNodes = parent is null ? allNodes : parent.AllNodes;
                incrementalPlan.ParentNodeId = ResolveDeferParentNodeId(owningNodes, path)
                    ?? throw ThrowHelper.IncrementalPlanParentNotFound(path);
            }
        }

        return operationPlan;
    }

    /// <summary>
    /// Returns the anchor path for the given incremental plan. When the plan
    /// has multiple delivery groups, the deepest delivery group path is used.
    /// </summary>
    private static SelectionPath ResolveIncrementalPlanPath(IncrementalPlan incrementalPlan)
    {
        SelectionPath? best = null;

        foreach (var usage in incrementalPlan.DeliveryGroups)
        {
            if (usage.Path is null)
            {
                continue;
            }

            if (best is null || usage.Path.Length > best.Length)
            {
                best = usage.Path;
            }
        }

        return best ?? SelectionPath.Root;
    }

    /// <summary>
    /// Finds the enclosing incremental plan for the given incremental plan.
    /// Returns <c>null</c> for top-level incremental plans.
    /// </summary>
    private static IncrementalPlan? ResolveIncrementalPlanParent(
        IncrementalPlan incrementalPlan,
        ImmutableArray<IncrementalPlan> incrementalPlans)
    {
        foreach (var usage in incrementalPlan.DeliveryGroups)
        {
            var ancestor = usage.Parent;
            while (ancestor is not null)
            {
                foreach (var candidate in incrementalPlans)
                {
                    if (ReferenceEquals(candidate, incrementalPlan))
                    {
                        continue;
                    }

                    foreach (var candidateUsage in candidate.DeliveryGroups)
                    {
                        if (ReferenceEquals(candidateUsage, ancestor))
                        {
                            return candidate;
                        }
                    }
                }

                ancestor = ancestor.Parent;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the execution node in <paramref name="owningNodes"/> whose fetch
    /// lands on (or inside) the selection set where this defer is anchored.
    /// The match is the node whose fetch target is the deepest path that is an
    /// ancestor of (or equal to) <paramref name="deferPath"/>, meaning its
    /// output contributes to the enclosing object where the deferred
    /// fragment's fields get merged.
    /// </summary>
    private static int? ResolveDeferParentNodeId(
        ImmutableArray<ExecutionNode> owningNodes,
        SelectionPath deferPath)
    {
        int? match = null;
        var bestDepth = -1;

        for (var i = 0; i < owningNodes.Length; i++)
        {
            SelectionPath target;

            switch (owningNodes[i])
            {
                case OperationExecutionNode op:
                    target = op.Target;
                    break;

                case ApolloOperationExecutionNode apolloOp:
                    target = apolloOp.Target;
                    break;

                default:
                    continue;
            }

            if (!target.IsParentOfOrSame(deferPath))
            {
                continue;
            }

            // Pick the deepest matching node so we attach to the most specific
            // fetch (e.g. a lookup node at $.user rather than a root fetch) when
            // multiple nodes could claim the defer's anchor.
            if (target.Length > bestDepth)
            {
                match = owningNodes[i].Id;
                bestDepth = target.Length;
            }
        }

        return match;
    }

    private static ImmutableList<PlanStep> TransformPlanSteps(
        ImmutableList<PlanStep> planSteps,
        OperationDefinitionNode originalOperation)
    {
        var updatedPlanSteps = planSteps;
        var forwardVariableContext = new ForwardVariableRewriter.Context();

        foreach (var variableDef in originalOperation.VariableDefinitions)
        {
            forwardVariableContext.Variables[variableDef.Variable.Name.Value] = variableDef;
        }

        foreach (var step in planSteps)
        {
            if (step is not OperationPlanStep operationPlanStep)
            {
                continue;
            }

            // Requirement rewriting can leave behind empty child selection sets.
            // We remove them here so later stages do not treat them as real selections.
            operationPlanStep = RemoveEmptySelectionSets(operationPlanStep);

            if (!ReferenceEquals(step, operationPlanStep))
            {
                updatedPlanSteps = updatedPlanSteps.Replace(step, operationPlanStep);
            }

            // Discard steps that have no meaningful selections left.
            if (IsEmptyOperation(operationPlanStep))
            {
                updatedPlanSteps = updatedPlanSteps.Remove(operationPlanStep);
                continue;
            }

            // When every root selection carries a @skip or @include directive,
            // we promote those directives to node-level conditions. This lets
            // the executor skip the entire network call when the condition is
            // not met, rather than sending a request that returns nothing.
            if (operationPlanStep.AreAllProvidedSelectionsConditional())
            {
                var updated = ExtractConditionsAndRewriteSelectionSet(operationPlanStep);
                updatedPlanSteps = updatedPlanSteps.Replace(operationPlanStep, updated);
                operationPlanStep = updated;
            }

            // Strip @defer directives from subgraph operations. The gateway
            // manages deferral itself and subgraphs should not see @defer.
            operationPlanStep = StripDeferDirectivesFromStep(operationPlanStep);

            // Attach variable definitions so the operation is syntactically valid
            // when sent to the downstream service.
            updatedPlanSteps = updatedPlanSteps.Replace(
                operationPlanStep,
                AddVariableDefinitions(operationPlanStep, forwardVariableContext));
        }

        return updatedPlanSteps;

        static bool IsEmptyOperation(OperationPlanStep step)
        {
            if (step.Definition.SelectionSet.Selections.Count == 0)
            {
                return true;
            }

            return step.Definition.SelectionSet.Selections is
            [
#pragma warning disable format
                FieldNode
                {
                    Alias: null,
                    Name.Value: IntrospectionFieldNames.TypeName,
                    Directives: [{ Name.Value: "fusion__empty" }]
                }
#pragma warning restore format
            ];
        }

        static OperationPlanStep RemoveEmptySelectionSets(OperationPlanStep step)
        {
            var updatedDefinition = RemoveEmptySelections(step.Definition);
            return ReferenceEquals(updatedDefinition, step.Definition)
                ? step
                : step with { Definition = updatedDefinition };
        }

        static OperationPlanStep StripDeferDirectivesFromStep(OperationPlanStep step)
        {
            var updated = StripDeferFromSelectionSet(step.Definition.SelectionSet);

            if (ReferenceEquals(updated, step.Definition.SelectionSet))
            {
                return step;
            }

            return step with { Definition = step.Definition.WithSelectionSet(updated) };
        }

        static SelectionSetNode StripDeferFromSelectionSet(SelectionSetNode selectionSet)
        {
            List<ISelectionNode>? rewritten = null;

            for (var i = 0; i < selectionSet.Selections.Count; i++)
            {
                var selection = selectionSet.Selections[i];

                if (selection is InlineFragmentNode inlineFragment)
                {
                    var strippedDirectives = StripDeferDirective(inlineFragment.Directives);
                    var strippedInner = StripDeferFromSelectionSet(inlineFragment.SelectionSet);

                    if (!ReferenceEquals(strippedDirectives, inlineFragment.Directives)
                        || !ReferenceEquals(strippedInner, inlineFragment.SelectionSet))
                    {
                        rewritten ??= [.. selectionSet.Selections];
                        rewritten[i] = inlineFragment
                            .WithDirectives(strippedDirectives)
                            .WithSelectionSet(strippedInner);
                    }
                }
                else if (selection is FieldNode { SelectionSet: not null } field)
                {
                    var strippedInner = StripDeferFromSelectionSet(field.SelectionSet);

                    if (!ReferenceEquals(strippedInner, field.SelectionSet))
                    {
                        rewritten ??= [.. selectionSet.Selections];
                        rewritten[i] = field.WithSelectionSet(strippedInner);
                    }
                }
            }

            return rewritten is null ? selectionSet : new SelectionSetNode(rewritten);
        }

        static IReadOnlyList<DirectiveNode> StripDeferDirective(IReadOnlyList<DirectiveNode> directives)
        {
            for (var i = 0; i < directives.Count; i++)
            {
                if (directives[i].Name.Value.Equals(
                    DirectiveNames.Defer.Name,
                    StringComparison.Ordinal))
                {
                    var result = new List<DirectiveNode>(directives.Count - 1);

                    for (var j = 0; j < directives.Count; j++)
                    {
                        if (j != i)
                        {
                            result.Add(directives[j]);
                        }
                    }

                    return result;
                }
            }

            return directives;
        }

        static OperationPlanStep AddVariableDefinitions(
            OperationPlanStep step,
            ForwardVariableRewriter.Context forwardVariableContext)
        {
            forwardVariableContext.Reset();

            foreach (var (key, requirement) in step.Requirements.OrderBy(t => t.Key))
            {
                forwardVariableContext.Requirements[key] =
                    new VariableDefinitionNode(
                        null,
                        new VariableNode(null, new NameNode(key)),
                        description: null,
                        requirement.Type,
                        null,
                        []);
            }

            var rewrittenNode = s_forwardVariableRewriter.Rewrite(step.Definition, forwardVariableContext);

            if (rewrittenNode is OperationDefinitionNode rewrittenOperationNode
                && !ReferenceEquals(rewrittenOperationNode, step.Definition))
            {
                return step with { Definition = rewrittenOperationNode };
            }

            return step;
        }
    }

    private static void IndexDependencies(
        ImmutableList<PlanStep> planSteps,
        ExecutionPlanBuildContext ctx)
    {
        foreach (var step in planSteps)
        {
            if (step is OperationPlanStep operationPlanStep)
            {
                // Plan steps store which steps they feed into ("dependents").
                // We invert that here so each step knows which steps it
                // depends on, which is what the executor needs for scheduling.
                foreach (var dependent in operationPlanStep.Dependents)
                {
                    if (!ctx.DependenciesByStepId.TryGetValue(dependent, out var dependencies))
                    {
                        dependencies = [];
                        ctx.DependenciesByStepId[dependent] = dependencies;
                    }

                    dependencies.Add(step.Id);
                }
            }
            else if (step is NodeFieldPlanStep nodePlanStep)
            {
                foreach (var (_, dependent) in nodePlanStep.Branches)
                {
                    if (!ctx.DependenciesByStepId.TryGetValue(dependent.Id, out var dependencies))
                    {
                        dependencies = [];
                        ctx.DependenciesByStepId[dependent.Id] = dependencies;
                    }

                    dependencies.Add(nodePlanStep.Id);

                    // In source-schema resolution the branch enriches the entity produced by the
                    // fallback query, so it must run after the fallback query has resolved the
                    // concrete type into the node result.
                    if (nodePlanStep.SourceSchemaResolution)
                    {
                        dependencies.Add(nodePlanStep.FallbackQuery.Id);
                    }
                }

                if (!ctx.DependenciesByStepId.TryGetValue(nodePlanStep.FallbackQuery.Id, out var fallbackDependencies))
                {
                    fallbackDependencies = [];
                    ctx.DependenciesByStepId[nodePlanStep.FallbackQuery.Id] = fallbackDependencies;
                }

                fallbackDependencies.Add(nodePlanStep.Id);

                ctx.BranchesByNodeId.Add(
                    nodePlanStep.Id,
                    nodePlanStep.Branches.ToDictionary(x => x.Key, x => x.Value.Id));
                ctx.FallbackByNodeId.Add(nodePlanStep.Id, nodePlanStep.FallbackQuery.Id);
            }
        }
    }

    private static void BuildExecutionNodes(
        ImmutableList<PlanStep> planSteps,
        ExecutionPlanBuildContext ctx,
        FusionSchemaDefinition schema,
        bool hasVariables,
        CancellationToken cancellationToken)
    {
        var requiresUpload = schema.Types.TryGetType(UploadScalarName, out var uploadType) && uploadType.IsScalarType();
        var readySteps = planSteps.Where(t => !ctx.DependenciesByStepId.ContainsKey(t.Id)).ToList();
        var variableBuffer = hasVariables ? new List<string>() : null;

        while (ctx.ProcessedStepIds.Count < planSteps.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var step in readySteps)
            {
                if (!ctx.ProcessedStepIds.Add(step.Id))
                {
                    continue;
                }

                if (step is OperationPlanStep operationStep)
                {
                    ctx.ExecutionNodes.Add(
                        step.Id,
                        operationStep.EventStreamPlan is null
                            ? CreateOperationExecutionNode(
                                operationStep,
                                schema,
                                requiresUpload,
                                variableBuffer)
                            : CreateEventStreamExecutionNode(operationStep, schema));
                }
                else if (step is NodeFieldPlanStep nodeStep)
                {
                    ctx.ExecutionNodes.Add(step.Id,
                        new NodeFieldExecutionNode(nodeStep.Id, nodeStep.ResponseName, nodeStep.IdValue, nodeStep.Conditions));
                }
                else if (step is PolicyPlanStep policyStep)
                {
                    ctx.ExecutionNodes.Add(step.Id, CreatePolicyExecutionNode(policyStep));
                }
            }

            readySteps.Clear();

            foreach (var step in planSteps)
            {
                if (ctx.DependenciesByStepId.TryGetValue(step.Id, out var stepDependencies)
                    && ctx.ProcessedStepIds.IsSupersetOf(stepDependencies))
                {
                    readySteps.Add(step);
                }
            }

            if (readySteps.Count == 0)
            {
                break;
            }
        }

        // Every plan step must be schedulable. If any remain unprocessed the plan has a cyclic
        // step dependency, which is an internal planner invariant violation rather than a
        // user-facing condition. We fail loudly instead of silently emitting a degenerate plan.
        if (ctx.ProcessedStepIds.Count < planSteps.Count)
        {
            var unschedulableStepIds = new List<int>();

            foreach (var step in planSteps)
            {
                if (!ctx.ProcessedStepIds.Contains(step.Id))
                {
                    unschedulableStepIds.Add(step.Id);
                }
            }

            throw new InvalidOperationException(
                "The execution plan could not be built because the following plan steps have a "
                + $"cyclic dependency and cannot be scheduled: {string.Join(", ", unschedulableStepIds)}.");
        }
    }

    private static ExecutionNode CreateOperationExecutionNode(
        OperationPlanStep operationStep,
        FusionSchemaDefinition schema,
        bool requiresUpload,
        List<string>? variableBuffer)
    {
        var requirements = operationStep.Requirements.IsEmpty
            ? []
            : operationStep.Requirements.OrderBy(t => t.Key).Select(t => t.Value).ToArray();

        var forwardedVariables = Array.Empty<string>();

        if (variableBuffer is not null && operationStep.Definition.VariableDefinitions.Count > 0)
        {
            variableBuffer.Clear();
            var requirementKeys = new HashSet<string>(requirements.Select(r => r.Key));

            foreach (var variableDef in operationStep.Definition.VariableDefinitions)
            {
                var name = variableDef.Variable.Name.Value;

                if (!requirementKeys.Contains(name))
                {
                    variableBuffer.Add(name);
                }
            }

            if (variableBuffer.Count > 0)
            {
                forwardedVariables = variableBuffer.ToArray();
            }
        }

        var requiresFileUpload = requiresUpload
            && DoVariablesContainUploadScalar(operationStep.Definition.VariableDefinitions, schema);

        var operation = RemoveInternalDirectives(operationStep.Definition);
        var operationSource = operation.ToSourceText();

        var selectionSetNode = GetSelectionSetNodeFromPath(operationStep.Definition, operationStep.Source);
        selectionSetNode = PruneNonValueTypeChildren(selectionSetNode, operationStep.Type, schema, operationStep.SchemaName);
        var resultSelectionSet = ResultSelectionSet.Create(
            selectionSetNode,
            schema,
            operationStep.Type,
            operationStep.SchemaName);

        // Only synthetic internal key lookups resolve through _entities. Real public
        // root-field lookups, for example the composed node field, stay native even
        // on Apollo schemas.
        if (operationStep.Lookup is { IsInternal: true } lookup
            && schema.GetSourceSchemaConnectorKind(operationStep.SchemaName ?? lookup.SchemaName)
                == ConnectorKindNames.ApolloFederation)
        {
            if (operationStep.SchemaName is null)
            {
                throw new InvalidOperationException(
                    $"The lookup '{lookup.FieldName}' targets the Apollo Federation source schema "
                    + $"'{lookup.SchemaName}', but the plan step does not specify a concrete source "
                    + "schema name. Apollo Federation lookups cannot be resolved dynamically.");
            }

            var apolloNode = ApolloOperationExecutionNode.Create(
                operationStep.Id,
                operationSource,
                operationStep.SchemaName,
                operationStep.Target,
                requirements,
                forwardedVariables,
                resultSelectionSet,
                operationStep.Conditions,
                requiresFileUpload,
                schema);

            foreach (var parentDependency in operationStep.ParentDependencies)
            {
                apolloNode.AddParentDependency(parentDependency.StepId);
            }

            return apolloNode;
        }

        var node = new OperationExecutionNode(
            operationStep.Id,
            operationSource,
            operationStep.SchemaName,
            operationStep.Target,
            operationStep.Source,
            requirements,
            forwardedVariables,
            resultSelectionSet,
            operationStep.Conditions,
            requiresFileUpload);

        foreach (var parentDependency in operationStep.ParentDependencies)
        {
            node.AddParentDependency(parentDependency.StepId);
        }

        return node;
    }

    private static PolicyExecutionNode CreatePolicyExecutionNode(PolicyPlanStep policyStep)
        => new(
            policyStep.Id,
            policyStep.Targets.ToArray(),
            policyStep.Conditions);

    private static EventStreamExecutionNode CreateEventStreamExecutionNode(
        OperationPlanStep operationStep,
        FusionSchemaDefinition schema)
    {
        var eventStreamPlan = operationStep.EventStreamPlan
            ?? throw new InvalidOperationException("The operation step does not carry event-stream metadata.");

        var selectionSetNode = GetSelectionSetNodeFromPath(operationStep.Definition, operationStep.Source);
        selectionSetNode = PruneNonValueTypeChildren(selectionSetNode, operationStep.Type, schema, operationStep.SchemaName);
        var resultSelectionSet = ResultSelectionSet.Create(
            selectionSetNode,
            schema,
            operationStep.Type,
            operationStep.SchemaName);

        var node = new EventStreamExecutionNode(
            operationStep.Id,
            eventStreamPlan.FieldName,
            operationStep.Target,
            operationStep.Source,
            resultSelectionSet,
            eventStreamPlan.Source,
            eventStreamPlan.Message,
            operationStep.Conditions);

        foreach (var parentDependency in operationStep.ParentDependencies)
        {
            node.AddParentDependency(parentDependency.StepId);
        }

        return node;
    }

    private static void MergeAndBatchOperations(
        ExecutionPlanBuildContext ctx,
        FusionSchemaDefinition schema,
        bool enableRequestGrouping,
        OperationMergePolicy mergePolicy)
    {
        var nodeFieldBoundCache = new Dictionary<int, bool>();
        var mergeResults = MergeStructurallyIdenticalOperations(ctx, nodeFieldBoundCache, mergePolicy);

        // Capture each node's dependency identifiers now, because the batching
        // step below will rewrite the dependency lookup as it merges nodes.
        var originalDependencies = new Dictionary<int, int[]>(ctx.DependenciesByStepId.Count);

        foreach (var (nodeId, dependencies) in ctx.DependenciesByStepId)
        {
            originalDependencies[nodeId] = dependencies.ToArray();
        }

        var perOperationDependencies = GroupBySchemaAndDepthIntoBatches(
            ctx, nodeFieldBoundCache, mergeResults, originalDependencies, enableRequestGrouping);

        foreach (var (batchNode, memberDependencies) in GroupApolloLookupsIntoBatches(
            ctx, schema, nodeFieldBoundCache, originalDependencies, enableRequestGrouping))
        {
            perOperationDependencies.Add(batchNode, memberDependencies);
        }

        WrapRemainingMergedOperations(ctx, mergeResults, perOperationDependencies, originalDependencies);
        WirePerOperationDependencies(ctx, perOperationDependencies);
    }

    /// <summary>
    /// Finds query operations that are structurally identical and merges
    /// them into a single <see cref="BatchOperationDefinition"/>. This
    /// reduces the number of network requests the executor has to send.
    /// </summary>
    private static Dictionary<int, MergeResult> MergeStructurallyIdenticalOperations(
        ExecutionPlanBuildContext ctx,
        Dictionary<int, bool> nodeFieldBoundCache,
        OperationMergePolicy mergePolicy)
    {
        var candidates = new Dictionary<string, List<OperationExecutionNode>>(StringComparer.Ordinal);

        foreach (var node in ctx.ExecutionNodes.Values.OfType<OperationExecutionNode>())
        {
            if (node.Operation.Type != OperationType.Query)
            {
                continue;
            }

            if (IsNodeFieldBound(node.Id, ctx, nodeFieldBoundCache))
            {
                continue;
            }

            var signature = ComputeCanonicalSignature(node);

            if (!candidates.TryGetValue(signature, out var list))
            {
                list = [];
                candidates[signature] = list;
            }

            list.Add(node);
        }

        var mergeResults = new Dictionary<int, MergeResult>();

        foreach (var (_, equivalentNodes) in candidates)
        {
            if (equivalentNodes.Count <= 1)
            {
                continue;
            }

            foreach (var group in PartitionIntoMergeableGroups(
                equivalentNodes, ctx.DependenciesByStepId, mergePolicy))
            {
                if (group.Count <= 1)
                {
                    continue;
                }

                group.Sort((a, b) => a.Id.CompareTo(b.Id));

                var primary = group[0];
                var (canonicalOp, canonicalRequirements) = CanonicalizeOperation(primary);
                var targets = new SelectionPath[group.Count];

                for (var i = 0; i < group.Count; i++)
                {
                    targets[i] = group[i].Target;
                }

                mergeResults[primary.Id] = new MergeResult(
                    targets, canonicalOp, canonicalRequirements, primary);

                AbsorbMergedNodes(ctx, primary.Id, group);
            }
        }

        return mergeResults;
    }

    /// <summary>
    /// Removes merged nodes from the execution graph and folds their
    /// dependencies into the primary node that represents them all.
    /// </summary>
    private static void AbsorbMergedNodes(
        ExecutionPlanBuildContext ctx,
        int primaryId,
        List<OperationExecutionNode> group)
    {
        var absorbedIds = new HashSet<int>(group.Count - 1);

        if (!ctx.DependenciesByStepId.TryGetValue(primaryId, out var primaryDeps))
        {
            primaryDeps = [];
        }

        for (var i = 1; i < group.Count; i++)
        {
            var otherId = group[i].Id;
            absorbedIds.Add(otherId);
            ctx.ExecutionNodes.Remove(otherId);
            ctx.RedirectedStepIds[otherId] = primaryId;

            if (ctx.DependenciesByStepId.TryGetValue(otherId, out var otherDependencies))
            {
                foreach (var dependency in otherDependencies)
                {
                    primaryDeps.Add(dependency);
                }

                ctx.DependenciesByStepId.Remove(otherId);
            }
        }

        if (primaryDeps.Count > 0)
        {
            ctx.DependenciesByStepId[primaryId] = primaryDeps;
        }
        else
        {
            ctx.DependenciesByStepId.Remove(primaryId);
        }

        RedirectDependencyReferences(ctx.DependenciesByStepId, absorbedIds, primaryId);
    }

    /// <summary>
    /// Groups query nodes by their target schema and dependency depth into
    /// batch execution nodes. Nodes at the same depth targeting the same
    /// source schema are independent of each other, so the executor can
    /// send them together in a single batched network request.
    /// </summary>
    private static Dictionary<ExecutionNode, Dictionary<int, int[]>>
        GroupBySchemaAndDepthIntoBatches(
            ExecutionPlanBuildContext ctx,
            Dictionary<int, bool> nodeFieldBoundCache,
            Dictionary<int, MergeResult> mergeResults,
            Dictionary<int, int[]> originalDependencies,
            bool enableRequestGrouping)
    {
        var consumedMergeIds = new HashSet<int>();
        var perOperationDependencies = new Dictionary<ExecutionNode, Dictionary<int, int[]>>();

        if (!enableRequestGrouping)
        {
            return perOperationDependencies;
        }

        var queryNodes = ctx.ExecutionNodes.Values
            .OfType<OperationExecutionNode>()
            .Where(n => n.Operation.Type == OperationType.Query)
            .Where(n => !IsNodeFieldBound(n.Id, ctx, nodeFieldBoundCache))
            .ToList();

        var depthLookup = new Dictionary<int, int>();
        var recursionStack = new HashSet<int>();

        foreach (var node in queryNodes)
        {
            GetDependencyDepth(node.Id, ctx.DependenciesByStepId, depthLookup, recursionStack);
        }

        var batchGroups = new Dictionary<(string schema, int depth), List<OperationExecutionNode>>();

        foreach (var node in queryNodes)
        {
            var schemaKey = node.SchemaName ?? DynamicSchemaNameMarker;
            var depth = depthLookup.TryGetValue(node.Id, out var d) ? d : 0;
            var key = (schemaKey, depth);

            if (!batchGroups.TryGetValue(key, out var group))
            {
                group = [];
                batchGroups[key] = group;
            }

            group.Add(node);
        }

        // Process from shallowest to deepest so that deeper groups
        // reference the already-redirected identifiers from earlier merges.
        foreach (var (_, groupMembers) in batchGroups.OrderBy(t => t.Key.depth))
        {
            if (groupMembers.Count <= 1)
            {
                continue;
            }

            groupMembers.Sort((a, b) => a.Id.CompareTo(b.Id));

            var operations = new List<OperationDefinition>();

            foreach (var member in groupMembers)
            {
                if (mergeResults.TryGetValue(member.Id, out var merge))
                {
                    consumedMergeIds.Add(member.Id);
                    operations.Add(CreateBatchOperationDefinition(merge));
                }
                else
                {
                    operations.Add(CreateSingleOperationDefinition(member));
                }
            }

            var lowestId = groupMembers[0].Id;
            var batchNode = new OperationBatchExecutionNode(lowestId, operations.ToArray());

            // Save each member's dependencies before replacing the individual
            // nodes, because the replacement will remove them from the lookup.
            var memberDependencies = new Dictionary<int, int[]>();

            foreach (var member in groupMembers)
            {
                if (originalDependencies.TryGetValue(member.Id, out var memberDeps))
                {
                    memberDependencies[member.Id] = memberDeps;
                }
            }

            ReplaceMembersWithBatchNode(ctx, groupMembers, batchNode, lowestId);
            perOperationDependencies[batchNode] = memberDependencies;
        }

        // Remove consumed merge results so the caller knows which ones still
        // need to be wrapped as standalone batch nodes.
        foreach (var id in consumedMergeIds)
        {
            mergeResults.Remove(id);
        }

        return perOperationDependencies;
    }

    /// <summary>
    /// Groups Apollo Federation entity lookups by their target schema and
    /// dependency depth into batch execution nodes. Lookups at the same depth
    /// targeting the same source schema are independent of each other, so the
    /// executor can send them together in a single batched network request.
    /// </summary>
    private static Dictionary<ExecutionNode, Dictionary<int, int[]>>
        GroupApolloLookupsIntoBatches(
            ExecutionPlanBuildContext ctx,
            FusionSchemaDefinition schema,
            Dictionary<int, bool> nodeFieldBoundCache,
            Dictionary<int, int[]> originalDependencies,
            bool enableRequestGrouping)
    {
        var perOperationDependencies = new Dictionary<ExecutionNode, Dictionary<int, int[]>>();

        if (!enableRequestGrouping)
        {
            return perOperationDependencies;
        }

        var lookupNodes = ctx.ExecutionNodes.Values
            .OfType<ApolloOperationExecutionNode>()
            .Where(n => n.Operation.Type == OperationType.Query)
            .Where(n => !IsNodeFieldBound(n.Id, ctx, nodeFieldBoundCache))
            .ToList();

        var depthLookup = new Dictionary<int, int>();
        var recursionStack = new HashSet<int>();

        foreach (var node in lookupNodes)
        {
            GetDependencyDepth(node.Id, ctx.DependenciesByStepId, depthLookup, recursionStack);
        }

        var batchGroups = new Dictionary<(string schema, int depth), List<ApolloOperationExecutionNode>>();

        foreach (var node in lookupNodes)
        {
            // Apollo lookup nodes always carry a concrete schema name because
            // the routing in CreateOperationExecutionNode rejects dynamic ones.
            var depth = depthLookup.TryGetValue(node.Id, out var d) ? d : 0;
            var key = (node.SchemaName!, depth);

            if (!batchGroups.TryGetValue(key, out var group))
            {
                group = [];
                batchGroups[key] = group;
            }

            group.Add(node);
        }

        // Process from shallowest to deepest so that deeper groups
        // reference the already-redirected identifiers from earlier merges.
        foreach (var (_, groupMembers) in batchGroups.OrderBy(t => t.Key.depth))
        {
            if (groupMembers.Count <= 1)
            {
                continue;
            }

            groupMembers.Sort((a, b) => a.Id.CompareTo(b.Id));

            var operations = new SingleOperationDefinition[groupMembers.Count];

            for (var i = 0; i < groupMembers.Count; i++)
            {
                operations[i] = CreateApolloSingleOperationDefinition(groupMembers[i]);
            }

            var lowestId = groupMembers[0].Id;
            var batchNode = ApolloOperationBatchExecutionNode.Create(lowestId, operations, schema);

            // Save each member's dependencies before replacing the individual
            // nodes, because the replacement will remove them from the lookup.
            var memberDependencies = new Dictionary<int, int[]>();

            foreach (var member in groupMembers)
            {
                if (originalDependencies.TryGetValue(member.Id, out var memberDeps))
                {
                    memberDependencies[member.Id] = memberDeps;
                }
            }

            ReplaceMembersWithBatchNode(ctx, groupMembers, batchNode, lowestId);
            perOperationDependencies[batchNode] = memberDependencies;
        }

        return perOperationDependencies;
    }

    /// <summary>
    /// Wraps merged operations that were not included in any multi-member
    /// batch group into standalone batch execution nodes.
    /// </summary>
    private static void WrapRemainingMergedOperations(
        ExecutionPlanBuildContext ctx,
        Dictionary<int, MergeResult> remainingMerges,
        Dictionary<ExecutionNode, Dictionary<int, int[]>> perOperationDependencies,
        Dictionary<int, int[]> originalDependencies)
    {
        foreach (var (primaryId, merge) in remainingMerges)
        {
            var operationDefinition = CreateBatchOperationDefinition(merge);
            var standaloneBatchNode = new OperationBatchExecutionNode(primaryId, [operationDefinition]);
            ctx.ExecutionNodes[primaryId] = standaloneBatchNode;

            perOperationDependencies[standaloneBatchNode] =
                new Dictionary<int, int[]>
                {
                    [operationDefinition.Id] = originalDependencies.TryGetValue(primaryId, out var primaryDeps)
                        ? primaryDeps
                        : []
                };
        }
    }

    /// <summary>
    /// Connects each inner operation inside a batch node to the upstream
    /// operation definitions it depends on. This gives the executor
    /// fine-grained visibility into per-operation readiness.
    /// </summary>
    private static void WirePerOperationDependencies(
        ExecutionPlanBuildContext ctx,
        Dictionary<ExecutionNode, Dictionary<int, int[]>> perOperationDependencies)
    {
        if (perOperationDependencies.Count == 0)
        {
            return;
        }

        var planNodeById = new Dictionary<int, IOperationPlanNode>();

        foreach (var node in ctx.ExecutionNodes.Values)
        {
            planNodeById[node.Id] = node;

            if (node is OperationBatchExecutionNode batch)
            {
                foreach (var operation in batch.Operations)
                {
                    planNodeById[operation.Id] = operation;
                }
            }

            if (node is ApolloOperationBatchExecutionNode apolloBatch)
            {
                foreach (var operation in apolloBatch.Operations)
                {
                    planNodeById[operation.Id] = operation;
                }
            }
        }

        foreach (var (_, memberDependencies) in perOperationDependencies)
        {
            foreach (var (operationId, dependencyIds) in memberDependencies)
            {
                if (planNodeById.TryGetValue(operationId, out var operationNode)
                    && operationNode is OperationDefinition operationDefinition)
                {
                    foreach (var dependencyId in dependencyIds)
                    {
                        if (planNodeById.TryGetValue(dependencyId, out var dependencyNode))
                        {
                            operationDefinition.AddDependency(dependencyNode);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Replaces individual member nodes in the execution graph with a single
    /// batch node, merging all of their dependencies into the batch node.
    /// </summary>
    private static void ReplaceMembersWithBatchNode(
        ExecutionPlanBuildContext ctx,
        IReadOnlyList<ExecutionNode> members,
        ExecutionNode batchNode,
        int batchNodeId)
    {
        var batchDependencies = new HashSet<int>();
        var memberIds = new HashSet<int>(members.Count);

        foreach (var member in members)
        {
            memberIds.Add(member.Id);
            ctx.ExecutionNodes.Remove(member.Id);
            ctx.RedirectedStepIds[member.Id] = batchNodeId;

            if (ctx.DependenciesByStepId.TryGetValue(member.Id, out var memberDependencies))
            {
                foreach (var dependency in memberDependencies)
                {
                    batchDependencies.Add(dependency);
                }

                ctx.DependenciesByStepId.Remove(member.Id);
            }
        }

        ctx.ExecutionNodes[batchNodeId] = batchNode;

        if (batchDependencies.Count > 0)
        {
            ctx.DependenciesByStepId[batchNodeId] = batchDependencies;
        }

        RedirectDependencyReferences(ctx.DependenciesByStepId, memberIds, batchNodeId);
    }

    private static int ResolveRedirectedStepId(int id, Dictionary<int, int> redirectedStepIds)
    {
        var current = id;
        var visited = new HashSet<int>();

        while (redirectedStepIds.TryGetValue(current, out var next) && visited.Add(current))
        {
            current = next;
        }

        return current;
    }

    private static BatchOperationDefinition CreateBatchOperationDefinition(MergeResult merge)
    {
        var primary = merge.Primary;

        var definition = new BatchOperationDefinition(
            primary.Id,
            merge.CanonicalOp,
            primary.SchemaName,
            merge.Targets,
            primary.Source,
            merge.CanonicalRequirements,
            primary.ForwardedVariables.ToArray(),
            primary.ResultSelectionSet,
            primary.Conditions.ToArray(),
            primary.RequiresFileUpload);

        foreach (var parentDependency in primary.BufferedParentDependencies)
        {
            definition.AddParentDependency(parentDependency);
        }

        return definition;
    }

    private static SingleOperationDefinition CreateSingleOperationDefinition(OperationExecutionNode member)
    {
        var definition = new SingleOperationDefinition(
            member.Id,
            member.Operation,
            member.SchemaName,
            member.Target,
            member.Source,
            member.Requirements.ToArray(),
            member.ForwardedVariables.ToArray(),
            member.ResultSelectionSet,
            member.Conditions.ToArray(),
            member.RequiresFileUpload);

        foreach (var parentDependency in member.BufferedParentDependencies)
        {
            definition.AddParentDependency(parentDependency);
        }

        return definition;
    }

    private static SingleOperationDefinition CreateApolloSingleOperationDefinition(
        ApolloOperationExecutionNode member)
    {
        // The definition carries the lookup operation rather than the rewritten
        // _entities operation because the batch node rewrites each definition
        // itself when it is created.
        var definition = new SingleOperationDefinition(
            member.Id,
            member.LookupOperation,
            member.SchemaName,
            member.Target,
            member.Source,
            member.Requirements.ToArray(),
            member.ForwardedVariables.ToArray(),
            member.ResultSelectionSet,
            member.Conditions.ToArray(),
            member.RequiresFileUpload);

        foreach (var parentDependency in member.BufferedParentDependencies)
        {
            definition.AddParentDependency(parentDependency);
        }

        return definition;
    }

    /// <summary>
    /// Rewrites the dependency graph so that every reference to any of
    /// <paramref name="oldIds"/> points to <paramref name="newId"/> instead.
    /// </summary>
    private static void RedirectDependencyReferences(
        Dictionary<int, HashSet<int>> dependenciesByStepId,
        HashSet<int> oldIds,
        int newId)
    {
        foreach (var depSet in dependenciesByStepId.Values)
        {
            var hadOld = false;

            foreach (var oldId in oldIds)
            {
                if (depSet.Remove(oldId))
                {
                    hadOld = true;
                }
            }

            if (hadOld)
            {
                depSet.Add(newId);
            }
        }
    }

    /// <summary>
    /// Checks whether a node is transitively dependent on a
    /// <see cref="NodeFieldExecutionNode"/>. Operations beneath a node-field
    /// dispatch must keep their original identifiers because the dispatch
    /// logic references them by identifier to select the correct branch.
    /// </summary>
    private static bool IsNodeFieldBound(
        int nodeId,
        ExecutionPlanBuildContext ctx,
        Dictionary<int, bool> cache)
    {
        if (cache.TryGetValue(nodeId, out var cached))
        {
            return cached;
        }

        if (!ctx.DependenciesByStepId.TryGetValue(nodeId, out var dependencies) || dependencies.Count == 0)
        {
            cache[nodeId] = false;
            return false;
        }

        foreach (var dependencyId in dependencies)
        {
            if (ctx.ExecutionNodes.TryGetValue(dependencyId, out var dependencyNode)
                && dependencyNode is NodeFieldExecutionNode)
            {
                cache[nodeId] = true;
                return true;
            }

            if (IsNodeFieldBound(dependencyId, ctx, cache))
            {
                cache[nodeId] = true;
                return true;
            }
        }

        cache[nodeId] = false;
        return false;
    }

    private static void WireExecutionDependencies(ExecutionPlanBuildContext ctx)
    {
        WireOperationDependencies(ctx);
        WireNodeFieldBranchesAndFallbacks(ctx);
    }

    private static Dictionary<int, HashSet<int>> AddPolicyRequirementDependencies(
        ExecutionPlanBuildContext ctx)
    {
        var providersByPolicyNodeId = new Dictionary<int, HashSet<int>>();

        foreach (var policyNode in ctx.ExecutionNodes.Values.OfType<PolicyExecutionNode>())
        {
            List<(string PolicyName, string[] Path)>? requiredPaths = null;

            foreach (var target in policyNode.Targets)
            {
                var entityPath = target.Kind is PolicyTargetKind.Field
                    ? target.Path.Parent ?? SelectionPath.Root
                    : target.Path;

                foreach (var requirement in target.Requirements)
                {
                    requiredPaths ??= [];
                    AddRequirementPaths(
                        requirement.PolicyName,
                        entityPath,
                        requirement.SelectionSet,
                        requiredPaths);
                }
            }

            if (requiredPaths is null)
            {
                continue;
            }

            if (!ctx.DependenciesByStepId.TryGetValue(policyNode.Id, out var dependencies))
            {
                dependencies = [];
                ctx.DependenciesByStepId.Add(policyNode.Id, dependencies);
            }

            var providers = new HashSet<int>();

            foreach (var (policyName, path) in requiredPaths)
            {
                var isProvided = false;

                foreach (var candidate in ctx.ExecutionNodes.Values)
                {
                    ResultSelectionSet resultSelectionSet;
                    SelectionPath target;

                    switch (candidate)
                    {
                        case OperationExecutionNode operation:
                            resultSelectionSet = operation.ResultSelectionSet;
                            target = operation.Target;
                            break;

                        case ApolloOperationExecutionNode operation:
                            resultSelectionSet = operation.ResultSelectionSet;
                            target = operation.Target;
                            break;

                        default:
                            continue;
                    }

                    if (!TryProvidesRequirement(
                        resultSelectionSet,
                        target,
                        path))
                    {
                        continue;
                    }

                    dependencies.Add(candidate.Id);
                    providers.Add(candidate.Id);
                    isProvided = true;
                }

                if (!isProvided)
                {
                    throw new InvalidOperationException(
                        $"Authorization policy '{policyName}' requires field '{FormatPath(path)}', "
                        + "but the execution plan does not provide it.");
                }
            }

            providersByPolicyNodeId.Add(policyNode.Id, providers);
        }

        return providersByPolicyNodeId;

        static void AddRequirementPaths(
            string policyName,
            SelectionPath entityPath,
            SelectionSetNode requirements,
            List<(string PolicyName, string[] Path)> paths)
        {
            var segments = new List<string>(entityPath.Length + 4);

            for (var i = 0; i < entityPath.Length; i++)
            {
                if (entityPath[i].Kind is SelectionPathSegmentKind.Field)
                {
                    segments.Add(entityPath[i].Name);
                }
            }

            AddRequirementLeafPaths(policyName, requirements, segments, paths);
        }

        static void AddRequirementLeafPaths(
            string policyName,
            SelectionSetNode requirements,
            List<string> segments,
            List<(string PolicyName, string[] Path)> paths)
        {
            foreach (var selection in requirements.Selections)
            {
                if (selection is not FieldNode field)
                {
                    throw new InvalidOperationException(
                        $"Authorization policy '{policyName}' has an unsupported requirement selection.");
                }

                segments.Add(field.Alias?.Value ?? field.Name.Value);

                if (field.SelectionSet is { } childSelectionSet)
                {
                    AddRequirementLeafPaths(policyName, childSelectionSet, segments, paths);
                }
                else
                {
                    paths.Add((policyName, segments.ToArray()));
                }

                segments.RemoveAt(segments.Count - 1);
            }
        }

        static bool TryProvidesRequirement(
            ResultSelectionSet resultSelectionSet,
            SelectionPath operationTarget,
            string[] requirementPath)
        {
            var targetFieldCount = 0;

            for (var i = 0; i < operationTarget.Length; i++)
            {
                if (operationTarget[i].Kind is SelectionPathSegmentKind.Field)
                {
                    targetFieldCount++;
                }
            }

            if (targetFieldCount > requirementPath.Length)
            {
                return false;
            }

            var targetFieldIndex = 0;

            for (var i = 0; i < operationTarget.Length; i++)
            {
                var segment = operationTarget[i];

                if (segment.Kind is SelectionPathSegmentKind.Field
                    && !segment.Name.Equals(
                        requirementPath[targetFieldIndex++],
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }

            for (var i = targetFieldCount; i < requirementPath.Length; i++)
            {
                var responseName = requirementPath[i];

                if (!ContainsResponseName(resultSelectionSet.ResponseNames, responseName))
                {
                    return false;
                }

                if (i + 1 < requirementPath.Length)
                {
                    if (resultSelectionSet.TryGetChild(responseName) is not { } child)
                    {
                        // A leaf result selection copies the complete field value, including
                        // any nested policy requirement data returned by the source operation.
                        return true;
                    }

                    resultSelectionSet = child;
                }
            }

            return true;
        }

        static bool ContainsResponseName(ReadOnlySpan<string> responseNames, string responseName)
        {
            foreach (var candidate in responseNames)
            {
                if (candidate.Equals(responseName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        static string FormatPath(string[] path)
            => path.Length == 0 ? "$" : "$." + string.Join('.', path);
    }

    private static Dictionary<int, HashSet<int>> CreatePolicyGuardLookup(
        ExecutionPlanBuildContext ctx,
        IReadOnlyDictionary<int, HashSet<int>> policyRequirementProviders)
    {
        var guardsByOperationId = new Dictionary<int, HashSet<int>>();

        foreach (var policyNode in ctx.ExecutionNodes.Values.OfType<PolicyExecutionNode>())
        {
            if (!ctx.DependenciesByStepId.TryGetValue(policyNode.Id, out var producerIds))
            {
                continue;
            }

            foreach (var candidate in ctx.ExecutionNodes.Values)
            {
                if (policyRequirementProviders.TryGetValue(policyNode.Id, out var providers)
                    && providers.Contains(candidate.Id))
                {
                    continue;
                }

                SelectionPath target;

                switch (candidate)
                {
                    case OperationExecutionNode operation:
                        target = operation.Target;
                        break;

                    case ApolloOperationExecutionNode operation:
                        target = operation.Target;
                        break;

                    default:
                        continue;
                }

                if (!ctx.DependenciesByStepId.TryGetValue(candidate.Id, out var dependencies)
                    || !dependencies.Overlaps(producerIds)
                    || !IsGuardedTarget(target, policyNode.Targets))
                {
                    continue;
                }

                if (!guardsByOperationId.TryGetValue(candidate.Id, out var guards))
                {
                    guards = [];
                    guardsByOperationId.Add(candidate.Id, guards);
                }

                guards.Add(policyNode.Id);
            }
        }

        return guardsByOperationId;

        static bool IsGuardedTarget(
            SelectionPath operationTarget,
            ReadOnlySpan<PolicyExecutionTarget> policyTargets)
        {
            foreach (var policyTarget in policyTargets)
            {
                if (policyTarget.Path.IsParentOfOrSame(operationTarget))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private static void ApplyPolicyGuards(
        ExecutionPlanBuildContext ctx,
        Dictionary<int, HashSet<int>> guardsByOperationId)
    {
        if (guardsByOperationId.Count == 0)
        {
            return;
        }

        var executionNodeByOperationId = new Dictionary<int, ExecutionNode>();
        var definitionByOperationId = new Dictionary<int, OperationDefinition>();

        foreach (var node in ctx.ExecutionNodes.Values)
        {
            executionNodeByOperationId[node.Id] = node;

            if (node is OperationBatchExecutionNode batch)
            {
                foreach (var operation in batch.Operations)
                {
                    executionNodeByOperationId[operation.Id] = batch;
                    definitionByOperationId[operation.Id] = operation;
                }
            }

            if (node is ApolloOperationBatchExecutionNode apolloBatch)
            {
                foreach (var operation in apolloBatch.Operations)
                {
                    executionNodeByOperationId[operation.Id] = apolloBatch;
                    definitionByOperationId[operation.Id] = operation;
                }
            }
        }

        foreach (var (originalOperationId, policyIds) in guardsByOperationId)
        {
            var operationId = originalOperationId;

            if (!executionNodeByOperationId.TryGetValue(operationId, out var executionNode))
            {
                operationId = ResolveRedirectedStepId(operationId, ctx.RedirectedStepIds);

                if (!executionNodeByOperationId.TryGetValue(operationId, out executionNode))
                {
                    continue;
                }
            }

            definitionByOperationId.TryGetValue(originalOperationId, out var operationDefinition);
            operationDefinition ??= definitionByOperationId.GetValueOrDefault(operationId);

            if (!ctx.DependenciesByStepId.TryGetValue(executionNode.Id, out var executionDependencies))
            {
                executionDependencies = [];
                ctx.DependenciesByStepId.Add(executionNode.Id, executionDependencies);
            }

            foreach (var policyId in policyIds)
            {
                if (!ctx.ExecutionNodes.TryGetValue(policyId, out var policyNode))
                {
                    continue;
                }

                executionDependencies.Add(policyId);
                operationDefinition?.AddDependency(policyNode);
            }
        }
    }

    private static void WireOperationDependencies(ExecutionPlanBuildContext ctx)
    {
        // Build a lookup from every operation identifier to its containing
        // execution node. A batch node wraps several operations, so each
        // inner operation identifier also maps back to the parent batch node.
        var executionNodeById = new Dictionary<int, ExecutionNode>();

        foreach (var node in ctx.ExecutionNodes.Values)
        {
            executionNodeById[node.Id] = node;

            if (node is OperationBatchExecutionNode batch)
            {
                foreach (var operation in batch.Operations)
                {
                    executionNodeById[operation.Id] = batch;
                }
            }

            if (node is ApolloOperationBatchExecutionNode apolloBatch)
            {
                foreach (var operation in apolloBatch.Operations)
                {
                    executionNodeById[operation.Id] = apolloBatch;
                }
            }
        }

        foreach (var (nodeId, stepDependencies) in ctx.DependenciesByStepId)
        {
            if (!ctx.ExecutionNodes.TryGetValue(nodeId, out var entry)
                || entry is not (
                    OperationExecutionNode
                    or OperationBatchExecutionNode
                    or ApolloOperationExecutionNode
                    or ApolloOperationBatchExecutionNode
                    or PolicyExecutionNode))
            {
                continue;
            }

            if (entry is OperationBatchExecutionNode batchEntry)
            {
                WireBatchNodeDependencies(
                    batchEntry, batchEntry.Operations.Length, stepDependencies, executionNodeById);
                continue;
            }

            if (entry is ApolloOperationBatchExecutionNode apolloBatchEntry)
            {
                WireBatchNodeDependencies(
                    apolloBatchEntry, apolloBatchEntry.Operations.Length, stepDependencies, executionNodeById);
                continue;
            }

            // For a standalone execution node, attach dependencies directly.
            foreach (var dependencyId in stepDependencies)
            {
                if (!ctx.ExecutionNodes.TryGetValue(dependencyId, out var childEntry)
                    || childEntry is not (
                        OperationExecutionNode
                        or OperationBatchExecutionNode
                        or ApolloOperationExecutionNode
                        or ApolloOperationBatchExecutionNode
                        or NodeFieldExecutionNode
                        or EventStreamExecutionNode
                        or PolicyExecutionNode))
                {
                    continue;
                }

                childEntry.AddDependent(entry);
                entry.AddDependency(childEntry);
            }
        }
    }

    private static void WireBatchNodeDependencies(
        ExecutionNode batchEntry,
        int operationCount,
        HashSet<int> stepDependencies,
        Dictionary<int, ExecutionNode> executionNodeById)
    {
        var seenExecutionDependencies = new HashSet<int>();

        foreach (var dependencyId in stepDependencies)
        {
            if (dependencyId == batchEntry.Id)
            {
                continue;
            }

            if (!executionNodeById.TryGetValue(dependencyId, out var dependencyExecutionNode)
                || dependencyExecutionNode.Id == batchEntry.Id)
            {
                continue;
            }

            if (!seenExecutionDependencies.Add(dependencyExecutionNode.Id))
            {
                continue;
            }

            dependencyExecutionNode.AddDependent(batchEntry);

            // When a batch holds multiple operations, the dependency is
            // optional. The executor evaluates each operation individually
            // and only waits for the specific upstream results it needs.
            if (operationCount > 1)
            {
                batchEntry.AddOptionalDependency(dependencyExecutionNode);
            }
            else
            {
                batchEntry.AddDependency(dependencyExecutionNode);
            }
        }
    }

    private static void WireNodeFieldBranchesAndFallbacks(ExecutionPlanBuildContext ctx)
    {
        foreach (var (nodeId, branches) in ctx.BranchesByNodeId)
        {
            if (!ctx.ExecutionNodes.TryGetValue(nodeId, out var entry) || entry is not NodeFieldExecutionNode node)
            {
                continue;
            }

            foreach (var (typeName, branchNodeId) in branches)
            {
                if (ctx.ExecutionNodes.TryGetValue(branchNodeId, out var branchNode))
                {
                    node.AddBranch(typeName, branchNode);
                }
            }
        }

        foreach (var (nodeId, fallbackNodeId) in ctx.FallbackByNodeId)
        {
            if (!ctx.ExecutionNodes.TryGetValue(nodeId, out var entry) || entry is not NodeFieldExecutionNode node)
            {
                continue;
            }

            if (ctx.ExecutionNodes.TryGetValue(fallbackNodeId, out var fallbackNode))
            {
                node.AddFallbackQuery(fallbackNode);
            }
        }
    }

    internal static Dictionary<int, int> CreateBatchingGroupLookup(
        ImmutableList<PlanStep> planSteps,
        Dictionary<int, HashSet<int>> dependencyLookup,
        bool enableRequestGrouping)
    {
        if (!enableRequestGrouping)
        {
            return [];
        }

        var queryStepsByService = new Dictionary<string, List<OperationPlanStep>>(StringComparer.Ordinal);

        foreach (var operationStep in planSteps.OfType<OperationPlanStep>())
        {
            if (operationStep.Definition.Operation is not OperationType.Query)
            {
                continue;
            }

            var schemaKey = operationStep.SchemaName ?? DynamicSchemaNameMarker;

            if (!queryStepsByService.TryGetValue(schemaKey, out var serviceSteps))
            {
                serviceSteps = [];
                queryStepsByService[schemaKey] = serviceSteps;
            }

            serviceSteps.Add(operationStep);
        }

        if (queryStepsByService.Count == 0)
        {
            return [];
        }

        var dependencyDepthLookup = new Dictionary<int, int>();
        var recursionStack = new HashSet<int>();

        foreach (var serviceSteps in queryStepsByService.Values)
        {
            foreach (var step in serviceSteps)
            {
                GetDependencyDepth(
                    step.Id,
                    dependencyLookup,
                    dependencyDepthLookup,
                    recursionStack);
            }
        }

        var lookup = new Dictionary<int, int>();
        var nextGroupId = 0;

        foreach (var (_, serviceSteps) in queryStepsByService.OrderBy(t => t.Key, StringComparer.Ordinal))
        {
            var stepsByDepth = new Dictionary<int, List<int>>();

            foreach (var step in serviceSteps)
            {
                var depth = dependencyDepthLookup.TryGetValue(step.Id, out var currentDepth)
                    ? currentDepth
                    : 0;

                if (!stepsByDepth.TryGetValue(depth, out var groupedSteps))
                {
                    groupedSteps = [];
                    stepsByDepth.Add(depth, groupedSteps);
                }

                groupedSteps.Add(step.Id);
            }

            foreach (var groupedSteps in stepsByDepth.OrderBy(t => t.Key).Select(t => t.Value))
            {
                if (groupedSteps.Count <= 1)
                {
                    continue;
                }

                groupedSteps.Sort();
                var groupId = ++nextGroupId;

                foreach (var stepId in groupedSteps)
                {
                    lookup.Add(stepId, groupId);
                }
            }
        }

        return lookup;
    }

    private static int GetDependencyDepth(
        int stepId,
        Dictionary<int, HashSet<int>> dependenciesByStepId,
        Dictionary<int, int> depthLookup,
        HashSet<int> recursionStack)
    {
        if (depthLookup.TryGetValue(stepId, out var depth))
        {
            return depth;
        }

        if (!dependenciesByStepId.TryGetValue(stepId, out var directDependencies) || directDependencies.Count == 0)
        {
            depthLookup[stepId] = 0;
            return 0;
        }

        if (!recursionStack.Add(stepId))
        {
            throw new InvalidOperationException("The execution dependency graph contains a cycle.");
        }

        var maxDepth = 0;

        foreach (var dependency in directDependencies.OrderBy(t => t))
        {
            var dependencyDepth = GetDependencyDepth(
                dependency,
                dependenciesByStepId,
                depthLookup,
                recursionStack);
            maxDepth = Math.Max(maxDepth, dependencyDepth + 1);
        }

        recursionStack.Remove(stepId);
        depthLookup[stepId] = maxDepth;
        return maxDepth;
    }

    private static string ComputeCanonicalSignature(OperationExecutionNode node)
    {
        var replacements = BuildPrefixReplacements(node.Requirements);
        var normalizedText = ApplyPrefixReplacements(node.Operation.SourceText, replacements);

        // The first line contains the operation name, which embeds a
        // step-specific identifier. We skip it so that two operations
        // with the same structure produce the same signature.
        var firstNewline = normalizedText.IndexOf('\n');
        var bodyText = firstNewline >= 0 ? normalizedText[(firstNewline + 1)..] : normalizedText;

        var conditions = string.Join(",", node.Conditions.ToArray()
            .OrderBy(c => c.VariableName)
            .Select(c => $"{c.VariableName}:{c.PassingValue}"));

        return $"{node.SchemaName}|{node.Source}|{conditions}|{bodyText}";
    }

    private static (OperationSourceText operation, OperationRequirement[] requirements) CanonicalizeOperation(
        OperationExecutionNode node)
    {
        return (node.Operation, node.Requirements.ToArray());
    }

    /// <summary>
    /// Builds replacement pairs that normalize step-specific
    /// <c>__fusion_{N}_</c> variable name prefixes into a canonical
    /// form, so structurally identical operations produce matching text.
    /// </summary>
    private static (string original, string canonical)[] BuildPrefixReplacements(
        ReadOnlySpan<OperationRequirement> requirements)
    {
        var prefixToArgs = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        foreach (var req in requirements)
        {
            var key = req.Key;
            var lastUnderscore = key.LastIndexOf('_');

            if (lastUnderscore <= 0)
            {
                continue;
            }

            var prefix = key[..lastUnderscore];
            var arg = key[(lastUnderscore + 1)..];

            if (!prefixToArgs.TryGetValue(prefix, out var args))
            {
                args = new(StringComparer.Ordinal);
                prefixToArgs[prefix] = args;
            }

            args.Add(arg);
        }

        var sortedPrefixes = prefixToArgs
            .OrderBy(kvp => string.Join(",", kvp.Value), StringComparer.Ordinal)
            .Select(kvp => kvp.Key)
            .ToList();

        var result = new (string original, string canonical)[sortedPrefixes.Count];

        for (var i = 0; i < sortedPrefixes.Count; i++)
        {
            result[i] = ($"{sortedPrefixes[i]}_", $"__fusion_{i}_");
        }

        return result;
    }

    private static string ApplyPrefixReplacements(
        string text,
        ReadOnlySpan<(string original, string canonical)> replacements)
    {
        foreach (var (original, canonical) in replacements)
        {
            text = text.Replace(original, canonical);
        }

        return text;
    }

    /// <summary>
    /// Partitions structurally identical operations into groups that can
    /// each be safely merged. Two operations cannot share a group if one
    /// transitively depends on the other, because merging them would
    /// create a cycle in the dependency graph. The <paramref name="mergePolicy"/>
    /// further restricts which candidates may share a group based on their
    /// dependency depth.
    /// </summary>
    private static List<List<OperationExecutionNode>> PartitionIntoMergeableGroups(
        List<OperationExecutionNode> candidates,
        Dictionary<int, HashSet<int>> dependenciesByStepId,
        OperationMergePolicy mergePolicy)
    {
        // Pre-compute dependency depths when the policy needs them.
        Dictionary<int, int>? depthLookup = null;

        if (mergePolicy is OperationMergePolicy.Conservative
            or OperationMergePolicy.Balanced)
        {
            depthLookup = [];
            var recursionStack = new HashSet<int>();

            foreach (var candidate in candidates)
            {
                GetDependencyDepth(candidate.Id, dependenciesByStepId, depthLookup, recursionStack);
            }
        }

        var groups = new List<List<OperationExecutionNode>>();
        var visited = new HashSet<int>();

        foreach (var candidate in candidates)
        {
            var placed = false;

            foreach (var group in groups)
            {
                var canJoin = true;

                // Policy-specific depth checks (applied before the more
                // expensive transitive-reachability walk).
                if (depthLookup is not null)
                {
                    var candidateDepth = depthLookup[candidate.Id];
                    var referenceDepth = depthLookup[group[0].Id];

                    switch (mergePolicy)
                    {
                        case OperationMergePolicy.Conservative
                            when candidateDepth != referenceDepth:
                            canJoin = false;
                            break;

                        case OperationMergePolicy.Balanced
                            when Math.Abs(candidateDepth - referenceDepth) > 1:
                            canJoin = false;
                            break;
                    }
                }

                if (canJoin)
                {
                    foreach (var existing in group)
                    {
                        visited.Clear();

                        if (IsTransitivelyReachable(candidate.Id, existing.Id, dependenciesByStepId, visited))
                        {
                            canJoin = false;
                            break;
                        }

                        visited.Clear();

                        if (IsTransitivelyReachable(existing.Id, candidate.Id, dependenciesByStepId, visited))
                        {
                            canJoin = false;
                            break;
                        }
                    }
                }

                if (canJoin)
                {
                    group.Add(candidate);
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                groups.Add([candidate]);
            }
        }

        return groups;
    }

    private static bool IsTransitivelyReachable(
        int fromId,
        int targetId,
        Dictionary<int, HashSet<int>> dependenciesByStepId,
        HashSet<int> visited)
    {
        if (!dependenciesByStepId.TryGetValue(fromId, out var dependencies))
        {
            return false;
        }

        foreach (var dependency in dependencies)
        {
            if (dependency == targetId)
            {
                return true;
            }

            if (visited.Add(dependency) && IsTransitivelyReachable(dependency, targetId, dependenciesByStepId, visited))
            {
                return true;
            }
        }

        return false;
    }

    private static SelectionSetNode GetSelectionSetNodeFromPath(
        OperationDefinitionNode operationDefinition,
        SelectionPath path)
    {
        var current = operationDefinition.SelectionSet;

        if (path.IsRoot)
        {
            return current;
        }

        for (var i = 0; i < path.Length; i++)
        {
            var segment = path[i];

            switch (segment.Kind)
            {
                case SelectionPathSegmentKind.InlineFragment:
                {
                    var selection = current.Selections
                        .OfType<InlineFragmentNode>()
                        .FirstOrDefault(s => s.TypeCondition?.Name.Value == segment.Name)
                        ?? throw new InvalidOperationException(
                            $"Inline fragment on type '{segment.Name}' not found at path segment {i}.");

                    current = selection.SelectionSet;
                    break;
                }
                case SelectionPathSegmentKind.Field:
                {
                    var selection = current.Selections
                        .OfType<FieldNode>()
                        .FirstOrDefault(s => s.Alias?.Value == segment.Name || s.Name.Value == segment.Name);

                    if (selection?.SelectionSet is null)
                    {
                        throw new InvalidOperationException(
                            $"Field '{segment.Name}' not found or has no selection set at path segment {i}.");
                    }

                    current = selection.SelectionSet;
                    break;
                }
            }
        }

        return current;
    }

    /// <summary>
    /// Strips child selection sets from fields whose return type is not a
    /// value type. Only value-type subtrees are relevant for the result
    /// selection set; the rest are resolved by separate execution nodes.
    /// </summary>
    private static SelectionSetNode PruneNonValueTypeChildren(
        SelectionSetNode selectionSet,
        ITypeDefinition parentType,
        FusionSchemaDefinition schema,
        string? sourceSchemaName)
    {
        if (parentType is not IComplexTypeDefinition complexType)
        {
            return selectionSet;
        }

        var changed = false;
        var selections = new ISelectionNode[selectionSet.Selections.Count];

        for (var i = 0; i < selectionSet.Selections.Count; i++)
        {
            var selection = selectionSet.Selections[i];

            switch (selection)
            {
                case FieldNode field when field.SelectionSet is not null:
                {
                    if (complexType.Fields.TryGetField(field.Name.Value, out var fieldDef))
                    {
                        var fieldNamedType = fieldDef.Type.NamedType();

                        if (fieldNamedType is FusionComplexTypeDefinition { IsValueType: true } valueType)
                        {
                            var pruned = PruneNonValueTypeChildren(
                                field.SelectionSet, valueType, schema, sourceSchemaName);

                            if (!ReferenceEquals(pruned, field.SelectionSet))
                            {
                                selections[i] = new FieldNode(
                                    field.Name, field.Alias, field.Directives, field.Arguments, pruned);
                                changed = true;
                                continue;
                            }
                        }
                        else if (!IsOpaqueInterfaceObjectStandIn(fieldNamedType, sourceSchemaName))
                        {
                            // A non-value complex field (an entity boundary) is normally stripped
                            // because it is completed by a separate execution node. When its subtree
                            // reaches an @interfaceObject stand-in, however, the path to that field
                            // must be kept so the result selection set can carry the opacity marker;
                            // recurse to preserve it instead of stripping the whole subtree.
                            if (fieldNamedType is FusionComplexTypeDefinition complexFieldType
                                && SubtreeContainsOpaqueStandIn(
                                    field.SelectionSet, complexFieldType, schema, sourceSchemaName))
                            {
                                var pruned = PruneNonValueTypeChildren(
                                    field.SelectionSet, complexFieldType, schema, sourceSchemaName);

                                selections[i] = new FieldNode(
                                    field.Name, field.Alias, field.Directives, field.Arguments, pruned);
                                changed = true;
                                continue;
                            }

                            selections[i] = new FieldNode(
                                field.Name, field.Alias, field.Directives, field.Arguments, null);
                            changed = true;
                            continue;
                        }

                        // An @interfaceObject stand-in field keeps its interface-declared child
                        // selections so the result selection set can carry the opacity marker; the
                        // opaque value completes interface-typed against exactly those fields.
                    }

                    selections[i] = selection;
                    break;
                }

                case InlineFragmentNode inlineFragment:
                {
                    var fragmentType = inlineFragment.TypeCondition is not null
                        && schema.Types.TryGetType(
                            inlineFragment.TypeCondition.Name.Value,
                            allowInaccessibleFields: true,
                            out var resolvedType)
                            ? resolvedType
                            : parentType;

                    var pruned = PruneNonValueTypeChildren(
                        inlineFragment.SelectionSet, fragmentType, schema, sourceSchemaName);

                    if (!ReferenceEquals(pruned, inlineFragment.SelectionSet))
                    {
                        selections[i] = new InlineFragmentNode(
                            inlineFragment.Location,
                            inlineFragment.TypeCondition,
                            inlineFragment.Directives,
                            pruned);
                        changed = true;
                        continue;
                    }

                    selections[i] = selection;
                    break;
                }

                default:
                    selections[i] = selection;
                    break;
            }
        }

        return changed ? new SelectionSetNode(selections) : selectionSet;
    }

    private static bool IsOpaqueInterfaceObjectStandIn(ITypeDefinition namedType, string? sourceSchemaName)
        => sourceSchemaName is not null
            && namedType is FusionInterfaceTypeDefinition interfaceType
            && interfaceType.Sources.TryGetMember(sourceSchemaName, out var source)
            && source.IsInterfaceObject;

    // Reports whether a selection set reaches an @interfaceObject stand-in field anywhere in its
    // subtree. Used to decide whether a non-value entity boundary must keep its child selections so
    // the result selection set can carry the opacity marker for a nested stand-in value.
    private static bool SubtreeContainsOpaqueStandIn(
        SelectionSetNode selectionSet,
        ITypeDefinition parentType,
        FusionSchemaDefinition schema,
        string? sourceSchemaName)
    {
        if (parentType is not IComplexTypeDefinition complexType)
        {
            return false;
        }

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode { SelectionSet: not null } field)
            {
                if (complexType.Fields.TryGetField(field.Name.Value, out var fieldDef))
                {
                    var fieldNamedType = fieldDef.Type.NamedType();

                    if (IsOpaqueInterfaceObjectStandIn(fieldNamedType, sourceSchemaName)
                        || (fieldNamedType is FusionComplexTypeDefinition complexFieldType
                            && SubtreeContainsOpaqueStandIn(
                                field.SelectionSet, complexFieldType, schema, sourceSchemaName)))
                    {
                        return true;
                    }
                }
            }
            else if (selection is InlineFragmentNode inlineFragment)
            {
                var fragmentType = inlineFragment.TypeCondition is not null
                    && schema.Types.TryGetType(
                        inlineFragment.TypeCondition.Name.Value,
                        allowInaccessibleFields: true,
                        out var resolvedType)
                        ? resolvedType
                        : parentType;

                if (SubtreeContainsOpaqueStandIn(
                        inlineFragment.SelectionSet, fragmentType, schema, sourceSchemaName))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool DoVariablesContainUploadScalar(
        IReadOnlyList<VariableDefinitionNode> variables,
        ISchemaDefinition schema)
    {
        var inputObjectTypes = new Queue<IInputObjectTypeDefinition>();
        var visited = new HashSet<IInputObjectTypeDefinition>(ReferenceEqualityComparer.Instance);

        foreach (var variable in variables)
        {
            var variableTypeName = variable.Type.NamedType().Name.Value;
            var variableType = schema.Types[variableTypeName];

            if (variableType is IScalarTypeDefinition { Name: UploadScalarName })
            {
                return true;
            }

            if (variableType is IInputObjectTypeDefinition inputObjectType && visited.Add(inputObjectType))
            {
                inputObjectTypes.Enqueue(inputObjectType);
            }
        }

        while (inputObjectTypes.TryDequeue(out var inputObjectType))
        {
            foreach (var field in inputObjectType.Fields)
            {
                var fieldType = field.Type.NamedType();

                if (fieldType is IScalarTypeDefinition { Name: UploadScalarName })
                {
                    return true;
                }

                if (fieldType is IInputObjectTypeDefinition nestedInputObjectType && visited.Add(nestedInputObjectType))
                {
                    inputObjectTypes.Enqueue(nestedInputObjectType);
                }
            }
        }

        return false;
    }

    private static OperationDefinitionNode RemoveEmptySelections(OperationDefinitionNode operationDefinition)
    {
        return SyntaxRewriter.Create(
                rewrite: node =>
                {
                    if (node is not SelectionSetNode selectionSet)
                    {
                        return node;
                    }

                    List<ISelectionNode>? rewritten = null;
                    var selections = selectionSet.Selections;

                    for (var i = 0; i < selections.Count; i++)
                    {
                        var selection = selections[i];
                        var removeSelection =
                            selection is FieldNode { SelectionSet.Selections.Count: 0 }
                                or InlineFragmentNode { SelectionSet.Selections.Count: 0 };

                        if (!removeSelection)
                        {
                            rewritten?.Add(selection);
                            continue;
                        }

                        if (rewritten is null)
                        {
                            rewritten = new List<ISelectionNode>(selections.Count - 1);
                            for (var j = 0; j < i; j++)
                            {
                                rewritten.Add(selections[j]);
                            }
                        }
                    }

                    return rewritten is null
                        ? node
                        : new SelectionSetNode(rewritten);
                })
            .Rewrite(operationDefinition)!;
    }

    /// <summary>
    /// Removes gateway-internal executable directives from an operation before it is serialized
    /// for a source schema. Source schemas do not understand these directives and reject any
    /// request that carries them. The caller must pass a copy: the original plan step definition
    /// keeps the markers so the result reader can still identify requirement-only selections.
    /// </summary>
    /// <remarks>
    /// Two internal directives can occur on outgoing selections:
    /// <c>fusion__requirement</c> marks requirement-only selections; the directive is stripped while
    /// the field is kept, because the data still has to be fetched. <c>fusion__empty</c> is a
    /// synthesized <c>__typename</c> placeholder for an otherwise-empty selection set; the placeholder
    /// is dropped when the set has real siblings, otherwise the directive is stripped so a plain
    /// <c>__typename</c> keeps the selection set valid.
    /// </remarks>
    private static OperationDefinitionNode RemoveInternalDirectives(OperationDefinitionNode operationDefinition)
    {
        return SyntaxRewriter.Create(
                rewrite: node =>
                {
                    if (node is not SelectionSetNode selectionSet)
                    {
                        return node;
                    }

                    var selections = selectionSet.Selections;
                    List<ISelectionNode>? rewritten = null;

                    for (var i = 0; i < selections.Count; i++)
                    {
                        var selection = selections[i];
                        var replacement = selection;
                        var drop = false;

                        switch (selection)
                        {
                            case FieldNode
                            {
                                Alias: null,
                                Name.Value: IntrospectionFieldNames.TypeName,
                                Directives: [{ Name.Value: "fusion__empty" }]
                            } placeholder:
                                if (selections.Count > 1)
                                {
                                    drop = true;
                                }
                                else
                                {
                                    replacement = placeholder.WithDirectives([]);
                                }

                                break;

                            case FieldNode { Directives.Count: > 0 } field
                                when TryRemoveRequirementDirective(field.Directives, out var fieldDirectives):
                                replacement = field.WithDirectives(fieldDirectives);
                                break;

                            case InlineFragmentNode { Directives.Count: > 0 } fragment
                                when TryRemoveRequirementDirective(fragment.Directives, out var fragmentDirectives):
                                replacement = fragment.WithDirectives(fragmentDirectives);
                                break;
                        }

                        if (rewritten is null)
                        {
                            if (!drop && ReferenceEquals(replacement, selection))
                            {
                                continue;
                            }

                            rewritten = new List<ISelectionNode>(selections.Count);
                            for (var j = 0; j < i; j++)
                            {
                                rewritten.Add(selections[j]);
                            }
                        }

                        if (!drop)
                        {
                            rewritten.Add(replacement);
                        }
                    }

                    return rewritten is null
                        ? node
                        : new SelectionSetNode(rewritten);
                })
            .Rewrite(operationDefinition)!;
    }

    private static bool TryRemoveRequirementDirective(
        IReadOnlyList<DirectiveNode> directives,
        out IReadOnlyList<DirectiveNode> result)
    {
        for (var i = 0; i < directives.Count; i++)
        {
            if (directives[i].Name.Value.Equals("fusion__requirement", StringComparison.Ordinal))
            {
                var remaining = new List<DirectiveNode>(directives.Count - 1);

                for (var j = 0; j < directives.Count; j++)
                {
                    if (!directives[j].Name.Value.Equals("fusion__requirement", StringComparison.Ordinal))
                    {
                        remaining.Add(directives[j]);
                    }
                }

                result = remaining;
                return true;
            }
        }

        result = directives;
        return false;
    }

    /// <summary>
    /// Extracts @skip and @include directives from every selection in the
    /// root selection set (or beneath a lookup field) and promotes them to
    /// node-level conditions on the plan step. This allows the executor to
    /// evaluate the conditions once and skip the entire request if needed.
    /// </summary>
    private static OperationPlanStep ExtractConditionsAndRewriteSelectionSet(OperationPlanStep step)
    {
        var context = new ConditionalSelectionSetRewriterContext();

        OperationDefinitionNode newOperation;

        if (step.Lookup is not null)
        {
            FieldNode? lookupFieldNode = null;

            foreach (var selection in step.Definition.SelectionSet.Selections)
            {
                if (selection is FieldNode fieldNode && fieldNode.Name.Value == step.Lookup.FieldName)
                {
                    lookupFieldNode = fieldNode;
                    break;
                }
            }

            if (lookupFieldNode?.SelectionSet is not { } lookupSelectionSet)
            {
                throw new InvalidOperationException(
                    "Expected to find the lookup field with a selection set in the operation definition");
            }

            var newLookupSelectionSet = RewriteConditionalSelectionSet(lookupSelectionSet, context);
            var newLookupField = lookupFieldNode.WithSelectionSet(newLookupSelectionSet);

            newOperation = step.Definition.WithSelectionSet(
                new SelectionSetNode([newLookupField]));
        }
        else
        {
            var newRootSelectionSet = RewriteConditionalSelectionSet(step.Definition.SelectionSet, context);

            newOperation = step.Definition.WithSelectionSet(newRootSelectionSet);
        }

        var mergedConditions = context.Conditions;

        foreach (var existing in step.Conditions)
        {
            mergedConditions.Add(existing);
        }

        return step with
        {
            Definition = newOperation,
            Conditions = mergedConditions
                .OrderBy(c => c.VariableName, StringComparer.Ordinal)
                .ThenBy(c => c.PassingValue)
                .ToArray(),
        };
    }

    private static SelectionSetNode RewriteConditionalSelectionSet(
        SelectionSetNode selectionSetNode,
        ConditionalSelectionSetRewriterContext context)
    {
        var selections = new List<ISelectionNode>();

        foreach (var selection in selectionSetNode.Selections)
        {
            switch (selection)
            {
                case FieldNode fieldNode:
                {
                    var conditions = ExtractConditions(fieldNode.Directives);

                    if (conditions is not null)
                    {
                        var newDirectives = new List<DirectiveNode>(fieldNode.Directives);

                        foreach (var condition in conditions)
                        {
                            context.Conditions.Add(condition);
                            newDirectives.Remove(condition.Directive!);
                        }

                        fieldNode = fieldNode.WithDirectives(newDirectives);
                    }

                    selections.Add(fieldNode);
                    break;
                }
                case InlineFragmentNode inlineFragmentNode:
                {
                    if (inlineFragmentNode.TypeCondition is null)
                    {
                        var fragmentSelectionSet =
                            RewriteConditionalSelectionSet(inlineFragmentNode.SelectionSet, context);

                        if (fragmentSelectionSet.Selections.Count == 0)
                        {
                            continue;
                        }

                        var conditions = ExtractConditions(inlineFragmentNode.Directives);

                        if (conditions is not null)
                        {
                            var newDirectives = new List<DirectiveNode>(inlineFragmentNode.Directives);

                            foreach (var condition in conditions)
                            {
                                context.Conditions.Add(condition);
                                newDirectives.Remove(condition.Directive!);
                            }

                            if (newDirectives.Count == 0)
                            {
                                selections.AddRange(fragmentSelectionSet.Selections);
                                continue;
                            }

                            inlineFragmentNode = inlineFragmentNode.WithDirectives(newDirectives);
                        }
                    }

                    selections.Add(inlineFragmentNode);
                    break;
                }
            }
        }

        return new SelectionSetNode(selections);
    }

    private sealed class ExecutionPlanBuildContext
    {
        public HashSet<int> ProcessedStepIds { get; } = [];
        public Dictionary<int, ExecutionNode> ExecutionNodes { get; } = [];
        public Dictionary<int, HashSet<int>> DependenciesByStepId { get; } = [];
        public Dictionary<int, Dictionary<string, int>> BranchesByNodeId { get; } = [];
        public Dictionary<int, int> FallbackByNodeId { get; } = [];
        public Dictionary<int, int> RedirectedStepIds { get; } = [];
    }

    private sealed class ConditionalSelectionSetRewriterContext
    {
        public HashSet<ExecutionNodeCondition> Conditions { get; } = [];
    }

    private readonly record struct MergeResult(
        SelectionPath[] Targets,
        OperationSourceText CanonicalOp,
        OperationRequirement[] CanonicalRequirements,
        OperationExecutionNode Primary);
}

file static class Extensions
{
    private static readonly Encoding s_encoding = Encoding.UTF8;

    /// <summary>
    /// Returns <see langword="true"/> when every selection in the relevant
    /// selection set carries a @skip or @include directive, meaning the
    /// entire operation is conditional and can potentially be skipped.
    /// </summary>
    public static bool AreAllProvidedSelectionsConditional(this OperationPlanStep step)
    {
        var selectionSetNode = step.Definition.SelectionSet;

        if (step.Lookup is not null)
        {
            FieldNode? lookupFieldNode = null;

            if (!step.Lookup.Path.IsEmpty)
            {
                foreach (var fieldName in step.Lookup.Path)
                {
                    var fieldNode = selectionSetNode.Selections.FirstOrDefault(selection =>
                        selection is FieldNode fieldNode && fieldNode.Name.Value == fieldName);

                    if (fieldNode is not FieldNode { SelectionSet: { } nextSelectionSetNode })
                    {
                        throw new InvalidOperationException("Unable to resolve the lookup path.");
                    }

                    selectionSetNode = nextSelectionSetNode;
                }
            }

            foreach (var selection in selectionSetNode.Selections)
            {
                if (selection is FieldNode fieldNode && fieldNode.Name.Value == step.Lookup.FieldName)
                {
                    lookupFieldNode = fieldNode;
                    break;
                }
            }

            selectionSetNode = lookupFieldNode?.SelectionSet ??
                throw new InvalidOperationException(
                    "Expected to find the lookup field with a selection set in the operation definition");
        }

        foreach (var selection in selectionSetNode.Selections)
        {
            switch (selection)
            {
                case FieldNode fieldNode
                    when !fieldNode.Directives.Any(d => d.Name.Value is "skip" or "include"):
                    return false;
                case InlineFragmentNode inlineFragmentNode
                    when !inlineFragmentNode.Directives.Any(d => d.Name.Value is "skip" or "include"):
                    return false;
            }
        }

        return true;
    }

    public static bool IsIntrospectionOnly(this Operation operation)
    {
        var hasNonInternalIntrospectionSelection = false;

        foreach (var selection in operation.RootSelectionSet.Selections)
        {
            if (selection.IsInternal)
            {
                continue;
            }

            if (selection.Field.IsIntrospectionField)
            {
                hasNonInternalIntrospectionSelection = true;
                continue;
            }

            return false;
        }

        return hasNonInternalIntrospectionSelection;
    }

    public static bool HasIntrospectionFields(this Operation operation)
    {
        foreach (var selection in operation.RootSelectionSet.Selections)
        {
            if (selection is { IsInternal: false, Field.IsIntrospectionField: true })
            {
                return true;
            }
        }

        return false;
    }

    public static Selection[] GetIntrospectionSelections(this Operation operation)
    {
        var selections = new List<Selection>(operation.RootSelectionSet.Selections.Length);

        foreach (var selection in operation.RootSelectionSet.Selections)
        {
            if (selection is { IsInternal: false, Field.IsIntrospectionField: true })
            {
                selections.Add(selection);
            }
        }

        return selections.ToArray();
    }

    public static OperationSourceText ToSourceText(this OperationDefinitionNode operation)
    {
        var sourceText = operation.ToString(indented: true);
        var sourceTextUtf8 = s_encoding.GetBytes(sourceText);
#if NET9_0_OR_GREATER
        var operationHash = Convert.ToHexStringLower(SHA256.HashData(sourceTextUtf8));
#else
        var operationHash = Convert.ToHexString(SHA256.HashData(sourceTextUtf8)).ToLowerInvariant();
#endif
        return new OperationSourceText(operation.Name!.Value, operation.Operation, sourceText, operationHash);
    }
}
