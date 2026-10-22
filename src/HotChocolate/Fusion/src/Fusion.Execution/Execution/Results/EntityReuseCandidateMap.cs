using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.ApolloFederation;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Results;

/// <summary>
/// Holds the <see cref="EntityReuseCandidateList"/> of every lookup in an operation plan that may
/// take entities from the composite result instead of fetching them again.
/// </summary>
internal sealed class EntityReuseCandidateMap
{
    private readonly EntityReuseCandidateList?[] _candidatesById;

    private EntityReuseCandidateMap(EntityReuseCandidateList?[] candidatesById)
    {
        _candidatesById = candidatesById;
    }

    /// <summary>
    /// Gets the reuse candidates of a lookup, or <c>null</c> if the lookup always fetches.
    /// </summary>
    /// <param name="lookupId">
    /// The id of an <see cref="OperationExecutionNode"/>, of an <see cref="ApolloOperationExecutionNode"/>,
    /// or of an <see cref="OperationDefinition"/> in a batch.
    /// </param>
    public EntityReuseCandidateList? GetCandidates(int lookupId)
        => (uint)lookupId < (uint)_candidatesById.Length ? _candidatesById[lookupId] : null;

    /// <summary>
    /// Creates the reuse candidate map of the specified plan.
    /// </summary>
    /// <returns>
    /// The map, or <c>null</c> if no lookup of the plan has reuse candidates.
    /// </returns>
    public static EntityReuseCandidateMap? Create(OperationPlan plan)
    {
        try
        {
            return CreateMap(plan);
        }
        catch
        {
            // A plan that cannot be analyzed executes without entity reuse.
            return null;
        }
    }

    private static EntityReuseCandidateMap? CreateMap(OperationPlan plan)
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

