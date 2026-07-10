using System.Collections.Immutable;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using Lookup = HotChocolate.Fusion.Types.Metadata.Lookup;

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
    /// Enqueues and scores a single plan node to this queue.
    /// </summary>
    public void Enqueue(PlanNode node)
        => _queue.Enqueue(node, PlannerCostEstimator.ScoreNode(node, schema));

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

    private void EnqueueRootPlanNodes(
        PlanNode planNodeTemplate,
        OperationWorkItem workItem)
    {
        foreach (var (schemaName, resolutionCost) in schema.GetPossibleSchemas(workItem.SelectionSet))
        {
            Enqueue(planNodeTemplate with
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

        // If this work item already carries a chosen lookup, keep it and
        // only align the node schema to the lookup schema.
        if (workItem.Lookup is not null)
        {
            var branchBacklog = backlog.Push(workItem);
            var branchRemainingCost = EstimateRemainingCost(planNodeTemplate, branchBacklog);

            Enqueue(planNodeTemplate with
            {
                SchemaName = workItem.Lookup.SchemaName,
                ResolutionCost = GetResolutionCost(workItem.SelectionSet, workItem.Lookup.SchemaName),
                Backlog = branchBacklog,
                RemainingCost = branchRemainingCost
            });
            return;
        }

        // For abstract types, try to resolve through per-concrete-type lookups
        // before falling through to the standard abstract-type lookup path.
        if (type.Kind is TypeKind.Interface or TypeKind.Union)
        {
            TryEnqueueConcreteTypeLookupPlanNodes(
                planNodeTemplate,
                workItem,
                backlog,
                allCandidateSchemas,
                type);
        }

        // Each branch starts from the same popped template and mutates a local copy
        // of backlog state. This avoids recomputing backlog shape from collections
        // for every candidate.
        foreach (var (toSchema, resolutionCost) in schema.GetPossibleSchemas(workItem.SelectionSet))
        {
            if (!workItem.AllowSourceSchemaReentry
                && toSchema.Equals(workItem.FromSchema, StringComparison.Ordinal))
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
                var branchRemainingCost = EstimateRemainingCost(planNodeTemplate, branchBacklog);
                Enqueue(planNodeTemplate with
                {
                    SchemaName = toSchema,
                    ResolutionCost = resolutionCost,
                    Backlog = branchBacklog,
                    RemainingCost = branchRemainingCost
                });

                // A self-cyclic direct lookup requires, as its input key, the very fields it is
                // asked to produce, so it cannot bootstrap that key on its own. In that case we do
                // not let it suppress the parent-path walk: the parent path can supply the key from
                // an enclosing entity, which yields an acyclic plan.
                if (IsSelfCyclicLookup(workItem, bestLookup))
                {
                    EnqueueParentPathLookupPlanNodes(planNodeTemplate, workItem, backlog, toSchema, resolutionCost);
                }

                continue;
            }

            var hasEnqueuedResolvingDirectLookup = false;
            foreach (var lookup in schema.GetPossibleLookupsOrdered(workItem.SelectionSet.Type, toSchema))
            {
                var lookupWorkItem = workItem with { Lookup = lookup };
                var branchBacklog = backlog.Push(lookupWorkItem);
                var branchRemainingCost = EstimateRemainingCost(planNodeTemplate, branchBacklog);
                Enqueue(planNodeTemplate with
                {
                    SchemaName = toSchema,
                    ResolutionCost = resolutionCost,
                    Backlog = branchBacklog,
                    RemainingCost = branchRemainingCost
                });

                // A self-cyclic lookup makes no progress on its own (see above), so it does not
                // count as a resolving direct lookup that can suppress the parent-path walk.
                if (!IsSelfCyclicLookup(workItem, lookup))
                {
                    hasEnqueuedResolvingDirectLookup = true;
                }
            }

            // If we did not find a direct lookup that can resolve the current selection set on its
            // own, we attempt to walk up the path we came from to see if we can lookup a parent type
            // or if we can just reuse the entire path we came from, e.g. viewer { ... }.
            if (!hasEnqueuedResolvingDirectLookup)
            {
                EnqueueParentPathLookupPlanNodes(planNodeTemplate, workItem, backlog, toSchema, resolutionCost);
            }
        }
    }

    /// <summary>
    /// Walks up the path of the work item's selection set to enqueue plan branches that resolve it
    /// through a parent-type lookup on <paramref name="toSchema"/> (or reuse the whole path up to the
    /// root, e.g. <c>viewer { ... }</c>).
    /// </summary>
    private void EnqueueParentPathLookupPlanNodes(
        PlanNode planNodeTemplate,
        OperationWorkItem workItem,
        Backlog backlog,
        string toSchema,
        double resolutionCost)
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
            var branchRemainingCost = EstimateRemainingCost(planNodeTemplate, branchBacklog);
            Enqueue(planNodeTemplate with
            {
                SchemaName = toSchema,
                SelectionSetIndex = index,
                ResolutionCost = resolutionCost + cost,
                Backlog = branchBacklog,
                RemainingCost = branchRemainingCost
            });
        }
    }

    /// <summary>
    /// Determines whether <paramref name="lookup"/> is self-cyclic for the given work item, meaning
    /// every field the work item asks it to produce is itself one of the key fields the lookup
    /// requires as input. Such a lookup cannot make progress on its own; the key must be supplied
    /// from the parent path instead.
    /// </summary>
    private static bool IsSelfCyclicLookup(OperationWorkItem workItem, Lookup lookup)
    {
        var requested = workItem.SelectionSet.Node.Selections;

        if (requested.Count == 0)
        {
            return false;
        }

        foreach (var selection in requested)
        {
            if (selection is not FieldNode field
                || !RequirementsContainField(lookup.Requirements, field.Name.Value))
            {
                return false;
            }
        }

        return true;
    }

    private static bool RequirementsContainField(SelectionSetNode requirements, string fieldName)
    {
        foreach (var selection in requirements.Selections)
        {
            if (selection is FieldNode field
                && field.Name.Value.Equals(fieldName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Resolves an abstract type (interface/union) by finding per-concrete-type lookups.
    /// First tries to keep all concrete types on a single schema (fewer network hops),
    /// then falls back to spreading each concrete type to its best available schema.
    /// </summary>
    private bool TryEnqueueConcreteTypeLookupPlanNodes(
        PlanNode planNodeTemplate,
        OperationWorkItem workItem,
        Backlog backlog,
        ImmutableHashSet<string> allCandidateSchemas,
        FusionComplexTypeDefinition type)
    {
        var enqueued = false;
        var hasCoveringAbstractLookup = false;

        // Phase 1: we try to find a single schema that can resolve all concrete types as
        // this would allow us to batch all requests to these into a single GraphQL batch request.
        foreach (var (toSchema, resolutionCost) in schema.GetPossibleSchemas(workItem.SelectionSet))
        {
            if (!workItem.AllowSourceSchemaReentry
                && toSchema.Equals(workItem.FromSchema, StringComparison.Ordinal))
            {
                continue;
            }

            var fromSchemas = allCandidateSchemas.Remove(toSchema);

            // If the target schema has a directly resolvable lookup returning the abstract type,
            // let the normal lookup path handle it instead of requiring a lookup for every concrete
            // type. Interface-object metadata alone is not enough: a non-resolvable key does not
            // provide a transition into the target schema.
            if (schema.TryGetBestDirectLookup(type, fromSchemas, toSchema, out var abstractLookup)
                && abstractLookup.FieldType.Name.Equals(type.Name, StringComparison.Ordinal))
            {
                hasCoveringAbstractLookup = true;
                continue;
            }

            var branchBacklog = backlog;
            var allFound = true;

            // for each concrete type that implements the abstract type,
            // find a lookup in the target schema.
            foreach (var possibleType in schema.GetPossibleTypes(type, includeInaccessible: true))
            {
                if (!schema.TryGetBestDirectLookup(possibleType, fromSchemas, toSchema, out var concreteLookup))
                {
                    concreteLookup = schema
                        .GetPossibleLookupsOrdered(possibleType, toSchema)
                        .FirstOrDefault(
                            t => !t.IsInternal
                                && t.FieldType.Name.Equals(possibleType.Name, StringComparison.Ordinal));
                }

                // If any concrete type lacks a lookup we skip this schema.
                if (concreteLookup is null)
                {
                    allFound = false;
                    break;
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

            if (!allFound)
            {
                continue;
            }

            // all concrete types have lookups in this schema, enqueue a single plan node
            // that fans out to each concrete type at execution time.
            var branchRemainingCost = EstimateRemainingCost(planNodeTemplate, branchBacklog);
            Enqueue(planNodeTemplate with
            {
                SchemaName = toSchema,
                ResolutionCost = resolutionCost,
                Backlog = branchBacklog,
                RemainingCost = branchRemainingCost
            });

            enqueued = true;
        }

        if (enqueued || hasCoveringAbstractLookup)
        {
            return enqueued;
        }

        // Phase 2: if we do not find a single schema that can can resolve all concrete types we
        // try to distribute each concrete type to the best available schema.
        var crossBacklog = backlog;
        string? topSchema = null;
        double topCost = 0;

        foreach (var possibleType in schema.GetPossibleTypes(type, includeInaccessible: true))
        {
            // rewrite the selection set to target the concrete type with a
            // fragment path so the executor can match the runtime type.
            var selectionSet = new SelectionSet(
                workItem.SelectionSet.Id,
                workItem.SelectionSet.Node,
                possibleType,
                workItem.SelectionSet.Path.AppendFragment(possibleType.Name));

            Lookup? concreteLookup = null;
            string? lookupSchema = null;
            double lookupCost = 0;

            // scan candidate schemas for the best lookup for this concrete type.
            foreach (var (candidateSchema, candidateCost) in schema.GetPossibleSchemas(selectionSet))
            {
                if (!workItem.AllowSourceSchemaReentry
                    && candidateSchema.Equals(workItem.FromSchema, StringComparison.Ordinal))
                {
                    continue;
                }

                if (schema.TryGetBestDirectLookup(
                        possibleType,
                        allCandidateSchemas.Remove(candidateSchema),
                        candidateSchema,
                        out var directLookup))
                {
                    concreteLookup = directLookup;
                    lookupSchema = candidateSchema;
                    lookupCost = candidateCost;
                    break;
                }

                var fallbackLookup = schema
                    .GetPossibleLookupsOrdered(possibleType, candidateSchema)
                    .FirstOrDefault(
                        t => !t.IsInternal
                            && t.FieldType.Name.Equals(possibleType.Name, StringComparison.Ordinal));

                if (fallbackLookup is not null)
                {
                    concreteLookup = fallbackLookup;
                    lookupSchema = candidateSchema;
                    lookupCost = candidateCost;
                    break;
                }
            }

            // If any concrete type lacks a lookup we bail out;
            // otherwise, we could end up with silent failures at runtime.
            if (concreteLookup is null)
            {
                return false;
            }

            // The backlog is LIFO, so the last pushed item is processed first.
            // Track its schema so the plan node's SchemaName matches.
            topSchema = lookupSchema;
            topCost = lookupCost;

            var lookupWorkItem = workItem with { SelectionSet = selectionSet, Lookup = concreteLookup };
            crossBacklog = crossBacklog.Push(lookupWorkItem);
        }

        if (topSchema is null)
        {
            return false;
        }

        // all concrete types have lookups, enqueue a single plan node
        // that fans out to each concrete type at execution time.
        var crossRemainingCost = EstimateRemainingCost(planNodeTemplate, crossBacklog);
        Enqueue(planNodeTemplate with
        {
            SchemaName = topSchema,
            ResolutionCost = topCost,
            Backlog = crossBacklog,
            RemainingCost = crossRemainingCost
        });

        return true;
    }

    private static double EstimateRemainingCost(PlanNode planNodeTemplate, Backlog branchBacklog)
        => PlannerCostEstimator.EstimateRemainingCost(
            planNodeTemplate.Options,
            planNodeTemplate.MaxDepth,
            planNodeTemplate.OpsPerLevel,
            branchBacklog.Cost);

    private double GetResolutionCost(SelectionSet selectionSet, string schemaName)
    {
        foreach (var (candidateSchema, candidateCost) in schema.GetPossibleSchemas(selectionSet))
        {
            if (candidateSchema.Equals(schemaName, StringComparison.Ordinal))
            {
                return candidateCost;
            }
        }

        throw new InvalidOperationException(
            $"Schema '{schemaName}' is not a valid candidate for selection set '{selectionSet.Type.Name}'.");
    }

    private void EnqueueNodeLookupPlanNodes(
        PlanNode planNodeTemplate,
        NodeLookupWorkItem workItem)
    {
        var backlog = planNodeTemplate.Backlog.Pop(out _);
        var type = workItem.SelectionSet.Type;
        var hasEnqueuedLookup = false;

        // Same branching rule as lookup work items:
        // copy backlog state per branch, then
        // materialize a new node with the
        // branch-local lower bound.
        foreach (var (schemaName, resolutionCost) in schema.GetPossibleSchemas(
            workItem.SelectionSet,
            workItem.SourceSchemaNodePolicy?.AllowedSchemaNames))
        {
            var policy = workItem.SourceSchemaNodePolicy;
            if (policy?.Allows(schemaName) == false)
            {
                continue;
            }

            // If we have multiple id lookups in a single schema,
            // we try to choose one that returns the desired type directly
            // and not an abstract type.
            var byIdLookup = SelectNodeBranchLookup(schema, type, schemaName);

            if (byIdLookup is null)
            {
                continue;
            }

            var candidateBacklog = backlog;
            if (policy is { CandidateGroupId: { } candidateGroupId })
            {
                policy = policy.BindCandidate(schemaName);
                candidateBacklog = backlog.BindSourceSchemaNodeCandidate(
                    candidateGroupId,
                    schemaName);
            }

            var lookupWorkItem = workItem with
            {
                Lookup = byIdLookup,
                SourceSchemaNodePolicy = policy
            };
            var branchBacklog = candidateBacklog.Push(lookupWorkItem);
            var branchRemainingCost = EstimateRemainingCost(planNodeTemplate, branchBacklog);
            Enqueue(planNodeTemplate with
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
            Lookup? byIdLookup;
            if (workItem.SourceSchemaNodePolicy?.AllowedSchemaNames is not { } allowedSchemaNames)
            {
                byIdLookup = SelectNodeBranchLookup(schema, type, schemaName: null);
            }
            else
            {
                byIdLookup = null;
                var schemaNames = allowedSchemaNames.ToArray();
                Array.Sort(schemaNames, StringComparer.Ordinal);

                foreach (var schemaName in schemaNames)
                {
                    byIdLookup = SelectNodeBranchLookup(schema, type, schemaName);
                    if (byIdLookup is not null)
                    {
                        break;
                    }
                }
            }

            if (byIdLookup is null)
            {
                if (workItem.SourceSchemaNodePolicy is not null)
                {
                    return;
                }

                throw new InvalidOperationException(
                    $"Expected to have at least one lookup with just an 'id' argument for type '{type.Name}'.");
            }

            var policy = workItem.SourceSchemaNodePolicy;
            var candidateBacklog = backlog;
            if (policy is { CandidateGroupId: { } candidateGroupId })
            {
                policy = policy.BindCandidate(byIdLookup.SchemaName);
                candidateBacklog = backlog.BindSourceSchemaNodeCandidate(
                    candidateGroupId,
                    byIdLookup.SchemaName);
            }

            var lookupWorkItem = workItem with
            {
                Lookup = byIdLookup,
                SourceSchemaNodePolicy = policy
            };
            var branchBacklog = candidateBacklog.Push(lookupWorkItem);
            var branchRemainingCost = EstimateRemainingCost(planNodeTemplate, branchBacklog);
            Enqueue(planNodeTemplate with
            {
                SchemaName = byIdLookup.SchemaName,
                Backlog = branchBacklog,
                RemainingCost = branchRemainingCost
            });
        }
    }

    // Selects the by-id lookup a per-type continuation branch uses. It must be
    // the source schema's public, non-internal by-id lookup: the composed `node`
    // field on Apollo schemas, or the native public by-id lookup elsewhere.
    // Synthetic internal key lookups are reserved for _entities entity resolution.
    private static Lookup? SelectNodeBranchLookup(
        FusionSchemaDefinition schema,
        ITypeDefinition type,
        string? schemaName)
        => schema
            .GetPossibleLookupsOrdered(type, schemaName)
            .FirstOrDefault(l => l.Fields is [PathNode { PathSegment.FieldName.Value: "id" }]
                && !l.IsInternal);

    private void EnqueueRequirePlanNodes(
        PlanNode planNodeTemplate,
        FieldRequirementWorkItem workItem)
    {
        var backlog = planNodeTemplate.Backlog.Pop(out _);
        var allCandidateSchemas = planNodeTemplate.GetCandidateSchemas(workItem.Selection.SelectionSetId);
        var selectionSetType = workItem.Selection.Field.DeclaringType;

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
                var inlineRemainingCost = EstimateRemainingCost(planNodeTemplate, inlineBacklog);
                Enqueue(planNodeTemplate with
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
                    var branchRemainingCost = EstimateRemainingCost(planNodeTemplate, branchBacklog);
                    Enqueue(planNodeTemplate with
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
                    var branchRemainingCost = EstimateRemainingCost(planNodeTemplate, branchBacklog);
                    Enqueue(planNodeTemplate with
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
                    var branchRemainingCost = EstimateRemainingCost(planNodeTemplate, branchBacklog);
                    Enqueue(planNodeTemplate with
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
                    var branchRemainingCost = EstimateRemainingCost(planNodeTemplate, branchBacklog);
                    Enqueue(planNodeTemplate with
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
