using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Converters;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Types.Rewriters;
using HotChocolate.Language;
using HotChocolate.Types;
using FusionNameNode = HotChocolate.Fusion.Language.NameNode;
using Lookup = HotChocolate.Fusion.Types.Metadata.Lookup;
using static HotChocolate.Fusion.Planning.PlannerExtensions;

namespace HotChocolate.Fusion.Planning;

public sealed partial class OperationPlanner
{
    // Inlines the selections that the context paths of a field read into the steps that select the
    // context roots, and returns the roots of each context nearest first. The excluded step is the
    // existing step the field is merged into, or null if the field gets a new step. The aliases
    // minted for each context are added to contextAliases, and the alias registry of each context is
    // adopted. Returns null if the path of the field has no ancestor that sets one of the contexts.
    // A context whose root lies above the anchor of the planned deferred fragment is not inlined,
    // only its root is returned.
    private Dictionary<string, List<RequirementContextRoot>>? InlineContextRequirements(
        FieldRequirementWorkItem workItem,
        Lookup lookup,
        int dependentStepId,
        int stepDepth,
        OperationPlanStep? excludedStep,
        ref PlanNode current,
        SelectionSetIndexBuilder index,
        ref Backlog backlog,
        ref ImmutableList<PlanStep> steps,
        RequirementAliasContext requirementAliases,
        Dictionary<string, List<(RequirementFieldSelectionKey Key, string Alias)>> contextAliases)
    {
        var contextRequirements =
            workItem.Selection.Field.Sources[current.SchemaName].Requirements!.ContextRequirements;
        var contextRoots = new Dictionary<string, List<RequirementContextRoot>>(StringComparer.Ordinal);
        var deferAnchorPath = workItem.Selection.Path.Equals(current.DeferAnchorPath)
            ? current.DeferAnchorPath
            : null;

        // The enumeration order of an immutable dictionary is not stable, and the order
        // decides how the planner allocates identifiers.
        foreach (var contextName in contextRequirements.Keys.Order(StringComparer.Ordinal))
        {
            if (FindContextRoots(workItem.Selection, current, index, contextName) is not { } candidates)
            {
                return null;
            }

            if (deferAnchorPath is not null && IsImportedContextRoot(candidates, deferAnchorPath, out var importedRoot))
            {
                // The incremental plan reads the context from the plan that encloses it.
                contextRoots.Add(contextName, [new RequirementContextRoot(contextName, importedRoot, [])]);
                continue;
            }

            var roots = new List<RequirementContextRoot>(candidates.Count);
            var contextAliasContext = new RequirementAliasContext(
                [],
                requirementAliases.Registry,
                aliasTypeConditions: true);

            contextRoots.Add(contextName, roots);

            // Selects the context fields of each root and settles the aliases of all roots.
            var candidateSelections = new SelectionSetNode[candidates.Count];

            for (var i = 0; i < candidates.Count; i++)
            {
                var (target, typeNames) = candidates[i];
                candidateSelections[i] = CreateContextSelections(contextRequirements[contextName], target, typeNames);

                MintSiblingCollisionAliases(
                    candidateSelections[i],
                    target.SelectionSetId,
                    index,
                    contextAliasContext);
            }

            for (var i = 0; i < candidates.Count; i++)
            {
                var (target, typeNames) = candidates[i];
                var selections = candidateSelections[i];

                roots.Add(new RequirementContextRoot(contextName, target.Path, typeNames));

                // The context may read nothing from the root type, for example when all of its
                // typed alternatives are on other types.
                if (selections.Selections.Count == 0)
                {
                    continue;
                }

                if (target.Path.IsRoot)
                {
                    steps = BindRootStepsToOperationRoot(steps, index, target.SelectionSetId);
                }

                // The candidate steps are read from the plan node, so it has to see the selections
                // that the previous candidates inlined.
                current = current with { Steps = steps };

                var leftover = TryInlineRequirements(
                    selections,
                    target,
                    workItem.SourceSchemaNodePolicy,
                    dependentStepId,
                    ref current,
                    excludedStep,
                    allowRequiringSchemaSteps: true,
                    index,
                    ref backlog,
                    ref steps,
                    contextAliasContext);

                if (leftover is not null && target.Path.IsRoot)
                {
                    // No lookup enters the operation root, so the selections that no root step took
                    // become a new root step of a schema that serves them.
                    index.Register(target.SelectionSetId, leftover);

                    backlog = backlog.Push(
                        new OperationWorkItem(
                            OperationWorkItemKind.Root,
                            new SelectionSet(
                                target.SelectionSetId,
                                leftover,
                                target.Type,
                                target.Path))
                        {
                            Dependents = ImmutableHashSet<int>.Empty.Add(dependentStepId),
                            ParentDepth = stepDepth,
                            Conditions = workItem.Conditions,
                            SourceSchemaNodePolicy = workItem.SourceSchemaNodePolicy
                        });
                }
                else if (leftover is not null)
                {
                    backlog = PushRequirementLeftover(
                        backlog,
                        index,
                        leftover,
                        target.SelectionSetId,
                        target.Type,
                        target.Path,
                        lookup.SchemaName,
                        ImmutableHashSet<int>.Empty.Add(dependentStepId),
                        stepDepth,
                        workItem.Conditions,
                        workItem.SourceSchemaNodePolicy);
                }
            }

            requirementAliases.Adopt(contextAliasContext.Registry);

            if (contextAliasContext.Aliases.Count > 0)
            {
                contextAliases.Add(contextName, [.. contextAliasContext.Aliases]);
            }
        }

        return contextRoots;
    }

