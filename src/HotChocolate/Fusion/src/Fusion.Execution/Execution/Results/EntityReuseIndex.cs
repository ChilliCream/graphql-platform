using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Results;

/// <summary>
/// Holds the <see cref="EntityReuse"/> of every lookup in an operation plan that can take
/// entities from the composite result instead of fetching them again.
/// </summary>
internal sealed class EntityReuseIndex
{
    private readonly EntityReuse?[] _reuseById;

    private EntityReuseIndex(EntityReuse?[] reuseById)
    {
        _reuseById = reuseById;
    }

    /// <summary>
    /// Gets the entity reuse of a lookup, or <c>null</c> if the lookup always fetches.
    /// </summary>
    /// <param name="lookupId">
    /// The id of an <see cref="OperationExecutionNode"/> or of an <see cref="OperationDefinition"/> in a batch.
    /// </param>
    public EntityReuse? GetReuse(int lookupId)
        => (uint)lookupId < (uint)_reuseById.Length ? _reuseById[lookupId] : null;

    /// <summary>
    /// Creates the entity reuse index of the specified plan.
    /// </summary>
    /// <returns>
    /// The index, or <c>null</c> if no lookup of the plan can reuse entities.
    /// </returns>
    public static EntityReuseIndex? Create(OperationPlan plan)
    {
        try
        {
            return CreateIndex(plan);
        }
        catch
        {
            // A plan that cannot be analyzed executes without entity reuse.
            return null;
        }
    }

    private static EntityReuseIndex? CreateIndex(OperationPlan plan)
    {
        if (plan.Operation.Definition.Operation is not OperationType.Query
            || !plan.IncrementalPlans.IsDefaultOrEmpty
            || !plan.DeliveryGroups.IsDefaultOrEmpty)
        {
            return null;
        }

        var writers = new Dictionary<(string Path, string ResponseName), HashSet<int>>();
        var lookups = new List<Lookup>();
        var nodesById = new Dictionary<int, ExecutionNode>();

        foreach (var node in plan.AllNodes)
        {
            nodesById[node.Id] = node;

            if (node is OperationBatchExecutionNode batchNode)
            {
                foreach (var definition in batchNode.Operations)
                {
                    nodesById[definition.Id] = node;
                }
            }
        }

        foreach (var node in plan.AllNodes)
        {
            switch (node)
            {
                case OperationExecutionNode operation:
                    if (!TryGetPathKey(operation.Target, out var targetKey)
                        || !TryAddWriters(writers, targetKey, operation.Operation, operation.Source, node.Id))
                    {
                        return null;
                    }

                    lookups.Add(new Lookup(
                        operation.Id,
                        operation.Dependencies.ToArray(),
                        operation.LookupTypeName,
                        operation.ResultSelectionSet,
                        operation.Requirements.ToArray(),
                        operation.ForwardedVariables.Length,
                        operation.Conditions.Length,
                        [targetKey]));
                    break;

                case OperationBatchExecutionNode batch:
                    foreach (var definition in batch.Operations)
                    {
                        var targets = GetTargets(definition);

                        if (targets is null)
                        {
                            return null;
                        }

                        var targetKeys = new string[targets.Length];

                        for (var i = 0; i < targets.Length; i++)
                        {
                            if (!TryGetPathKey(targets[i], out var key))
                            {
                                return null;
                            }

                            if (!TryAddWriters(writers, key, definition.SourceText, definition.Source, node.Id))
                            {
                                return null;
                            }

                            targetKeys[i] = key;
                        }

                        lookups.Add(new Lookup(
                            definition.Id,
                            definition.Dependencies.ToArray(),
                            definition.LookupTypeName,
                            definition.ResultSelectionSet,
                            definition.Requirements.ToArray(),
                            definition.ForwardedVariables.Length,
                            definition.Conditions.Length,
                            targetKeys));
                    }
                    break;

                default:
                    // Other node kinds can write anywhere in the result.
                    return null;
            }
        }

        var selectionSets = new Dictionary<string, SelectionSet>(StringComparer.Ordinal);
        var pathsByType = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        CollectSelectionSets(plan.Operation, plan.Operation.RootSelectionSet, string.Empty, selectionSets, pathsByType);

        EntityReuse?[]? reuseById = null;

        foreach (var lookup in lookups)
        {
            if (TryCreateReuse(nodesById, lookup, writers, selectionSets, pathsByType) is { } reuse)
            {
                reuseById ??= new EntityReuse?[plan.MaxNodeId + 1];
                reuseById[lookup.Id] = reuse;
            }
        }

        return reuseById is null ? null : new EntityReuseIndex(reuseById);
    }

