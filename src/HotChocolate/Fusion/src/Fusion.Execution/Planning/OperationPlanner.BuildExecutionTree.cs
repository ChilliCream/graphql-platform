using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

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
        int searchSpace,
        int expandedNodes)
    {
        if (operation.IsIntrospectionOnly())
        {
            var introspectionNode = new IntrospectionExecutionNode(
                1,
                [.. operation.RootSelectionSet.Selections],
                []);
            introspectionNode.Seal();

            var nodes = ImmutableArray.Create<ExecutionNode>(introspectionNode);

            return OperationPlan.Create(operation, nodes, nodes, searchSpace, expandedNodes);
        }

        var ctx = new ExecutionPlanBuildContext();
        var hasVariables = operationDefinition.VariableDefinitions.Count > 0;

        planSteps = TransformPlanSteps(planSteps, operationDefinition);
        IndexDependencies(planSteps, ctx);
        BuildExecutionNodes(planSteps, ctx, _schema, hasVariables);
        MergeAndBatchOperations(ctx, _options.EnableRequestGrouping, _options.MergePolicy);
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

        return OperationPlan.Create(operation, rootNodes, allNodes, searchSpace, expandedNodes);
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
        ISchemaDefinition schema,
        bool hasVariables)
    {
        var requiresUpload = schema.Types.TryGetType(UploadScalarName, out var uploadType) && uploadType.IsScalarType();
        var readySteps = planSteps.Where(t => !ctx.DependenciesByStepId.ContainsKey(t.Id)).ToList();
        var variableBuffer = hasVariables ? new List<string>() : null;

        while (ctx.ProcessedStepIds.Count < planSteps.Count)
        {
            foreach (var step in readySteps)
            {
                if (!ctx.ProcessedStepIds.Add(step.Id))
                {
                    continue;
                }

                if (step is OperationPlanStep operationStep)
                {
                    ctx.ExecutionNodes.Add(step.Id,
                        CreateOperationExecutionNode(operationStep, schema, requiresUpload, variableBuffer));
                }
                else if (step is NodeFieldPlanStep nodeStep)
                {
                    ctx.ExecutionNodes.Add(step.Id,
                        new NodeFieldExecutionNode(nodeStep.Id, nodeStep.ResponseName, nodeStep.IdValue, nodeStep.Conditions));
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
    }

    private static OperationExecutionNode CreateOperationExecutionNode(
        OperationPlanStep operationStep,
        ISchemaDefinition schema,
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

        var operation = RemoveEmptyTypeNames(operationStep.Definition);
        var operationSource = operation.ToSourceText();

        var selectionSetNode = GetSelectionSetNodeFromPath(operationStep.Definition, operationStep.Source);
        selectionSetNode = PruneNonValueTypeChildren(selectionSetNode, operationStep.Type, schema);
        var resultSelectionSet = ResultSelectionSet.Create(selectionSetNode, schema);

        return new OperationExecutionNode(
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
    }

    private static void MergeAndBatchOperations(
        ExecutionPlanBuildContext ctx,
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
    private static Dictionary<OperationBatchExecutionNode, Dictionary<int, int[]>>
        GroupBySchemaAndDepthIntoBatches(
            ExecutionPlanBuildContext ctx,
            Dictionary<int, bool> nodeFieldBoundCache,
            Dictionary<int, MergeResult> mergeResults,
            Dictionary<int, int[]> originalDependencies,
            bool enableRequestGrouping)
    {
        var consumedMergeIds = new HashSet<int>();
        var perOperationDependencies = new Dictionary<OperationBatchExecutionNode, Dictionary<int, int[]>>();

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
    /// Wraps merged operations that were not included in any multi-member
    /// batch group into standalone batch execution nodes.
    /// </summary>
    private static void WrapRemainingMergedOperations(
        ExecutionPlanBuildContext ctx,
        Dictionary<int, MergeResult> remainingMerges,
        Dictionary<OperationBatchExecutionNode, Dictionary<int, int[]>> perOperationDependencies,
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
        Dictionary<OperationBatchExecutionNode, Dictionary<int, int[]>> perOperationDependencies)
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
        List<OperationExecutionNode> members,
        OperationBatchExecutionNode batchNode,
        int batchNodeId)
    {
        var batchDependencies = new HashSet<int>();
        var memberIds = new HashSet<int>(members.Count);

        foreach (var member in members)
        {
            memberIds.Add(member.Id);
            ctx.ExecutionNodes.Remove(member.Id);

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

    private static BatchOperationDefinition CreateBatchOperationDefinition(MergeResult merge)
    {
        var primary = merge.Primary;
        return new BatchOperationDefinition(
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
    }

    private static SingleOperationDefinition CreateSingleOperationDefinition(OperationExecutionNode member)
    {
        return new SingleOperationDefinition(
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
        }

        foreach (var (nodeId, stepDependencies) in ctx.DependenciesByStepId)
        {
            if (!ctx.ExecutionNodes.TryGetValue(nodeId, out var entry)
                || entry is not (OperationExecutionNode or OperationBatchExecutionNode))
            {
                continue;
            }

            if (entry is OperationBatchExecutionNode batchEntry)
            {
                WireBatchNodeDependencies(batchEntry, stepDependencies, executionNodeById);
                continue;
            }

            // For a standalone operation node, wire dependencies directly.
            foreach (var dependencyId in stepDependencies)
            {
                if (!ctx.ExecutionNodes.TryGetValue(dependencyId, out var childEntry)
                    || childEntry is not (OperationExecutionNode or OperationBatchExecutionNode or NodeFieldExecutionNode))
                {
                    continue;
                }

                childEntry.AddDependent(entry);
                entry.AddDependency(childEntry);
            }
        }
    }

    private static void WireBatchNodeDependencies(
        OperationBatchExecutionNode batchEntry,
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
            if (batchEntry.Operations.Length > 1)
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
        ISchemaDefinition schema)
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
                    var responseName = field.Alias?.Value ?? field.Name.Value;

                    if (complexType.Fields.TryGetField(responseName, out var fieldDef))
                    {
                        var fieldNamedType = fieldDef.Type.NamedType();

                        if (fieldNamedType is FusionComplexTypeDefinition { IsValueType: true } valueType)
                        {
                            var pruned = PruneNonValueTypeChildren(field.SelectionSet, valueType, schema);

                            if (!ReferenceEquals(pruned, field.SelectionSet))
                            {
                                selections[i] = new FieldNode(
                                    field.Name, field.Alias, field.Directives, field.Arguments, pruned);
                                changed = true;
                                continue;
                            }
                        }
                        else
                        {
                            selections[i] = new FieldNode(
                                field.Name, field.Alias, field.Directives, field.Arguments, null);
                            changed = true;
                            continue;
                        }
                    }

                    selections[i] = selection;
                    break;
                }

                case InlineFragmentNode inlineFragment:
                {
                    var fragmentType = inlineFragment.TypeCondition is not null
                        && schema.Types.TryGetType(inlineFragment.TypeCondition.Name.Value, out var resolvedType)
                            ? resolvedType
                            : parentType;

                    var pruned = PruneNonValueTypeChildren(inlineFragment.SelectionSet, fragmentType, schema);

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

    private static OperationDefinitionNode RemoveEmptyTypeNames(OperationDefinitionNode operationDefinition)
    {
        return (OperationDefinitionNode)SyntaxRewriter.Create<List<bool>>(
                rewrite: (node, context) =>
                {
                    if (node is SelectionSetNode selectionSet && context.Peek())
                    {
                        var items = selectionSet.Selections.ToList();
                        for (var i = items.Count - 1; i >= 0; i--)
                        {
                            if (items[i] is FieldNode
                                {
                                    Alias: null,
                                    Name.Value: IntrospectionFieldNames.TypeName,
                                    Directives: [{ Name.Value: "fusion__empty" }]
                                } field)
                            {
                                if (items.Count > 1)
                                {
                                    items.RemoveAt(i);
                                }
                                else
                                {
                                    items[i] = field.WithDirectives([]);
                                }
                            }
                        }

                        return new SelectionSetNode(items);
                    }

                    return node;
                },
                enter: (node, context) =>
                {
                    switch (node)
                    {
                        case SelectionSetNode:
                            context.Push(false);
                            break;

                        case FieldNode
                        {
                            Alias: null,
                            Name.Value: IntrospectionFieldNames.TypeName,
                            Directives: [{ Name.Value: "fusion__empty" }]
                        }:
                            context[^1] = true;
                            break;
                    }

                    return context;
                },
                leave: (node, context) =>
                {
                    if (node is SelectionSetNode)
                    {
                        context.Pop();
                    }
                })
            .Rewrite(operationDefinition, [])!;
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
