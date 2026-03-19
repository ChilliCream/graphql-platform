using System.Collections.Immutable;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// A priority queue of plan nodes, ordered by cost (lowest first).
/// Handles scoring and branch expansion so the planner can focus on
/// search logic rather than queue mechanics.
/// </summary>
internal sealed class PlanQueue(FusionSchemaDefinition schema)
{
    private readonly PriorityQueue<PlanNode, double> _queue = new();

    /// <summary>
    /// The number of plan nodes currently in the queue.
    /// </summary>
    public int Count => _queue.Count;

    /// <summary>
    /// Removes and returns the cheapest plan node.
    /// </summary>
    public bool TryDequeue(out PlanNode node, out double priority)
        => _queue.TryDequeue(out node!, out priority);

    /// <summary>
    /// Returns the cheapest plan node without removing it.
    /// </summary>
    public bool TryPeek(out PlanNode node, out double priority)
        => _queue.TryPeek(out node!, out priority);

    /// <summary>
    /// Removes all plan nodes from the queue.
    /// </summary>
    public void Clear() => _queue.Clear();

    /// <summary>
    /// Expands a plan node's next work item into all possible branches
    /// (one per candidate schema or lookup) and enqueues each branch.
    /// </summary>
    public void EnqueueBranches(PlanNode planNodeTemplate)
    {
        var nextWorkItem =
            planNodeTemplate.Backlog.IsEmpty
                ? null
                : planNodeTemplate.Backlog.Peek();

        // we reset the resolution cost so that the next plan is not chosen based
        // on the last resolutions cost.
        planNodeTemplate = planNodeTemplate with { ResolutionCost = 0 };

        switch (nextWorkItem)
        {
            case null:
            case NodeFieldWorkItem:
                Enqueue(planNodeTemplate);
                break;

            case OperationWorkItem { Kind: OperationWorkItemKind.Root } wi:
                EnqueueRootPlanNodes(planNodeTemplate, wi);
                break;

            case OperationWorkItem { Kind: OperationWorkItemKind.Lookup } wi:
                EnqueueLookupPlanNodes(planNodeTemplate, wi);
                break;

            case FieldRequirementWorkItem wi:
                EnqueueRequirePlanNodes(planNodeTemplate, wi);
                break;

            case NodeLookupWorkItem { Lookup: null } wi:
                EnqueueNodeLookupPlanNodes(planNodeTemplate, wi);
                break;

            default:
                throw new NotSupportedException(
                    "The work item type is not supported.");
        }
    }

    /// <summary>
    /// Enqueues and scores a single plan node to this queue.
    /// </summary>
    public void Enqueue(PlanNode node)
        => _queue.Enqueue(node, PlannerCostEstimator.ScoreNode(node, schema));

    private void EnqueueRootPlanNodes(
        PlanNode planNodeTemplate,
        OperationWorkItem workItem)
    {
        foreach (var (schemaName, resolutionCost) in schema.GetPossibleSchemas(workItem.SelectionSet))
        {
            Enqueue(
                planNodeTemplate with
                {
                    SchemaName = schemaName,
                    ResolutionCost = resolutionCost
                });
        }
    }