    // A context is imported when its single complete root lies above the anchor of the deferred
    // fragment and the field that reads it is a direct child of that anchor.
    private static bool IsImportedContextRoot(
        List<ContextRootCandidate> candidates,
        SelectionPath deferAnchorPath,
        out SelectionPath rootPath)
    {
        if (candidates is [{ TypeNames.IsEmpty: true } candidate]
            && IsAboveDeferAnchor(candidate.Target.Path, deferAnchorPath))
        {
            rootPath = candidate.Target.Path;
            return true;
        }

        rootPath = SelectionPath.Root;
        return false;
    }

    private static bool IsAboveDeferAnchor(SelectionPath rootPath, SelectionPath deferAnchorPath)
        => rootPath.IsParentOfOrSame(deferAnchorPath) && !rootPath.Equals(deferAnchorPath);

    // Creates the selections a context reads from a candidate root. A candidate restricted to
    // types reads the selections of each type in a type condition.
    private SelectionSetNode CreateContextSelections(
        SelectionSetNode contextRequirement,
        RequirementTarget target,
        ImmutableArray<string> typeNames)
    {
        if (typeNames.IsEmpty)
        {
            return SelectAlternativesOfType(
                _mergeRewriter.Merge([contextRequirement], target.Type),
                target.Type);
        }

        var selections = new List<ISelectionNode>(typeNames.Length);

        foreach (var typeName in typeNames)
        {
            var type = _schema.Types.GetType(typeName, allowInaccessibleFields: true);
            var typeSelections = SelectAlternativesOfType(
                _mergeRewriter.Merge([contextRequirement], type),
                type);

            if (typeSelections.Selections.Count > 0)
            {
                selections.Add(
                    new InlineFragmentNode(null, new NamedTypeNode(typeName), [], typeSelections));
            }
        }

        return new SelectionSetNode(selections);
    }