    private static EntityReuse? TryCreateReuse(
        Dictionary<int, ExecutionNode> nodesById,
        Lookup lookup,
        Dictionary<(string Path, string ResponseName), HashSet<int>> writers,
        Dictionary<string, SelectionSet> selectionSets,
        Dictionary<string, List<string>> pathsByType)
    {
        var resultSelectionSet = lookup.ResultSelectionSet;

        if (lookup.LookupTypeName is null
            || lookup.Requirements.Length == 0
            || lookup.ForwardedVariableCount > 0
            || lookup.ConditionCount > 0
            || resultSelectionSet.HasSourceResponseNameMappings
            || resultSelectionSet.ProducesOpaqueElements
            || resultSelectionSet.Fragments.Length > 0
            || !pathsByType.TryGetValue(lookup.LookupTypeName, out var candidatePaths))
        {
            return null;
        }

        var responseNames = new string[resultSelectionSet.Selections.Length];

        for (var i = 0; i < responseNames.Length; i++)
        {
            var selection = resultSelectionSet.Selections[i];

            if (selection.Child is not null)
            {
                return null;
            }

            responseNames[i] = selection.ResponseName;
        }

        var requirementFields = new string[lookup.Requirements.Length];

        for (var i = 0; i < requirementFields.Length; i++)
        {
            var requirement = lookup.Requirements[i];

            if (requirement.InternalAlias is not null
                || !FetchResultStore.TryGetSimpleRequirementFieldName(requirement, out var fieldName))
            {
                return null;
            }

            requirementFields[i] = fieldName;
        }

        // Every target must select the lookup fields so we know which field and arguments they stand for.
        var targetSelectionSets = new SelectionSet[lookup.TargetKeys.Length];

        for (var i = 0; i < targetSelectionSets.Length; i++)
        {
            if (!selectionSets.TryGetValue(lookup.TargetKeys[i], out var targetSelectionSet)
                || !targetSelectionSet.Type.Name.Equals(lookup.LookupTypeName, StringComparison.Ordinal))
            {
                return null;
            }

            foreach (var responseName in responseNames)
            {
                if (!targetSelectionSet.TryGetSelection(responseName, out var targetSelection)
                    || !targetSelection.IsLeaf)
                {
                    return null;
                }
            }

            targetSelectionSets[i] = targetSelectionSet;
        }

        if (CollectTransitiveDependencies(nodesById, lookup.Dependencies) is not { } dependencies)
        {
            return null;
        }

        List<SelectionPath>? producerPaths = null;

        foreach (var candidatePath in candidatePaths)
        {
            if (Array.IndexOf(lookup.TargetKeys, candidatePath) >= 0)
            {
                continue;
            }

            var producerSelectionSet = selectionSets[candidatePath];

            if (!IsProducer(
                candidatePath,
                producerSelectionSet,
                responseNames,
                requirementFields,
                targetSelectionSets,
                writers,
                dependencies))
            {
                continue;
            }

            producerPaths ??= [];
            producerPaths.Add(ToSelectionPath(candidatePath));
        }

        return producerPaths is null ? null : new EntityReuse([.. producerPaths], responseNames);
    }

