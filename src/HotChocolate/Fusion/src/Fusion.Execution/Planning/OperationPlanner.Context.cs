using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Language;
using HotChocolate.Types;
using Lookup = HotChocolate.Fusion.Types.Metadata.Lookup;
using static HotChocolate.Fusion.Planning.PlannerExtensions;
using ThrowHelper = HotChocolate.Fusion.Execution.ThrowHelper;

namespace HotChocolate.Fusion.Planning;

public sealed partial class OperationPlanner
{
    // Inlines the selections that the context paths of a field read into the steps that select the
    // context roots, and returns the root of each context. The excluded step is the existing step
    // the field is merged into, or null if the field gets a new step.
    private Dictionary<string, RequirementContextRoot> InlineContextRequirements(
        FieldRequirementWorkItem workItem,
        Lookup lookup,
        int dependentStepId,
        int stepDepth,
        OperationPlanStep? excludedStep,
        ref PlanNode current,
        SelectionSetIndexBuilder index,
        ref Backlog backlog,
        ref ImmutableList<PlanStep> steps,
        RequirementAliasContext requirementAliases)
    {
        var contextRequirements =
            workItem.Selection.Field.Sources[current.SchemaName].Requirements!.ContextRequirements;
        var contextRoots = new Dictionary<string, RequirementContextRoot>(StringComparer.Ordinal);

        // The enumeration order of an immutable dictionary is not stable, and the order
        // decides how the planner allocates identifiers.
        foreach (var contextName in contextRequirements.Keys.Order(StringComparer.Ordinal))
        {
            var target = FindContextRoot(workItem.Selection, current, index, contextName);
            var selections = SelectAlternativesOfType(
                _mergeRewriter.Merge([contextRequirements[contextName]], target.Type),
                target.Type);

            contextRoots.Add(contextName, new RequirementContextRoot(contextName, target.Path, []));

            // The context may read nothing from the root type, for example when all of its
            // typed alternatives are on other types.
            if (selections.Selections.Count == 0)
            {
                continue;
            }

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
                requirementAliases);

            if (leftover is not null)
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

        return contextRoots;
    }

    // Finds the nearest ancestor of the selection whose possible object types all set the context
    // in the schema of the plan node. Selection sets of the object of the selection are not ancestors.
    private RequirementTarget FindContextRoot(
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

        if (pathItems is not null)
        {
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
                var setsContext = 0;

                foreach (var possibleType in possibleTypes)
                {
                    if (_schema.SetsContext(possibleType, current.SchemaName, contextName))
                    {
                        setsContext++;
                    }
                }

                if (setsContext > 0)
                {
                    if (setsContext < possibleTypes.Length)
                    {
                        throw ThrowHelper.ContextSetForSomeTypesOnPath(
                            contextName,
                            selection.Field.Coordinate.ToString());
                    }

                    return new RequirementTarget(
                        index.GetId(selectionSet),
                        type,
                        selection.Path.Slice(segmentLength));
                }

                segmentLength--;
            }
        }

        throw ThrowHelper.ContextNotSetOnPath(contextName, selection.Field.Coordinate.ToString());
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

    private static ImmutableArray<RequirementContextRoot> GetRequirementContextRoots(
        IValueSelectionNode fieldSelectionMap,
        Dictionary<string, RequirementContextRoot>? contextRoots)
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

        var builder = ImmutableArray.CreateBuilder<RequirementContextRoot>(names.Count);

        foreach (var name in names)
        {
            builder.Add(contextRoots[name]);
        }

        return builder.MoveToImmutable();
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
}