    // Finds the ancestors of the selection that set the context for at least one of their possible
    // object types in the schema of the plan node, nearest first, or null if there is none. The
    // walk ends at the first ancestor whose possible object types all set the context, and that
    // candidate has no type names. Selection sets of the object of the selection are not ancestors.
    // The root of a query operation is the last ancestor.
    private List<ContextRootCandidate>? FindContextRoots(
        FieldSelection selection,
        PlanNode current,
        SelectionSetIndexBuilder index,
        string contextName)
    {
        var pathItems = ReverseSelectionPath(
            current.InternalOperationDefinition,
            selection.Path,
            _schema,
            index,
            selection.SelectionSetId);

        if (pathItems is null)
        {
            return null;
        }

        List<ContextRootCandidate>? candidates = null;
        var segmentLength = selection.Path.Length;
        var isOwnObject = true;

        while (pathItems.TryPop(out var item))
        {
            if (item is InlineFragmentPathItem { TypeCondition: null })
            {
                continue;
            }

            if (isOwnObject)
            {
                // The selection sets up to the field that selects the object of the selection
                // describe that object itself.
                isOwnObject = item is not FieldPathItem;
                segmentLength--;
                continue;
            }

            var (selectionSet, type) = item switch
            {
                FieldPathItem f => (f.Node.SelectionSet!, f.Field.Type.NamedType<IOutputTypeDefinition>()),
                InlineFragmentPathItem f => (f.Node.SelectionSet, f.TypeCondition!),
                _ => throw new InvalidOperationException(
                    "Expected the path item to either be a field or an inline fragment.")
            };

            var possibleTypes = _schema.GetPossibleTypes(type, includeInaccessible: true);
            var settingTypes = ImmutableArray.CreateBuilder<string>();

            foreach (var possibleType in possibleTypes)
            {
                if (_schema.SetsContext(possibleType, current.SchemaName, contextName))
                {
                    settingTypes.Add(possibleType.Name);
                }
            }

            if (settingTypes.Count > 0)
            {
                var isComplete = settingTypes.Count == possibleTypes.Length;
                var target = new RequirementTarget(
                    index.GetId(selectionSet),
                    type,
                    selection.Path.Slice(segmentLength));

                candidates ??= [];
                candidates.Add(new ContextRootCandidate(target, isComplete ? [] : settingTypes.ToImmutable()));

                if (isComplete)
                {
                    return candidates;
                }
            }

            segmentLength--;
        }

        // Only a query operation can use its root as the context root.
        var operationDefinition = current.InternalOperationDefinition;

        if (operationDefinition.Operation == OperationType.Query
            && !selection.Path.IsRoot
            && _schema.SetsContext(_schema.QueryType, current.SchemaName, contextName))
        {
            candidates ??= [];
            candidates.Add(
                new ContextRootCandidate(
                    new RequirementTarget(
                        index.GetId(operationDefinition.SelectionSet),
                        _schema.QueryType,
                        SelectionPath.Root),
                    []));
        }

        return candidates;
    }

    // Gives the query root steps the selection set id of the operation root, so that the steps
    // are found as candidates for selections inlined into the root.
    private static ImmutableList<PlanStep> BindRootStepsToOperationRoot(
        ImmutableList<PlanStep> steps,
        SelectionSetIndexBuilder index,
        uint rootSelectionSetId)
    {
        for (var i = 0; i < steps.Count; i++)
        {
            if (steps[i] is OperationPlanStep step
                && step.Lookup is null
                && step.Target.IsRoot
                && step.Definition.Operation == OperationType.Query
                && step.RootSelectionSetId != rootSelectionSetId)
            {
                index.Register(rootSelectionSetId, step.Definition.SelectionSet);

                steps = steps.SetItem(
                    i,
                    step with
                    {
                        RootSelectionSetId = rootSelectionSetId,
                        SelectionSets = SelectionSetIndexer.CreateIdSet(step.Definition.SelectionSet, index)
                    });
            }
        }

        return steps;
    }

    // Drops the typed alternatives of a context path that no possible type of the root can take.
    private SelectionSetNode SelectAlternativesOfType(
        SelectionSetNode selectionSet,
        ITypeDefinition rootType)
    {
        List<ISelectionNode>? selections = null;
        var rootTypes = _schema.GetPossibleTypes(rootType, includeInaccessible: true);

        for (var i = 0; i < selectionSet.Selections.Count; i++)
        {
            var selection = selectionSet.Selections[i];

            if (selection is InlineFragmentNode { TypeCondition: { } typeCondition }
                && !_schema.GetPossibleTypes(
                    _schema.Types.GetType(typeCondition.Name.Value, allowInaccessibleFields: true),
                    includeInaccessible: true).Any(rootTypes.Contains))
            {
                selections ??= [.. selectionSet.Selections.Take(i)];
                continue;
            }

            selections?.Add(selection);
        }

        return selections is null ? selectionSet : new SelectionSetNode(selections);
    }