    private static bool IsProducer(
        string path,
        SelectionSet producerSelectionSet,
        string[] responseNames,
        string[] requirementFields,
        SelectionSet[] targetSelectionSets,
        Dictionary<(string Path, string ResponseName), HashSet<int>> writers,
        HashSet<int> dependencies)
    {
        foreach (var responseName in responseNames)
        {
            if (!producerSelectionSet.TryGetSelection(responseName, out var producerSelection)
                || producerSelection.IsConditional
                || !IsWrittenOnlyBy(writers, path, responseName, dependencies))
            {
                return false;
            }

            // The producer must hold the same field with the same arguments as every target.
            foreach (var targetSelectionSet in targetSelectionSets)
            {
                targetSelectionSet.TryGetSelection(responseName, out var targetSelection);

                if (targetSelection is null
                    || targetSelection.IsConditional
                    || !ReferenceEquals(targetSelection.Field, producerSelection.Field)
                    || !HaveSameArguments(targetSelection, producerSelection))
                {
                    return false;
                }
            }
        }

        foreach (var fieldName in requirementFields)
        {
            if (!producerSelectionSet.TryGetSelection(fieldName, out var producerSelection)
                || producerSelection.IsConditional
                || !producerSelection.Field.Name.Equals(fieldName, StringComparison.Ordinal)
                || !IsWrittenOnlyBy(writers, path, fieldName, dependencies))
            {
                return false;
            }

            // The producer must hold the requirement field itself, not an alias of another field.
            foreach (var targetSelectionSet in targetSelectionSets)
            {
                targetSelectionSet.TryGetSelection(fieldName, out var targetSelection);

                if (targetSelection is null
                    || !ReferenceEquals(targetSelection.Field, producerSelection.Field)
                    || !HaveSameArguments(targetSelection, producerSelection))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsWrittenOnlyBy(
        Dictionary<(string Path, string ResponseName), HashSet<int>> writers,
        string path,
        string responseName,
        HashSet<int> dependencies)
        => writers.TryGetValue((path, responseName), out var nodeIds)
            && nodeIds.IsSubsetOf(dependencies);

    private static bool HaveSameArguments(Selection a, Selection b)
    {
        var argumentsA = a.SyntaxNodes[0].Node.Arguments;
        var argumentsB = b.SyntaxNodes[0].Node.Arguments;

        if (argumentsA.Count != argumentsB.Count)
        {
            return false;
        }

        for (var i = 0; i < argumentsA.Count; i++)
        {
            var argumentA = argumentsA[i];
            var argumentB = argumentsB.FirstOrDefault(
                t => t.Name.Value.Equals(argumentA.Name.Value, StringComparison.Ordinal));

            if (argumentB is null || !SyntaxComparer.BySyntax.Equals(argumentA.Value, argumentB.Value))
            {
                return false;
            }
        }

        return true;
    }

    // Collects the execution nodes that complete before a lookup executes, or returns null
    // when a dependency is not part of the plan.
    private static HashSet<int>? CollectTransitiveDependencies(
        Dictionary<int, ExecutionNode> nodesById,
        IOperationPlanNode[] dependencies)
    {
        var visited = new HashSet<int>();
        var stack = new Stack<IOperationPlanNode>(dependencies);

        while (stack.TryPop(out var dependency))
        {
            if (!nodesById.TryGetValue(dependency.Id, out var node))
            {
                return null;
            }

            if (!visited.Add(node.Id))
            {
                continue;
            }

            foreach (var next in node.Dependencies)
            {
                stack.Push(next);
            }

            if (node is OperationBatchExecutionNode batch)
            {
                foreach (var operation in batch.Operations)
                {
                    foreach (var next in operation.Dependencies)
                    {
                        stack.Push(next);
                    }
                }
            }
        }

        return visited;
    }

    // Records the result fields a node writes: the selection set of its source schema operation
    // at the source path, mapped onto the target path.
    private static bool TryAddWriters(
        Dictionary<(string Path, string ResponseName), HashSet<int>> writers,
        string targetPath,
        OperationSourceText sourceText,
        SelectionPath sourcePath,
        int nodeId)
    {
        var document = Utf8GraphQLParser.Parse(sourceText.Value.Span, ParserOptions.Trusted);

        if (document.Definitions.Count != 1
            || document.Definitions[0] is not OperationDefinitionNode operation)
        {
            return false;
        }

        var selectionSet = operation.SelectionSet;

        for (var i = 0; i < sourcePath.Length; i++)
        {
            var segment = sourcePath[i];

            if (segment.Kind is not SelectionPathSegmentKind.Field
                || FindField(selectionSet, segment.Name) is not { SelectionSet: { } child })
            {
                return false;
            }

            selectionSet = child;
        }

        return TryAddWriters(writers, targetPath, selectionSet, nodeId);
    }

    private static bool TryAddWriters(
        Dictionary<(string Path, string ResponseName), HashSet<int>> writers,
        string path,
        SelectionSetNode selectionSet,
        int nodeId)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    var responseName = field.Alias?.Value ?? field.Name.Value;
                    AddWriter(writers, path, responseName, nodeId);
                    AddWriter(writers, path, field.Name.Value, nodeId);

                    foreach (var directive in field.Directives)
                    {
                        foreach (var argument in directive.Arguments)
                        {
                            if (argument.Value is StringValueNode { Value: var name })
                            {
                                AddWriter(writers, path, name, nodeId);
                            }
                        }
                    }

                    if (field.SelectionSet is { } child
                        && !TryAddWriters(writers, AppendPath(path, responseName), child, nodeId))
                    {
                        return false;
                    }

                    break;

                case InlineFragmentNode fragment:
                    if (!TryAddWriters(writers, path, fragment.SelectionSet, nodeId))
                    {
                        return false;
                    }

                    break;

                default:
                    return false;
            }
        }

        return true;
    }

