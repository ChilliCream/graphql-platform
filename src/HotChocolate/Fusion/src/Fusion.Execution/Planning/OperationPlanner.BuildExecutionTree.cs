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

        var completedSteps = new HashSet<int>();
        var completedNodes = new Dictionary<int, ExecutionNode>();
        var dependencyLookup = new Dictionary<int, HashSet<int>>();
        var branchesLookup = new Dictionary<int, Dictionary<string, int>>();
        var fallbackLookup = new Dictionary<int, int>();
        var hasVariables = operationDefinition.VariableDefinitions.Count > 0;

        planSteps = PrepareSteps(planSteps, operationDefinition, dependencyLookup, branchesLookup, fallbackLookup);
        BuildExecutionNodes(
            planSteps,
            completedSteps,
            completedNodes,
            dependencyLookup,
            _schema,
            hasVariables);
        MergeIntoNewBatchNodes(completedNodes, dependencyLookup, _options.EnableRequestGrouping);
        BuildDependencyStructure(completedNodes, dependencyLookup, branchesLookup, fallbackLookup);

        var rootNodes = planSteps
            .Where(t => !dependencyLookup.ContainsKey(t.Id) && completedNodes.ContainsKey(t.Id))
            .Select(t => completedNodes[t.Id])
            .ToImmutableArray();

        var allNodes = completedNodes
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

    private static ImmutableList<PlanStep> PrepareSteps(
        ImmutableList<PlanStep> planSteps,
        OperationDefinitionNode originalOperation,
        Dictionary<int, HashSet<int>> dependencyLookup,
        Dictionary<int, Dictionary<string, int>> branchesLookup,
        Dictionary<int, int> fallbackLookup)
    {
        var updatedPlanSteps = planSteps;
        var forwardVariableContext = new ForwardVariableRewriter.Context();

        foreach (var variableDef in originalOperation.VariableDefinitions)
        {
            forwardVariableContext.Variables[variableDef.Variable.Name.Value] = variableDef;
        }

        foreach (var step in planSteps)
        {
            if (step is OperationPlanStep operationPlanStep)
            {
                // The planning phase can leave behind empty child selection sets (literal `{}`)
                // after rewriting requirement fields. We clean those up first. Only after
                // that cleanup do we check whether the entire root selection set is empty,
                // because removing an empty child may still leave other valid selections.
                operationPlanStep = RemoveEmptySelectionSets(operationPlanStep);

                if (!ReferenceEquals(step, operationPlanStep))
                {
                    updatedPlanSteps = updatedPlanSteps.Replace(step, operationPlanStep);
                }

                // During planning we keep partially built operation steps alive so that
                // requirement fields can be inlined into them later. If those requirements
                // never materialize, the step ends up with no useful selections. We remove
                // it here because an empty step would produce a broken execution node.
                if (IsEmptyOperation(operationPlanStep))
                {
                    updatedPlanSteps = updatedPlanSteps.Remove(operationPlanStep);
                    continue;
                }

                // When every root-level selection (or every selection beneath a lookup
                // field) carries a @skip or @include directive, we extract those directives
                // and promote them to conditions on the execution node itself. This lets
                // the executor skip the entire network call when the condition evaluates
                // to false, rather than sending a request that returns nothing.
                if (operationPlanStep.AreAllProvidedSelectionsConditional())
                {
                    var updatedOperationPlanStep = ExtractConditionsAndRewriteSelectionSet(operationPlanStep);

                    updatedPlanSteps = updatedPlanSteps.Replace(operationPlanStep, updatedOperationPlanStep);

                    operationPlanStep = updatedOperationPlanStep;
                }

                // At this point the operation definition on the plan step does not yet
                // declare its variable definitions. We walk the definition to discover
                // which variables and requirements it actually references, then attach
                // matching variable definitions so the operation is syntactically complete.
                updatedPlanSteps = updatedPlanSteps.Replace(
                    operationPlanStep,
                    AddVariableDefinitions(operationPlanStep));

                // Each plan step knows which other steps depend on its results
                // (for example, steps that need lookup data or field requirements
                // from this step). Here we invert that relationship into a reverse
                // lookup: for every step, we record which steps it depends on.
                // This reverse view makes the execution-order algorithm simpler
                // because we can quickly check whether all of a step's
                // dependencies have been completed.
                foreach (var dependent in operationPlanStep.Dependents)
                {
                    if (!dependencyLookup.TryGetValue(dependent, out var dependencies))
                    {
                        dependencies = [];
                        dependencyLookup[dependent] = dependencies;
                    }

                    dependencies.Add(step.Id);
                }
            }
            else if (step is NodeFieldPlanStep nodePlanStep)
            {
                foreach (var (_, dependent) in nodePlanStep.Branches)
                {
                    if (!dependencyLookup.TryGetValue(dependent.Id, out var dependencies))
                    {
                        dependencies = [];
                        dependencyLookup[dependent.Id] = dependencies;
                    }

                    dependencies.Add(nodePlanStep.Id);
                }

                if (!dependencyLookup.TryGetValue(nodePlanStep.FallbackQuery.Id, out var fallbackDependencies))
                {
                    fallbackDependencies = [];
                    dependencyLookup[nodePlanStep.FallbackQuery.Id] = fallbackDependencies;
                }

                fallbackDependencies.Add(nodePlanStep.Id);

                branchesLookup.Add(nodePlanStep.Id, nodePlanStep.Branches.ToDictionary(x => x.Key, x => x.Value.Id));
                fallbackLookup.Add(nodePlanStep.Id, nodePlanStep.FallbackQuery.Id);
            }
        }

        return updatedPlanSteps;

        bool IsEmptyOperation(OperationPlanStep step)
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

        OperationPlanStep RemoveEmptySelectionSets(OperationPlanStep step)
        {
            var updatedDefinition = RemoveEmptySelections(step.Definition);
            return ReferenceEquals(updatedDefinition, step.Definition)
                ? step
                : step with { Definition = updatedDefinition };
        }

        OperationPlanStep AddVariableDefinitions(OperationPlanStep step)
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

    private static void BuildExecutionNodes(
        ImmutableList<PlanStep> planSteps,
        HashSet<int> completedSteps,
        Dictionary<int, ExecutionNode> completedNodes,
        Dictionary<int, HashSet<int>> dependencyLookup,
        ISchemaDefinition schema,
        bool hasVariables)
    {
        var requiresUpload = schema.Types.TryGetType(UploadScalarName, out var uploadType) && uploadType.IsScalarType();
        var readySteps = planSteps.Where(t => !dependencyLookup.ContainsKey(t.Id)).ToList();
        List<string>? variables = null;

        while (completedSteps.Count < planSteps.Count)
        {
            foreach (var step in readySteps)
            {
                if (!completedSteps.Add(step.Id))
                {
                    continue;
                }

                if (step is OperationPlanStep operationStep)
                {
                    var requirements = Array.Empty<OperationRequirement>();

                    if (!operationStep.Requirements.IsEmpty)
                    {
                        var temp = new List<OperationRequirement>();

                        foreach (var (_, requirement) in operationStep.Requirements.OrderBy(t => t.Key))
                        {
                            temp.Add(requirement);
                        }

                        requirements = temp.ToArray();
                    }

                    variables?.Clear();

                    if (hasVariables && operationStep.Definition.VariableDefinitions.Count > 0)
                    {
                        variables ??= [];

                        foreach (var variableDef in operationStep.Definition.VariableDefinitions)
                        {
                            if (requirements.Any(r => r.Key == variableDef.Variable.Name.Value))
                            {
                                continue;
                            }

                            variables.Add(variableDef.Variable.Name.Value);
                        }
                    }

                    var requiresFileUpload = requiresUpload
                        && DoVariablesContainUploadScalar(operationStep.Definition.VariableDefinitions, schema);

                    var operation = RemoveEmptyTypeNames(operationStep.Definition);
                    var operationSource = operation.ToSourceText();

                    var selectionSetNode = GetSelectionSetNodeFromPath(operationStep.Definition, operationStep.Source);
                    selectionSetNode = PruneNonValueTypeChildren(selectionSetNode, operationStep.Type, schema);
                    var resultSelectionSet = ResultSelectionSet.Create(selectionSetNode, schema);

                    var node = new OperationExecutionNode(
                        operationStep.Id,
                        operationSource,
                        operationStep.SchemaName,
                        operationStep.Target,
                        operationStep.Source,
                        requirements,
                        variables?.Count > 0 ? variables.ToArray() : [],
                        resultSelectionSet,
                        operationStep.Conditions,
                        requiresFileUpload);

                    completedNodes.Add(step.Id, node);
                }
                else if (step is NodeFieldPlanStep nodeStep)
                {
                    var node = new NodeFieldExecutionNode(
                        nodeStep.Id,
                        nodeStep.ResponseName,
                        nodeStep.IdValue,
                        nodeStep.Conditions);

                    completedNodes.Add(step.Id, node);
                }
            }

            readySteps.Clear();

            foreach (var step in planSteps)
            {
                if (dependencyLookup.TryGetValue(step.Id, out var stepDependencies)
                    && completedSteps.IsSupersetOf(stepDependencies))
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
        Dictionary<int, HashSet<int>> dependencyLookup,
        Dictionary<int, int> dependencyDepthLookup,
        HashSet<int> recursionStack)
    {
        if (dependencyDepthLookup.TryGetValue(stepId, out var depth))
        {
            return depth;
        }

        if (!dependencyLookup.TryGetValue(stepId, out var directDependencies) || directDependencies.Count == 0)
        {
            dependencyDepthLookup[stepId] = 0;
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
                dependencyLookup,
                dependencyDepthLookup,
                recursionStack);
            maxDepth = Math.Max(maxDepth, dependencyDepth + 1);
        }

        recursionStack.Remove(stepId);
        dependencyDepthLookup[stepId] = maxDepth;
        return maxDepth;
    }

    private static void BuildDependencyStructure(
        Dictionary<int, ExecutionNode> completedNodes,
        Dictionary<int, HashSet<int>> dependencyLookup,
        Dictionary<int, Dictionary<string, int>> branchesLookup,
        Dictionary<int, int> fallbackLookup)
    {
        // Build a lookup that maps every operation plan node identifier to the
        // execution node that contains it. A batch execution node wraps several
        // individual operations, so each of those inner operation identifiers
        // also maps back to the parent batch node. We need this mapping to
        // translate the per-operation dependency graph into execution-level
        // dependencies.
        var executionNodeById = new Dictionary<int, ExecutionNode>();

        foreach (var node in completedNodes.Values)
        {
            executionNodeById[node.Id] = node;

            if (node is OperationBatchExecutionNode batch)
            {
                foreach (var op in batch.Operations)
                {
                    executionNodeById[op.Id] = batch;
                }
            }
        }

        foreach (var (nodeId, stepDependencies) in dependencyLookup)
        {
            if (!completedNodes.TryGetValue(nodeId, out var entry)
                || entry is not (OperationExecutionNode or OperationBatchExecutionNode))
            {
                continue;
            }

            // A batch node bundles multiple operations into one network call.
            // The dependency lookup already holds the union of every inner
            // operation's dependencies. We translate each dependency identifier
            // to the execution node that owns it, skip duplicates, and ignore
            // self-references (which arise when merged operations land in the
            // same batch).
            if (entry is OperationBatchExecutionNode batchEntry)
            {
                var seenExecutionDeps = new HashSet<int>();

                foreach (var dependencyId in stepDependencies)
                {
                    if (dependencyId == batchEntry.Id)
                    {
                        continue;
                    }

                    if (!executionNodeById.TryGetValue(dependencyId, out var depExecNode)
                        || depExecNode.Id == batchEntry.Id)
                    {
                        continue;
                    }

                    // Multiple operation identifiers can map to the same
                    // execution node when several operations were grouped into
                    // one batch. We only wire each execution dependency once.
                    if (!seenExecutionDeps.Add(depExecNode.Id))
                    {
                        continue;
                    }

                    depExecNode.AddDependent(batchEntry);

                    // When a batch holds more than one operation, each
                    // operation may have a different dependency set. A
                    // dependency that only matters to some operations must
                    // not block the whole batch, so we mark it optional.
                    // The executor checks each operation individually and
                    // skips only those whose dependencies failed.
                    //
                    // The same reasoning applies to a single
                    // BatchOperationDefinition with multiple targets created
                    // by a cross-dependency merge. Type dispatch may skip
                    // some targets while others succeed, so dependencies
                    // must stay optional there too.
                    //
                    // Only a true single-operation, single-target batch is
                    // unambiguous: every dependency is required.
                    if (batchEntry.Operations.Length > 1
                        || batchEntry.Operations[0] is BatchOperationDefinition)
                    {
                        batchEntry.AddOptionalDependency(depExecNode);
                    }
                    else
                    {
                        batchEntry.AddDependency(depExecNode);
                    }
                }

                continue;
            }

            // For a regular (non-batch) operation node, the dependency lookup
            // already contains the correct set of dependencies. Wire them up.
            foreach (var dependencyId in stepDependencies)
            {
                if (!completedNodes.TryGetValue(dependencyId, out var childEntry)
                    || childEntry is not (OperationExecutionNode or OperationBatchExecutionNode or NodeFieldExecutionNode))
                {
                    continue;
                }

                childEntry.AddDependent(entry);
                entry.AddDependency(childEntry);
            }
        }

        foreach (var (nodeId, branches) in branchesLookup)
        {
            if (!completedNodes.TryGetValue(nodeId, out var entry) || entry is not NodeFieldExecutionNode node)
            {
                continue;
            }

            foreach (var (typeName, branchNodeId) in branches)
            {
                if (!completedNodes.TryGetValue(branchNodeId, out var branchNode))
                {
                    continue;
                }

                node.AddBranch(typeName, branchNode);
            }
        }

        foreach (var (nodeId, fallbackNodeId) in fallbackLookup)
        {
            if (!completedNodes.TryGetValue(nodeId, out var entry) || entry is not NodeFieldExecutionNode node)
            {
                continue;
            }

            if (!completedNodes.TryGetValue(fallbackNodeId, out var fallbackNode))
            {
                continue;
            }

            node.AddFallbackQuery(fallbackNode);
        }
    }

    private static void MergeIntoNewBatchNodes(
        Dictionary<int, ExecutionNode> completedNodes,
        Dictionary<int, HashSet<int>> dependencyLookup,
        bool enableRequestGrouping)
    {
        // Pass 1 -- Merge structurally identical operations.
        // We compute a canonical signature for each query operation (covering
        // schema name, source path, and query body). Operations that share
        // the same signature are merged into one BatchOperationDefinition so
        // the executor sends a single request instead of many. Dependency
        // sets may differ between merged operations; that is fine as long as
        // merging does not create a cycle in the dependency graph.
        var candidates = new Dictionary<string, List<OperationExecutionNode>>(StringComparer.Ordinal);
        var nodeFieldBoundCache = new Dictionary<int, bool>();

        foreach (var node in completedNodes.Values.OfType<OperationExecutionNode>())
        {
            // Only query operations can be merged or batched. Mutations must
            // execute in order, so they are never candidates for batching.
            if (node.Operation.Type != OperationType.Query)
            {
                continue;
            }

            // Operations that sit below a node-field dispatch must keep their
            // original identifiers. The branch and fallback wiring phase
            // references those identifiers directly, so replacing them with
            // a merged identifier would silently drop executable branches.
            if (IsBoundToNodeField(node.Id))
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

        bool IsBoundToNodeField(int nodeId)
        {
            if (nodeFieldBoundCache.TryGetValue(nodeId, out var cached))
            {
                return cached;
            }

            // The dependency graph is a DAG (enforced by GetDependencyDepth),
            // so we don't need a recursion guard here; caching is sufficient.
            if (!dependencyLookup.TryGetValue(nodeId, out var dependencies) || dependencies.Count == 0)
            {
                nodeFieldBoundCache[nodeId] = false;
                return false;
            }

            foreach (var dependencyId in dependencies)
            {
                if (completedNodes.TryGetValue(dependencyId, out var depNode)
                    && depNode is NodeFieldExecutionNode)
                {
                    nodeFieldBoundCache[nodeId] = true;
                    return true;
                }

                if (IsBoundToNodeField(dependencyId))
                {
                    nodeFieldBoundCache[nodeId] = true;
                    return true;
                }
            }

            nodeFieldBoundCache[nodeId] = false;
            return false;
        }

        // Keep track of which operations were merged in Pass 1 so that Pass 2
        // can reuse the merged definitions when building batch nodes.
        var mergeInfo = new Dictionary<int, MergeResult>();

        foreach (var (_, equivalentNodes) in candidates)
        {
            if (equivalentNodes.Count <= 1)
            {
                continue;
            }

            // Before merging, verify that no candidate transitively depends
            // on another. Merging such a pair would create a cycle in the
            // dependency graph (the merged batch node would depend on itself).
            // When conflicts exist, partition into independent subsets.
            foreach (var group in PartitionIntoMergeableGroups(equivalentNodes, dependencyLookup))
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

                mergeInfo[primary.Id] = new MergeResult(
                    targets,
                    canonicalOp,
                    canonicalRequirements,
                    primary);

                // Remove absorbed nodes and merge their dependency sets
                // into the primary so the dependency graph stays consistent.
                var absorbedIds = new HashSet<int>(group.Count - 1);

                if (!dependencyLookup.TryGetValue(primary.Id, out var primaryDeps))
                {
                    primaryDeps = [];
                }

                for (var i = 1; i < group.Count; i++)
                {
                    var otherId = group[i].Id;
                    absorbedIds.Add(otherId);
                    completedNodes.Remove(otherId);

                    if (dependencyLookup.TryGetValue(otherId, out var otherDeps))
                    {
                        foreach (var dep in otherDeps)
                        {
                            primaryDeps.Add(dep);
                        }

                        dependencyLookup.Remove(otherId);
                    }
                }

                if (primaryDeps.Count > 0)
                {
                    dependencyLookup[primary.Id] = primaryDeps;
                }
                else
                {
                    dependencyLookup.Remove(primary.Id);
                }

                // Redirect remaining references to absorbed IDs
                // so they point to the primary instead.
                foreach (var depSet in dependencyLookup.Values)
                {
                    var hadAbsorbed = false;

                    foreach (var absorbedId in absorbedIds)
                    {
                        if (depSet.Remove(absorbedId))
                        {
                            hadAbsorbed = true;
                        }
                    }

                    if (hadAbsorbed)
                    {
                        depSet.Add(primary.Id);
                    }
                }
            }
        }
        // Take a snapshot of each node's dependency identifiers before Pass 2
        // rewrites the lookup. Pass 2 redirects member identifiers to batch-node
        // identifiers, but the inner operation definitions must retain their
        // original per-operation dependencies to preserve the plan structure.
        var originalDependenciesByNodeId = new Dictionary<int, int[]>(dependencyLookup.Count);

        foreach (var (nodeId, deps) in dependencyLookup)
        {
            originalDependenciesByNodeId[nodeId] = deps.ToArray();
        }

        // Pass 2 -- Group by schema and depth.
        // Query nodes at the same dependency depth targeting the same source
        // schema are independent of each other, so they can ride in a single
        // batched network request.
        var consumedMergeIds = new HashSet<int>();
        var allPerOpDeps = new Dictionary<OperationBatchExecutionNode, Dictionary<int, int[]>>();

        if (enableRequestGrouping)
        {
            var queryNodes = completedNodes.Values
                .OfType<OperationExecutionNode>()
                .Where(n => n.Operation.Type == OperationType.Query)
                .Where(n => !IsBoundToNodeField(n.Id))
                .ToList();

            var dependencyDepthLookup = new Dictionary<int, int>();
            var recursionStack = new HashSet<int>();

            foreach (var node in queryNodes)
            {
                GetDependencyDepth(node.Id, dependencyLookup, dependencyDepthLookup, recursionStack);
            }

            // Group query nodes by the combination of source schema and dependency
            // depth. Nodes sharing the same key are independent of each other and
            // can safely execute together in a single batch request.
            var batchGroups = new Dictionary<(string schema, int depth), List<OperationExecutionNode>>();

            foreach (var node in queryNodes)
            {
                var schemaKey = node.SchemaName ?? DynamicSchemaNameMarker;
                var depth = dependencyDepthLookup.TryGetValue(node.Id, out var d) ? d : 0;
                var key = (schemaKey, depth);

                if (!batchGroups.TryGetValue(key, out var group))
                {
                    group = [];
                    batchGroups[key] = group;
                }

                group.Add(node);
            }

            // Process groups from the shallowest depth to the deepest. When we
            // replace individual nodes with a batch node we redirect dependency
            // references, so processing shallow groups first ensures that deeper
            // groups see already-redirected identifiers.
            foreach (var (_, groupMembers) in batchGroups.OrderBy(t => t.Key.depth))
            {
                if (groupMembers.Count <= 1)
                {
                    // A group with only one member cannot be batched with other
                    // nodes here. If it was merged in Pass 1, it still needs a
                    // batch node wrapper, which is created in the loop below.
                    continue;
                }

                groupMembers.Sort((a, b) => a.Id.CompareTo(b.Id));

                var operations = new List<OperationDefinition>();

                foreach (var member in groupMembers)
                {
                    if (mergeInfo.TryGetValue(member.Id, out var merge))
                    {
                        consumedMergeIds.Add(member.Id);
                        operations.Add(new BatchOperationDefinition(
                            merge.Primary.Id,
                            merge.CanonicalOp,
                            merge.Primary.SchemaName,
                            merge.Targets,
                            merge.Primary.Source,
                            merge.CanonicalRequirements,
                            merge.Primary.ForwardedVariables.ToArray(),
                            merge.Primary.ResultSelectionSet,
                            merge.Primary.Conditions.ToArray(),

                            merge.Primary.RequiresFileUpload));
                    }
                    else
                    {
                        operations.Add(new SingleOperationDefinition(
                            member.Id,
                            member.Operation,
                            member.SchemaName,
                            member.Target,
                            member.Source,
                            member.Requirements.ToArray(),
                            member.ForwardedVariables.ToArray(),
                            member.ResultSelectionSet,
                            member.Conditions.ToArray(),

                            member.RequiresFileUpload));
                    }
                }

                var lowestId = groupMembers[0].Id;
                var batchNode = new OperationBatchExecutionNode(lowestId, operations.ToArray());

                // Capture each member's individual dependency identifiers before we
                // replace the member nodes with the combined batch node. We need
                // these later to wire per-operation dependency links.
                var perOpDeps = new Dictionary<int, int[]>();

                foreach (var member in groupMembers)
                {
                    if (originalDependenciesByNodeId.TryGetValue(member.Id, out var memberDeps))
                    {
                        perOpDeps[member.Id] = memberDeps;
                    }
                }

                // Remove every individual member node from the completed set and
                // replace them with a single batch node. Merge all their
                // dependencies so the batch node knows what it must wait for.
                var batchDeps = new HashSet<int>();

                foreach (var member in groupMembers)
                {
                    completedNodes.Remove(member.Id);

                    if (dependencyLookup.TryGetValue(member.Id, out var memberDeps))
                    {
                        foreach (var dep in memberDeps)
                        {
                            batchDeps.Add(dep);
                        }

                        dependencyLookup.Remove(member.Id);
                    }
                }

                completedNodes[lowestId] = batchNode;

                if (batchDeps.Count > 0)
                {
                    dependencyLookup[lowestId] = batchDeps;
                }

                // Other nodes in the dependency graph may still reference the old
                // member identifiers. Redirect those references to the batch
                // node's identifier so the graph stays consistent.
                var memberIds = new HashSet<int>(groupMembers.Select(m => m.Id));

                foreach (var depSet in dependencyLookup.Values)
                {
                    var hadMember = false;

                    foreach (var memberId in memberIds)
                    {
                        if (depSet.Remove(memberId))
                        {
                            hadMember = true;
                        }
                    }

                    if (hadMember)
                    {
                        depSet.Add(lowestId);
                    }
                }

                // We defer per-operation dependency wiring until every batch node
                // has been created. Otherwise, the lookup would be incomplete and
                // some dependencies would fail to resolve.
                allPerOpDeps[batchNode] = perOpDeps;
            }
        }

        // Some operations were merged in Pass 1 but not consumed by any
        // multi-member batch group in Pass 2 (for example, when their depth
        // group had only one entry). These still need a batch execution node
        // wrapper so the executor can handle them uniformly.
        foreach (var (primaryId, merge) in mergeInfo)
        {
            if (consumedMergeIds.Contains(primaryId))
            {
                continue;
            }

            var primary = merge.Primary;
            var opDef = new BatchOperationDefinition(
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

            var standaloneBatchNode = new OperationBatchExecutionNode(primaryId, [opDef]);
            completedNodes[primaryId] = standaloneBatchNode;

            // Wire the full set of dependencies (union of all merged
            // operations), not just the intersection. The per-operation
            // dependencies must reflect every upstream operation that
            // provides data for any of the merged targets.
            allPerOpDeps[standaloneBatchNode] =
                new Dictionary<int, int[]>
                {
                    [opDef.Id] = originalDependenciesByNodeId.TryGetValue(primaryId, out var primaryDeps)
                        ? primaryDeps
                        : []
                };
        }

        // All batch nodes are now in place. Build a lookup from plan-node
        // identifier to plan node, then wire each inner operation's
        // dependencies to the original operation definitions rather than the
        // redirected batch-node identifiers. This gives the executor
        // fine-grained visibility into exactly which upstream operation each
        // inner operation depends on.
        if (allPerOpDeps.Count > 0)
        {
            var planNodeById = new Dictionary<int, IOperationPlanNode>();

            foreach (var node in completedNodes.Values)
            {
                planNodeById[node.Id] = node;

                if (node is OperationBatchExecutionNode batch)
                {
                    foreach (var op in batch.Operations)
                    {
                        planNodeById[op.Id] = op;
                    }
                }
            }

            foreach (var (_, perOpDeps) in allPerOpDeps)
            {
                foreach (var (opId, depIds) in perOpDeps)
                {
                    if (planNodeById.TryGetValue(opId, out var opNode) && opNode is OperationDefinition opDef)
                    {
                        foreach (var depId in depIds)
                        {
                            if (planNodeById.TryGetValue(depId, out var depNode))
                            {
                                opDef.AddDependency(depNode);
                            }
                        }
                    }
                }
            }
        }
    }

    private static string ComputeCanonicalSignature(OperationExecutionNode node)
    {
        var replacements = BuildPrefixReplacements(node.Requirements);
        var normalizedText = ApplyPrefixReplacements(node.Operation.SourceText, replacements);

        // The first line contains the operation name, which embeds a step
        // identifier that differs between otherwise identical operations.
        // We skip it so the signature reflects structure only.
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
        // Return the primary node's operation text and requirements unchanged.
        // The primary always has the lowest identifier, so its __fusion_{N}_
        // variable-name prefixes already use the lowest numbers that the planner
        // assigned. We keep these original numbers in the merged operation to
        // preserve globally unique naming. The canonical signature method
        // normalizes prefixes only for the purpose of comparing structure; the
        // actual merged operation must not be normalized.
        return (node.Operation, node.Requirements.ToArray());
    }

    /// <summary>
    /// Builds a list of (original, canonical) string replacement pairs for normalizing
    /// <c>__fusion_{N}_</c> variable-name prefixes.
    /// </summary>
    /// <remarks>
    /// The planner assigns a unique numeric prefix to each set of requirement variables,
    /// but two structurally identical operations may receive different numbers depending
    /// on processing order. To compare them reliably, we sort prefixes deterministically
    /// by the alphabetically joined set of their argument names and then map each
    /// original prefix to a canonical <c>__fusion_{index}_</c> form. This way,
    /// structurally identical operations always produce the same normalized text.
    /// </remarks>
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
    /// Partitions a list of structurally identical operations into groups that can
    /// each be safely merged. Two operations cannot be in the same group if one
    /// transitively depends on the other, because merging them would create a cycle
    /// (the resulting batch node would depend on itself).
    /// </summary>
    private static List<List<OperationExecutionNode>> PartitionIntoMergeableGroups(
        List<OperationExecutionNode> candidates,
        Dictionary<int, HashSet<int>> dependencyLookup)
    {
        var groups = new List<List<OperationExecutionNode>>();
        var visited = new HashSet<int>();

        foreach (var candidate in candidates)
        {
            var placed = false;

            foreach (var group in groups)
            {
                var canJoin = true;

                foreach (var existing in group)
                {
                    visited.Clear();

                    if (IsTransitivelyReachable(candidate.Id, existing.Id, dependencyLookup, visited))
                    {
                        canJoin = false;
                        break;
                    }

                    visited.Clear();

                    if (IsTransitivelyReachable(existing.Id, candidate.Id, dependencyLookup, visited))
                    {
                        canJoin = false;
                        break;
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

    /// <summary>
    /// Checks whether <paramref name="targetId"/> is reachable from
    /// <paramref name="fromId"/> by following dependency edges.
    /// </summary>
    private static bool IsTransitivelyReachable(
        int fromId,
        int targetId,
        Dictionary<int, HashSet<int>> dependencyLookup,
        HashSet<int> visited)
    {
        if (!dependencyLookup.TryGetValue(fromId, out var deps))
        {
            return false;
        }

        foreach (var dep in deps)
        {
            if (dep == targetId)
            {
                return true;
            }

            if (visited.Add(dep) && IsTransitivelyReachable(dep, targetId, dependencyLookup, visited))
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
    /// Removes child selection sets from fields whose return type is not a value type.
    /// </summary>
    /// <remarks>
    /// The <see cref="ResultSelectionSet"/> only needs to track selections along
    /// value-type paths. By stripping the children of non-value-type fields here,
    /// we avoid building a large tree for the common case where most fields point
    /// to complex (non-value) types. This saves memory without losing any
    /// information the executor needs.
    /// </remarks>
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
                            // This field returns a value type, so its children may
                            // still contain non-value-type descendants. Recurse to
                            // prune those deeper levels.
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
                            // The field's return type is not a value type, so
                            // its selection set is irrelevant for result mapping.
                            // Strip the children to save memory.
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
        // During requirement rewriting, some fields or inline fragments may end up with
        // empty selection sets (literal `{}`). We strip those individual selections here.
        // This is a local cleanup pass and intentionally does not remove the entire
        // operation node, because other selections at the same level may still be valid.
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
    /// Extracts @skip and @include directives from every selection in the root
    /// selection set (or the selection set beneath a lookup field) and promotes
    /// them to node-level conditions on the plan step.
    /// </summary>
    /// <remarks>
    /// This is only called when every selection in the relevant set is conditional.
    /// Moving the conditions to the execution node lets the executor skip the
    /// entire downstream request when the condition evaluates to false.
    /// </remarks>
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

        // Combine the newly extracted conditions with any conditions that were already
        // propagated from earlier work items. The set automatically deduplicates by
        // value equality so we do not end up with duplicate condition checks.
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

    private sealed class ConditionalSelectionSetRewriterContext
    {
        public HashSet<ExecutionNodeCondition> Conditions { get; } = [];
    }
}

file static class Extensions
{
    private static readonly Encoding s_encoding = Encoding.UTF8;

    /// <summary>
    /// Determines whether every selection in the relevant selection set carries
    /// a @skip or @include directive, making it fully conditional.
    /// </summary>
    /// <remarks>
    /// The "relevant" selection set is either the root selection set or, when
    /// the step uses a lookup field, the selection set nested beneath that
    /// lookup field. If all selections are conditional, the caller can promote
    /// those conditions to the execution node and potentially skip the entire
    /// downstream request.
    /// </remarks>
    /// <returns>
    /// <c>true</c> if every selection in the set has a @skip or @include
    /// directive; <c>false</c> if any selection is unconditional.
    /// </returns>
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

file readonly record struct MergeResult(
    SelectionPath[] Targets,
    OperationSourceText CanonicalOp,
    OperationRequirement[] CanonicalRequirements,
    OperationExecutionNode Primary);