    // Replaces the first segment of every context path whose field was aliased in the step of the
    // context root with a segment that names the alias. Entity paths are not changed.
    private static IValueSelectionNode RewriteContextAliases(
        IValueSelectionNode node,
        Dictionary<string, List<(RequirementFieldSelectionKey Key, string Alias)>> contextAliases)
    {
        switch (node)
        {
            case PathNode path:
                return RewriteContextPath(path, contextAliases);

            case PathObjectValueSelectionNode pathObject:
            {
                var rewritten = RewriteContextPath(pathObject.Path, contextAliases);

                return ReferenceEquals(rewritten, pathObject.Path)
                    ? pathObject
                    : new PathObjectValueSelectionNode(
                        pathObject.Location,
                        rewritten,
                        pathObject.ObjectValueSelection);
            }

            case PathListValueSelectionNode pathList:
            {
                var rewritten = RewriteContextPath(pathList.Path, contextAliases);

                return ReferenceEquals(rewritten, pathList.Path)
                    ? pathList
                    : new PathListValueSelectionNode(
                        pathList.Location,
                        rewritten,
                        pathList.ListValueSelection);
            }

            case ChoiceValueSelectionNode choice:
                return new ChoiceValueSelectionNode(
                    choice.Location,
                    [.. choice.Branches.Select(b => RewriteContextAliases(b, contextAliases))]);

            case ObjectValueSelectionNode objectValue:
                return new ObjectValueSelectionNode(
                    objectValue.Location,
                    [.. objectValue.Fields.Select(f => f.ValueSelection is null
                        ? f
                        : new ObjectFieldSelectionNode(
                            f.Location,
                            f.Name,
                            f.Arguments,
                            RewriteContextAliases(f.ValueSelection, contextAliases)))]);

            default:
                return node;
        }
    }

    private static PathNode RewriteContextPath(
        PathNode path,
        Dictionary<string, List<(RequirementFieldSelectionKey Key, string Alias)>> contextAliases)
    {
        if (path.ContextName is not { } contextName
            || !contextAliases.TryGetValue(contextName.Value, out var aliases))
        {
            return path;
        }

        var segment = path.PathSegment;
        var key = new RequirementFieldSelectionKey(
            segment.FieldName.Value,
            FieldSelectionMapValueNodeConverter.Convert(segment.Arguments));

        foreach (var (aliasedKey, alias) in aliases)
        {
            if (aliasedKey.Equals(key))
            {
                return new PathNode(
                    path.Location,
                    contextName,
                    new PathSegmentNode(
                        segment.Location,
                        new FusionNameNode(alias),
                        [],
                        segment.TypeName,
                        segment.PathSegment),
                    path.TypeName);
            }
        }

        return path;
    }

    private static ImmutableArray<RequirementContextRoot> GetRequirementContextRoots(
        IValueSelectionNode fieldSelectionMap,
        Dictionary<string, List<RequirementContextRoot>>? contextRoots)
    {
        if (contextRoots is not { Count: > 0 })
        {
            return [];
        }

        var names = new List<string>();
        CollectContextNames(fieldSelectionMap, names);

        if (names.Count == 0)
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<RequirementContextRoot>();

        foreach (var name in names)
        {
            builder.AddRange(contextRoots[name]);
        }

        return builder.ToImmutable();
    }

    private static void CollectContextNames(IFieldSelectionMapSyntaxNode node, List<string> names)
    {
        if (node is PathNode { ContextName: { } contextName }
            && !names.Contains(contextName.Value, StringComparer.Ordinal))
        {
            names.Add(contextName.Value);
        }

        foreach (var child in node.GetNodes())
        {
            CollectContextNames(child, names);
        }
    }

    // Gets the roots of the requirement that the incremental plan of the descriptor imports from
    // the enclosing scope. They are the single complete roots above the anchor of a requirement
    // that sits at the anchor.
    private static List<RequirementContextRoot> GetImportedContextRoots(
        IncrementalPlanDescriptor descriptor,
        OperationRequirement requirement)
    {
        var imported = new List<RequirementContextRoot>();

        if (descriptor.Operation.Operation != OperationType.Query
            || requirement.ContextRoots.IsDefaultOrEmpty
            || !requirement.Path.Equals(descriptor.Path))
        {
            return imported;
        }

        foreach (var contextRoot in requirement.ContextRoots)
        {
            if (contextRoot.TypeNames.IsEmpty
                && IsAboveDeferAnchor(contextRoot.Path, descriptor.Path)
                && requirement.ContextRoots.Count(r => r.Name.Equals(contextRoot.Name, StringComparison.Ordinal)) == 1)
            {
                imported.Add(contextRoot);
            }
        }

        return imported;
    }