    private static FieldNode? FindField(SelectionSetNode selectionSet, string responseName)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode field
                && (field.Alias?.Value ?? field.Name.Value).Equals(responseName, StringComparison.Ordinal))
            {
                return field;
            }
        }

        return null;
    }

    private static void AddWriter(
        Dictionary<(string Path, string ResponseName), HashSet<int>> writers,
        string path,
        string responseName,
        int nodeId)
    {
        if (!writers.TryGetValue((path, responseName), out var nodeIds))
        {
            nodeIds = [];
            writers.Add((path, responseName), nodeIds);
        }

        nodeIds.Add(nodeId);
    }

    private static void CollectSelectionSets(
        Operation operation,
        SelectionSet selectionSet,
        string path,
        Dictionary<string, SelectionSet> selectionSets,
        Dictionary<string, List<string>> pathsByType)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (selection.IsLeaf || selection.NamedType is not IObjectTypeDefinition objectType)
            {
                continue;
            }

            var childPath = AppendPath(path, selection.ResponseName);
            var childSelectionSet = operation.GetSelectionSet(selection, objectType);

            if (!selectionSets.TryAdd(childPath, childSelectionSet))
            {
                continue;
            }

            if (!pathsByType.TryGetValue(objectType.Name, out var paths))
            {
                paths = [];
                pathsByType.Add(objectType.Name, paths);
            }

            paths.Add(childPath);
            CollectSelectionSets(operation, childSelectionSet, childPath, selectionSets, pathsByType);
        }
    }

    private static bool TryGetPathKey(SelectionPath path, out string key)
    {
        if (path.IsRoot)
        {
            key = string.Empty;
            return true;
        }

        var segments = new string[path.Length];

        for (var i = 0; i < path.Length; i++)
        {
            var segment = path[i];

            if (segment.Kind is not SelectionPathSegmentKind.Field)
            {
                key = string.Empty;
                return false;
            }

            segments[i] = segment.Name;
        }

        key = string.Join('.', segments);
        return true;
    }

    private static SelectionPath[]? GetTargets(OperationDefinition definition)
        => definition switch
        {
            SingleOperationDefinition single => [single.Target],
            BatchOperationDefinition batch => batch.Targets.ToArray(),
            _ => null
        };

    private static string AppendPath(string path, string responseName)
        => path.Length == 0 ? responseName : path + "." + responseName;

    private static SelectionPath ToSelectionPath(string key)
    {
        var builder = SelectionPath.CreateBuilder();

        foreach (var segment in key.Split('.'))
        {
            builder = builder.AppendField(segment);
        }

        return builder.Build();
    }

    private readonly record struct Lookup(
        int Id,
        IOperationPlanNode[] Dependencies,
        string? LookupTypeName,
        ResultSelectionSet ResultSelectionSet,
        OperationRequirement[] Requirements,
        int ForwardedVariableCount,
        int ConditionCount,
        string[] TargetKeys);
}