    private void EnqueueLookupPlanNodes(
        PlanNode planNodeTemplate,
        OperationWorkItem workItem)
    {
        var backlog = planNodeTemplate.Backlog.Pop(out _);
        var allCandidateSchemas = planNodeTemplate.GetCandidateSchemas(workItem.SelectionSet.Id);
        var type = (FusionComplexTypeDefinition)workItem.SelectionSet.Type;

        double EstimateBranchLowerBound(Backlog branchBacklog)
            => PlannerCostEstimator.EstimateRemainingCost(
                planNodeTemplate.Options,
                planNodeTemplate.MaxDepth,
                planNodeTemplate.OpsPerLevel,
                branchBacklog.Cost);

        // Each branch starts from the same popped template and
        // mutates a local copy of backlog state.
        // This avoids recomputing backlog shape
        // from collections for every candidate.
        foreach (var (toSchema, resolutionCost) in schema.GetPossibleSchemas(workItem.SelectionSet))
        {
            if (toSchema.Equals(workItem.FromSchema, StringComparison.Ordinal))
            {
                continue;
            }

            // if the target schema has no lookup for the abstract type itself,
            // try resolving through per-concrete-type lookups instead.
            if (type.Kind is TypeKind.Interface or TypeKind.Union
                && TryEnqueueConcreteTypeLookupPlanNode(toSchema, resolutionCost))
            {
                continue;
            }

            if (schema.TryGetBestDirectLookup(
                type,
                allCandidateSchemas.Remove(toSchema),
                toSchema,
                out var bestLookup))
            {
                var lookupWorkItem = workItem with { Lookup = bestLookup };
                var branchBacklog = backlog.Push(lookupWorkItem);
                var branchRemainingCost = EstimateBranchLowerBound(branchBacklog);
                Enqueue(
                    planNodeTemplate with
                    {
                        SchemaName = toSchema,
                        ResolutionCost = resolutionCost,
                        Backlog = branchBacklog,
                        RemainingCost = branchRemainingCost
                    });
                continue;
            }

            var hasEnqueuedDirectLookup = false;
            foreach (var lookup in schema.GetPossibleLookupsOrdered(workItem.SelectionSet.Type, toSchema))
            {
                var lookupWorkItem = workItem with { Lookup = lookup };
                var branchBacklog = backlog.Push(lookupWorkItem);
                var branchRemainingCost = EstimateBranchLowerBound(branchBacklog);
                Enqueue(
                    planNodeTemplate with
                    {
                        SchemaName = toSchema,
                        ResolutionCost = resolutionCost,
                        Backlog = branchBacklog,
                        RemainingCost = branchRemainingCost
                    });

                hasEnqueuedDirectLookup = true;
            }

            // If we did not find a direct lookup for the type of the current selection set,
            // we attempt to walk up the path we came from to see if we can lookup a parent
            // type or if we can just reuse the entire path we came from, e.g. viewer { ... }.
            if (!hasEnqueuedDirectLookup)
            {
                foreach (var (lookupThroughPathWorkItem, cost, index) in PlannerExtensions.GetPossibleLookupsThroughPath(
                    planNodeTemplate,
                    workItem,
                    toSchema,
                    schema).OrderBy(
                    t => PlannerExtensions.LookupOrderingKey(t.WorkItem.Lookup),
                    StringComparer.Ordinal))
                {
                    var branchBacklog = backlog.Push(lookupThroughPathWorkItem);
                    var branchRemainingCost = EstimateBranchLowerBound(branchBacklog);
                    Enqueue(
                        planNodeTemplate with
                        {
                            SchemaName = toSchema,
                            SelectionSetIndex = index,
                            ResolutionCost = resolutionCost + cost,
                            Backlog = branchBacklog,
                            RemainingCost = branchRemainingCost
                        });
                }
            }
        }

        // When the target schema has no lookup that returns the abstract type directly,
        // we try to resolve through per-concrete-type lookups instead.
        bool TryEnqueueConcreteTypeLookupPlanNode(string toSchema, double resolutionCost)
        {
            // if the target schema already has a lookup returning the abstract type,
            // let the normal lookup path handle it.
            var hasAbstractLookups = schema
                .GetPossibleLookupsOrdered(type, toSchema)
                .Any(t => t.FieldType.Name.Equals(type.Name, StringComparison.Ordinal));

            if (hasAbstractLookups)
            {
                return false;
            }

            var branchBacklog = backlog;
            var fromSchemas = allCandidateSchemas.Remove(toSchema);

            // for each concrete type that implements the abstract type,
            // find a lookup in the target schema.
            foreach (var possibleType in schema.GetPossibleTypes(type))
            {
                if (!schema.TryGetBestDirectLookup(possibleType, fromSchemas, toSchema, out var concreteLookup))
                {
                    concreteLookup = schema
                        .GetPossibleLookupsOrdered(possibleType, toSchema)
                        .FirstOrDefault(
                            t => !t.IsInternal
                                && t.FieldType.Name.Equals(possibleType.Name, StringComparison.Ordinal));
                }

                // If any concrete type lacks a lookup we bail out so the normal path can try instead.
                // otherwise, we could end up with silent failures at runtime.
                if (concreteLookup is null)
                {
                    return false;
                }

                // rewrite the selection set to target the concrete type with a
                // fragment path so the executor can match the runtime type.
                var selectionSet = new SelectionSet(
                    workItem.SelectionSet.Id,
                    workItem.SelectionSet.Node,
                    possibleType,
                    workItem.SelectionSet.Path.AppendFragment(possibleType.Name));

                var lookupWorkItem = workItem with { SelectionSet = selectionSet, Lookup = concreteLookup };
                branchBacklog = branchBacklog.Push(lookupWorkItem);
            }

            // all concrete types have lookups, enqueue a single plan node
            // that fans out to each concrete type at execution time.
            var branchRemainingCost = EstimateBranchLowerBound(branchBacklog);
            Enqueue(planNodeTemplate with
            {
                SchemaName = toSchema,
                ResolutionCost = resolutionCost,
                Backlog = branchBacklog,
                RemainingCost = branchRemainingCost
            });

            return true;
        }
    }