            foreach (var definition in GetBatchOperations(node))
            {
                nodesById[definition.Id] = node;
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
                        [.. operation.Dependencies, .. operation.OptionalDependencies],
                        operation.LookupTypeName,
                        operation.ResultSelectionSet,
                        operation.Requirements.ToArray(),
                        operation.ForwardedVariables.Length,
                        operation.Conditions.Length,
                        [targetKey],
                        RepresentationShape: default));
                    break;

                case OperationBatchExecutionNode batch:
                    foreach (var definition in batch.Operations)
                    {
                        if (!TryAddBatchLookup(
                            writers,
                            lookups,
                            definition,
                            definition.SourceText,
                            definition.LookupTypeName,
                            representationShape: default,
                            node.Id))
                        {
                            return null;
                        }
                    }
                    break;

                case ApolloOperationExecutionNode apolloOperation:
                    if (!TryGetPathKey(apolloOperation.Target, out var apolloTargetKey)
                        || !TryAddWriters(
                            writers,
                            apolloTargetKey,
                            apolloOperation.Lookup.Operation,
                            apolloOperation.Source,
                            node.Id))
                    {
                        return null;
                    }

                    lookups.Add(new Lookup(
                        apolloOperation.Id,
                        apolloOperation.Dependencies.ToArray(),
                        apolloOperation.Lookup.EntityTypeName,
                        apolloOperation.ResultSelectionSet,
                        apolloOperation.Requirements.ToArray(),
                        apolloOperation.ForwardedVariables.Length,
                        apolloOperation.Conditions.Length,
                        [apolloTargetKey],
                        apolloOperation.Lookup.RepresentationShape));
                    break;

                case ApolloOperationBatchExecutionNode apolloBatch:
                    for (var i = 0; i < apolloBatch.Operations.Length; i++)
                    {
                        var apolloLookup = apolloBatch.Lookups[i];

                        if (!TryAddBatchLookup(
                            writers,
                            lookups,
                            apolloBatch.Operations[i],
                            apolloLookup.Operation,
                            apolloLookup.EntityTypeName,
                            apolloLookup.RepresentationShape,
                            node.Id))
                        {
                            return null;
                        }
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

        EntityReuseCandidateList?[]? candidatesById = null;

        foreach (var lookup in lookups)
        {
            if (TryCreateCandidates(nodesById, lookup, writers, selectionSets, pathsByType) is { } candidates)
            {
                candidatesById ??= new EntityReuseCandidateList?[plan.MaxNodeId + 1];
                candidatesById[lookup.Id] = candidates;
            }
        }

        return candidatesById is null ? null : new EntityReuseCandidateMap(candidatesById);
    }

    /// <summary>
    /// Gets the response names of the leaf fields a lookup selects and of its key fields.
    /// </summary>
    /// <returns>
    /// <c>false</c> if the lookup can never take its entities from the composite result.
    /// </returns>
    internal static bool TryGetReusableFields(
        ResultSelectionSet resultSelectionSet,
        ReadOnlySpan<OperationRequirement> requirements,
        int forwardedVariableCount,
        int conditionCount,
        [NotNullWhen(true)] out string[]? responseNames,
        [NotNullWhen(true)] out string[]? keyResponseNames)
    {
        responseNames = null;
        keyResponseNames = null;

        if (requirements.Length == 0
            || forwardedVariableCount > 0
            || conditionCount > 0
            || resultSelectionSet.HasSourceResponseNameMappings
            || resultSelectionSet.ProducesOpaqueElements
            || resultSelectionSet.Fragments.Length > 0)
        {
            return false;
        }

        var leafNames = new string[resultSelectionSet.Selections.Length];

        for (var i = 0; i < leafNames.Length; i++)
        {
            var selection = resultSelectionSet.Selections[i];

            if (selection.Child is not null)
            {
                return false;
            }

            leafNames[i] = selection.ResponseName;
        }

        var keyNames = new string[requirements.Length];

        for (var i = 0; i < keyNames.Length; i++)
        {
            var requirement = requirements[i];

            if (requirement.InternalAlias is not null
                || !FetchResultStore.TryGetSimpleRequirementFieldName(requirement, out var fieldName))
            {
                return false;
            }

            keyNames[i] = fieldName;
        }

        responseNames = leafNames;
        keyResponseNames = keyNames;
        return true;
    }

    private static EntityReuseCandidateList? TryCreateCandidates(
        Dictionary<int, ExecutionNode> nodesById,
        Lookup lookup,
        Dictionary<(string Path, string ResponseName), HashSet<int>> writers,
        Dictionary<string, SelectionSet> selectionSets,
        Dictionary<string, List<string>> pathsByType)
    {
        if (lookup.LookupTypeName is null
            || !TryGetReusableFields(
                lookup.ResultSelectionSet,
                lookup.Requirements,
                lookup.ForwardedVariableCount,
                lookup.ConditionCount,
                out var responseNames,
                out var requirementFields)
            || !pathsByType.TryGetValue(lookup.LookupTypeName, out var candidatePaths))
        {
            return null;
        }

        // Apollo lookups match entities by their representation, so the representation must
        // consist of exactly the requirement fields that the producers are checked for.
        if (!lookup.RepresentationShape.IsDefault
            && !IsFlatRepresentationShape(lookup.RepresentationShape, requirementFields))
        {
            return null;
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
        List<SelectionSet>? producerSelectionSets = null;

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
            producerSelectionSets ??= [];
            producerSelectionSets.Add(producerSelectionSet);
        }

        if (producerPaths is null || producerSelectionSets is null)
        {
            return null;
        }

        var layouts = CreateLayouts(
            lookup.Requirements,
            responseNames,
            requirementFields,
            targetSelectionSets,
            producerSelectionSets);

        return new EntityReuseCandidateList([.. producerPaths], responseNames, requirementFields, layouts);
    }

    // Creates the row layouts of the target and producer selection sets, or returns an empty
    // array when the entities cannot be matched on the raw bytes of their key values.
    private static EntityReuseLayout[] CreateLayouts(
        OperationRequirement[] requirements,
        string[] responseNames,
        string[] requirementFields,
        SelectionSet[] targetSelectionSets,
        List<SelectionSet> producerSelectionSets)
    {
        // The raw key comparison mirrors the variable value fast paths, which cover up to three
        // requirements with values that serialize as their raw bytes.
        if (requirements.Length > FetchResultStore.MaxRawKeyRequirements)
        {
            return [];
        }

        foreach (var requirement in requirements)
        {
            if (requirement.Type.IsListType())
            {
                return [];
            }
        }

        var layouts = new List<EntityReuseLayout>();
        var reference = targetSelectionSets[0];

        foreach (var selectionSet in targetSelectionSets.Concat(producerSelectionSets))
        {
            if (layouts.Any(t => t.SelectionSetId == selectionSet.Id))
            {
                continue;
            }

            if (TryCreateLayout(selectionSet, reference, responseNames, requirementFields) is not { } layout)
            {
                return [];
            }

            layouts.Add(layout);
        }

        return [.. layouts];
    }

    private static EntityReuseLayout? TryCreateLayout(
        SelectionSet selectionSet,
        SelectionSet reference,
        string[] responseNames,
        string[] requirementFields)
    {
        var keyRowOffsets = new int[requirementFields.Length];

        for (var i = 0; i < requirementFields.Length; i++)
        {
            if (!selectionSet.TryGetSelection(requirementFields[i], out var selection))
            {
                return null;
            }

            keyRowOffsets[i] = CompositeResultDocument.GetValueRowOffset(selectionSet, selection);
        }

        var fieldRowOffsets = new int[responseNames.Length];

        for (var i = 0; i < responseNames.Length; i++)
        {
            // A leaf value is only assigned between fields of the same enum type.
            if (!selectionSet.TryGetSelection(responseNames[i], out var selection)
                || !reference.TryGetSelection(responseNames[i], out var referenceSelection)
                || selection.IsEnumValue != referenceSelection.IsEnumValue
                || (selection.IsEnumValue && !ReferenceEquals(selection.NamedType, referenceSelection.NamedType)))
            {
                return null;
            }

            fieldRowOffsets[i] = CompositeResultDocument.GetValueRowOffset(selectionSet, selection);
        }

        return new EntityReuseLayout(selectionSet.Id, keyRowOffsets, fieldRowOffsets);
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

    private static bool IsFlatRepresentationShape(
        ImmutableArray<RepresentationShapeNode> shape,
        string[] requirementFields)
    {
        if (shape.Length != requirementFields.Length)
        {
            return false;
        }

        foreach (var node in shape)
        {
            if (!node.Nodes.IsDefault
                || node.IsList
                || !node.LhsPath.IsEmpty
                || node.ParentTypeCondition is not null
                || node.TypeCondition is not null
                || (uint)node.RequirementIndex >= (uint)requirementFields.Length
                || !node.ResponseName.Equals(requirementFields[node.RequirementIndex], StringComparison.Ordinal))
            {
                return false;
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
    // when a dependency is not part of the plan. Optional dependencies complete first as well.
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

            foreach (var next in node.OptionalDependencies)
            {
                stack.Push(next);
            }

            foreach (var operation in GetBatchOperations(node))
            {
                foreach (var next in operation.Dependencies)
                {
                    stack.Push(next);
                }

                foreach (var next in operation.OptionalDependencies)
                {
                    stack.Push(next);
                }
            }
        }

        return visited;
    }

    // Records the writers of an operation definition in a batch and adds it as a lookup.
    private static bool TryAddBatchLookup(
        Dictionary<(string Path, string ResponseName), HashSet<int>> writers,
        List<Lookup> lookups,
        OperationDefinition definition,
        OperationSourceText sourceText,
        string? lookupTypeName,
        ImmutableArray<RepresentationShapeNode> representationShape,
        int nodeId)
    {
        if (GetTargets(definition) is not { } targets)
        {
            return false;
        }

        var targetKeys = new string[targets.Length];

        for (var i = 0; i < targets.Length; i++)
        {
            if (!TryGetPathKey(targets[i], out var key)
                || !TryAddWriters(writers, key, sourceText, definition.Source, nodeId))
            {
                return false;
            }

            targetKeys[i] = key;
        }

        lookups.Add(new Lookup(
            definition.Id,
            [.. definition.Dependencies, .. definition.OptionalDependencies],
            lookupTypeName,
            definition.ResultSelectionSet,
            definition.Requirements.ToArray(),
            definition.ForwardedVariables.Length,
            definition.Conditions.Length,
            targetKeys,
            representationShape));
        return true;
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

    private static ReadOnlySpan<OperationDefinition> GetBatchOperations(ExecutionNode node)
        => node switch
        {
            OperationBatchExecutionNode batch => batch.Operations,
            ApolloOperationBatchExecutionNode apolloBatch => apolloBatch.Operations,
            _ => []
        };

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

    // RepresentationShape is default for a lookup that sends variable value sets, and holds the
    // representation shape of an Apollo Federation entity lookup.
    private readonly record struct Lookup(
        int Id,
        IOperationPlanNode[] Dependencies,
        string? LookupTypeName,
        ResultSelectionSet ResultSelectionSet,
        OperationRequirement[] Requirements,
        int ForwardedVariableCount,
        int ConditionCount,
        string[] TargetKeys,
        ImmutableArray<RepresentationShapeNode> RepresentationShape);
}