    // Inlines the selections that the requirements of the incremental plan read from imported
    // contexts into the steps of the enclosing scopes that select the context roots, and records
    // those steps as the parent steps of the requirements. Returns whether a requirement imports a
    // context.
    private bool LiftParentContextRoots(
        IncrementalPlanDescriptor descriptor,
        ImmutableList<PlanStep> incrementalPlanSteps,
        ParentPlanContext parentContext,
        PlanContextGraph contextGraph,
        Func<ParentPlanContext, ScopeState> scopeStateFor,
        List<LiftedDeferRequirement> lifted)
    {
        var importsContext = false;

        foreach (var step in incrementalPlanSteps)
        {
            if (step is not OperationPlanStep consumer)
            {
                continue;
            }

            foreach (var (_, requirement) in consumer.Requirements)
            {
                var importedRoots = GetImportedContextRoots(descriptor, requirement);

                if (importedRoots.Count == 0)
                {
                    continue;
                }

                // A requirement takes its value from a single scope.
                if (importedRoots.Count != requirement.ContextRoots.Length)
                {
                    throw CreateUnsatisfiableDeferRequirementException(
                        consumer,
                        requirement,
                        "the requirement reads contexts from the enclosing scope and from the incremental plan");
                }

                var contextSelections =
                    ValueSelectionToSelectionSetRewriter.RewriteContextPaths([requirement.Map]);

                foreach (var importedRoot in importedRoots)
                {
                    var parentStepId =
                        contextSelections.TryGetValue(importedRoot.Name, out var selections)
                            ? TryInlineContextRootInEnclosingScopes(
                                importedRoot.Path,
                                selections,
                                consumer.SchemaName,
                                parentContext,
                                contextGraph,
                                scopeStateFor)
                            : null;

                    if (parentStepId is not { } resolvedParentStepId)
                    {
                        throw CreateUnsatisfiableDeferRequirementException(
                            consumer,
                            requirement,
                            $"no parent-reachable subgraph provides the context '{importedRoot.Name}'");
                    }

                    lifted.Add(new LiftedDeferRequirement(requirement, consumer.Id, resolvedParentStepId));
                    importsContext = true;
                }
            }
        }

        return importsContext;
    }

    private int? TryInlineContextRootInEnclosingScopes(
        SelectionPath rootPath,
        SelectionSetNode selections,
        string? preferredSchemaName,
        ParentPlanContext parentContext,
        PlanContextGraph contextGraph,
        Func<ParentPlanContext, ScopeState> scopeStateFor)
    {
        for (var scope = parentContext; scope is not null; scope = contextGraph.GetEnclosingScope(scope))
        {
            var scopeState = scopeStateFor(scope);
            var schemaNames = scopeState.Steps
                .OfType<OperationPlanStep>()
                .Select(step => step.SchemaName)
                .Where(schemaName => schemaName is not null)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(schemaName => !string.Equals(schemaName, preferredSchemaName, StringComparison.Ordinal))
                .ToList();

            foreach (var schemaName in schemaNames)
            {
                if (TryInlineDeferRequirementInScope(
                    rootPath,
                    _ => selections,
                    schemaName!,
                    scopeState,
                    out var parentStepId,
                    out _))
                {
                    return parentStepId;
                }
            }
        }

        return null;
    }

    // Whether the value selection reads data of the entity that declares the field, as opposed to
    // data of a context.
    private static bool ReadsEntityData(IValueSelectionNode node)
        => node switch
        {
            PathNode path => path.ContextName is null,
            PathObjectValueSelectionNode pathObject => pathObject.Path.ContextName is null,
            PathListValueSelectionNode pathList => pathList.Path.ContextName is null,
            ChoiceValueSelectionNode choice => choice.Branches.Any(ReadsEntityData),
            ObjectValueSelectionNode objectValue => objectValue.Fields.Any(
                f => f.ValueSelection is null || ReadsEntityData(f.ValueSelection)),
            ListValueSelectionNode list => ReadsEntityData(list.ElementSelection),
            _ => true
        };

    private readonly record struct ContextRootCandidate(
        RequirementTarget Target,
        ImmutableArray<string> TypeNames);
}