    private void EnqueueNodeLookupPlanNodes(
        PlanNode planNodeTemplate,
        NodeLookupWorkItem workItem)
    {
        var backlog = planNodeTemplate.Backlog.Pop(out _);
        var type = workItem.SelectionSet.Type;
        var hasEnqueuedLookup = false;

        double EstimateBranchLowerBound(Backlog branchBacklog)
            => PlannerCostEstimator.EstimateRemainingCost(
                planNodeTemplate.Options,
                planNodeTemplate.MaxDepth,
                planNodeTemplate.OpsPerLevel,
                branchBacklog.Cost);

        // Same branching rule as lookup work items:
        // copy backlog state per branch, then
        // materialize a new node with the
        // branch-local lower bound.
        foreach (var (schemaName, resolutionCost) in schema.GetPossibleSchemas(workItem.SelectionSet))
        {
            // If we have multiple id lookups in a single schema,
            // we try to choose one that returns the desired type directly
            // and not an abstract type.
            var byIdLookup = schema
                .GetPossibleLookupsOrdered(type, schemaName)
                .FirstOrDefault(l => l.Fields is [PathNode { PathSegment.FieldName.Value: "id" }] && !l.IsInternal);

            if (byIdLookup is null)
            {
                continue;
            }

            var lookupWorkItem = workItem with { Lookup = byIdLookup };
            var branchBacklog = backlog.Push(lookupWorkItem);
            var branchRemainingCost = EstimateBranchLowerBound(branchBacklog);
            Enqueue(
                planNodeTemplate with
                {
                    SchemaName = schemaName,
                    ResolutionCost = resolutionCost,
                    Backlog = branchBacklog,
                    RemainingCost = branchRemainingCost
                });

            hasEnqueuedLookup = true;
        }

        // It could be that we didn't find a suitable source schema for the requested selections
        // that also has a by id resolver.
        // In this case we enqueue the best matching by id lookup of any source schema.
        if (!hasEnqueuedLookup)
        {
            var byIdLookup = schema
                .GetPossibleLookupsOrdered(type)
                .FirstOrDefault(l => l.Fields is [PathNode { PathSegment.FieldName.Value: "id" }] && !l.IsInternal)
                    ?? throw new InvalidOperationException(
                        $"Expected to have at least one lookup with just an 'id' argument for type '{type.Name}'.");

            var lookupWorkItem = workItem with { Lookup = byIdLookup };
            var branchBacklog = backlog.Push(lookupWorkItem);
            var branchRemainingCost = EstimateBranchLowerBound(branchBacklog);
            Enqueue(
                planNodeTemplate with
                {
                    SchemaName = byIdLookup.SchemaName,
                    Backlog = branchBacklog,
                    RemainingCost = branchRemainingCost
                });
        }
    }

    private void EnqueueRequirePlanNodes(
        PlanNode planNodeTemplate,
        FieldRequirementWorkItem workItem)
    {
        var backlog = planNodeTemplate.Backlog.Pop(out _);
        var allCandidateSchemas = planNodeTemplate.GetCandidateSchemas(workItem.Selection.SelectionSetId);
        var selectionSetType = workItem.Selection.Field.DeclaringType;

        double EstimateBranchLowerBound(Backlog branchBacklog)
            => PlannerCostEstimator.EstimateRemainingCost(
                planNodeTemplate.Options,
                planNodeTemplate.MaxDepth,
                planNodeTemplate.OpsPerLevel,
                branchBacklog.Cost);

        // Requirement planning can fork into inline and lookup paths.
        // Both are scored from the same popped template by cloning and
        // mutating backlog state per candidate.
        var requirementSchemas =
            schema.TryGetFieldResolution(selectionSetType, workItem.Selection.Field.Name, out var fieldResolution)
                ? fieldResolution.Schemas
                : workItem.Selection.Field.Sources.Schemas.OrderBy(static t => t, StringComparer.Ordinal).ToImmutableArray();

        foreach (var schemaName in requirementSchemas)
        {
            var candidateSchemas = allCandidateSchemas.Remove(schemaName);

            if (schemaName == planNodeTemplate.SchemaName)
            {
                var inlineBacklog = backlog.Push(workItem);
                var inlineRemainingCost = EstimateBranchLowerBound(inlineBacklog);
                Enqueue(
                    planNodeTemplate with
                    {
                        Backlog = inlineBacklog,
                        RemainingCost = inlineRemainingCost,
                    });

                if (schema.TryGetBestDirectLookup(
                    selectionSetType,
                    candidateSchemas.Remove(schemaName),
                    schemaName,
                    out var bestLookup))
                {
                    var lookupWorkItem = workItem with { Lookup = bestLookup };
                    var branchBacklog = backlog.Push(lookupWorkItem);
                    var branchRemainingCost = EstimateBranchLowerBound(branchBacklog);
                    Enqueue(
                        planNodeTemplate with
                        {
                            SchemaName = schemaName,
                            Backlog = branchBacklog,
                            RemainingCost = branchRemainingCost
                        });
                    continue;
                }

                foreach (var lookup in schema.GetPossibleLookupsOrdered(selectionSetType, schemaName))
                {
                    var lookupWorkItem = workItem with { Lookup = lookup };
                    var branchBacklog = backlog.Push(lookupWorkItem);
                    var branchRemainingCost = EstimateBranchLowerBound(branchBacklog);
                    Enqueue(
                        planNodeTemplate with
                        {
                            SchemaName = schemaName,
                            Backlog = branchBacklog,
                            RemainingCost = branchRemainingCost
                        });
                }
            }
            else
            {
                if (schema.TryGetBestDirectLookup(
                    selectionSetType,
                    candidateSchemas,
                    schemaName,
                    out var bestLookup))
                {
                    var lookupWorkItem = workItem with { Lookup = bestLookup };
                    var branchBacklog = backlog.Push(lookupWorkItem);
                    var branchRemainingCost = EstimateBranchLowerBound(branchBacklog);
                    Enqueue(
                        planNodeTemplate with
                        {
                            SchemaName = schemaName,
                            Backlog = branchBacklog,
                            RemainingCost = branchRemainingCost
                        });
                    continue;
                }

                foreach (var lookup in schema.GetPossibleLookupsOrdered(selectionSetType, schemaName))
                {
                    var lookupWorkItem = workItem with { Lookup = lookup };
                    var branchBacklog = backlog.Push(lookupWorkItem);
                    var branchRemainingCost = EstimateBranchLowerBound(branchBacklog);
                    Enqueue(
                        planNodeTemplate with
                        {
                            SchemaName = schemaName,
                            Backlog = branchBacklog,
                            RemainingCost = branchRemainingCost
                        });
                }
            }
        }
    }
}
