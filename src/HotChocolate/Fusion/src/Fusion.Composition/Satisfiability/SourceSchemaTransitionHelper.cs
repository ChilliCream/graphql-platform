using System.Collections.Immutable;
using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// Shared helper for validating that, while traversing the merged schema, we
/// can move from one source schema to another in order to reach a field. The
/// satisfiability and requirements validators both face this question and
/// resolve it the same way: try a direct lookup on the target type, fall back
/// to a parent entity call via an ancestor on the path, re-root a viewer field
/// at <c>Query.viewer</c> when a query viewer field is given, and otherwise check
/// whether the current path can be traversed one-to-one in the target schema.
/// </summary>
internal static class SourceSchemaTransitionHelper
{
    /// <summary>
    /// Validates whether a transition to <paramref name="transitionToSchemaName"/>
    /// is possible for <paramref name="type"/>.
    /// </summary>
    /// <param name="schema">The merged schema.</param>
    /// <param name="lookupCache">The lookup directive cache for the current validation run.</param>
    /// <param name="type">
    /// The type we need to be holding in <paramref name="transitionToSchemaName"/>
    /// after the transition.
    /// </param>
    /// <param name="transitionToSchemaName">The target source schema.</param>
    /// <param name="pathFromLeaf">
    /// The current access path enumerated from the leaf (the current position)
    /// toward the root. Used to detect path-traversal and parent entity call
    /// opportunities when a direct lookup on <paramref name="type"/> is
    /// unavailable or unsatisfiable.
    /// </param>
    /// <param name="validateLookupRequirements">
    /// Callback that validates a candidate lookup's key requirements. It is
    /// invoked with the context type whose lookup is being attempted (either
    /// <paramref name="type"/> for a direct lookup, or an ancestor's type for a
    /// parent entity call) and the path from the path item that anchors the
    /// validation context toward the root, which is empty at a root type.
    /// Caller-owned so each validator can supply its own state (cycle
    /// detection, excluded schema, etc.).
    /// </param>
    /// <param name="noLookupsFoundForTypeMessageFormat">
    /// Format string used when no lookup or path-based transition exists. The
    /// caller provides its own resource key so the error reads naturally in
    /// the caller's context.
    /// </param>
    /// <param name="unableToSatisfyRequirementForLookupMessageFormat">
    /// Format string used when a candidate lookup exists but its key
    /// requirements could not be satisfied. The caller provides its own
    /// resource key.
    /// </param>
    /// <param name="queryViewerField">
    /// The <c>Query.viewer</c> field that a viewer field on the path, which is missing in
    /// <paramref name="transitionToSchemaName"/>, is re-rooted at, or <c>null</c> when the path
    /// cannot be re-rooted.
    /// </param>
    /// <param name="anchor">
    /// The anchor that the last item of <paramref name="pathFromLeaf"/> holds its object at, which the
    /// walk stops at and asks which schemas can hold the object, or <c>null</c> to walk the path up to
    /// the root.
    /// </param>
    /// <param name="unavailableSchemaNames">
    /// The schemas whose data is unavailable, which <paramref name="anchor"/> takes into account.
    /// </param>
    /// <param name="budget">The budget for the explanation entries of the search.</param>
    /// <param name="transitionFails">
    /// Whether the transition is known to fail, so that every lookup it tries is listed even when the
    /// budget is used up. Otherwise, a lookup that the budget cuts stands for one omitted entry.
    /// </param>
    /// <returns>
    /// An empty array if the transition is satisfiable; otherwise the
    /// accumulated errors describing every direct and parent-call lookup that
    /// was tried, plus a "no lookups" error if no lookup was available at all.
    /// </returns>
    public static ImmutableArray<SatisfiabilityError> ValidateSourceSchemaTransition(
        MutableSchemaDefinition schema,
        FusionLookupDirectiveCache lookupCache,
        MutableObjectTypeDefinition type,
        string transitionToSchemaName,
        IReadOnlyList<SatisfiabilityPathItem> pathFromLeaf,
        Func<MutableObjectTypeDefinition,
            IReadOnlyList<SatisfiabilityPathItem>,
            SelectionSetNode,
            ImmutableArray<SatisfiabilityError>> validateLookupRequirements,
        string noLookupsFoundForTypeMessageFormat,
        string unableToSatisfyRequirementForLookupMessageFormat,
        MutableOutputFieldDefinition? queryViewerField,
        PathAnchor? anchor,
        ImmutableHashSet<string> unavailableSchemaNames,
        SatisfiabilityExplanationBudget budget,
        bool transitionFails)
    {
        var errors = new List<SatisfiabilityError>();

        // The anchor answers for its item and the path above it, including the direct lookups of its
        // object.
        var walkedCount = anchor is null ? pathFromLeaf.Count : pathFromLeaf.Count - 1;

        if (anchor is not null && walkedCount == 0)
        {
            return anchor.CanHold(transitionToSchemaName, unavailableSchemaNames)
                ? []
                :
                [
                    budget.CreateError(
                        string.Format(
                            noLookupsFoundForTypeMessageFormat,
                            type.Name,
                            transitionToSchemaName))
                ];
        }

        var lookupDirectives =
            lookupCache.GetPossibleFusionLookupDirectives(type, transitionToSchemaName);

        var directLookupErrors = new List<SatisfiabilityError>();
        var satisfiable = false;

        // Direct lookup on `type` in the target schema.
        foreach (var lookupDirective in lookupDirectives)
        {
            if (TryLookup(lookupDirective, type, parentIndex: 0, directLookupErrors))
            {
                satisfiable = true;
                break;
            }
        }

        if (satisfiable)
        {
            return [];
        }

        errors.AddRange(directLookupErrors);

        // Parent entity call: walk from the leaf upward, find the nearest
        // ancestor on the path whose declaring type has a lookup in the target
        // schema, and try the ancestor's lookups. Accepting one such ancestor
        // is the validator's way of saying the gateway could re-fetch the
        // ancestor in the target schema (via its lookup) and walk the suffix
        // back down to the stuck position; the loop's early break ensures
        // every field on that suffix also exists in the target schema.
        //
        // The loop also tracks whether the entire path's fields exist in the
        // target schema (`pathIsTraversable`); that doubles as the fallback
        // condition below when no lookup-based option succeeded.
        //
        // Known limitation: we do not explicitly replay the suffix fields
        // through the @require rules. The main satisfiability loop covers
        // most cases implicitly by visiting every reachable type on its access
        // path and running those checks there. The gap is when this
        // parent-call enables a (type, schema) combination that the
        // forward-from-Query traversal does not otherwise reach (e.g. a target
        // schema whose only entry point for the ancestor type is the @lookup
        // itself, which is @inaccessible and skipped by the main loop).
        var pathIsTraversable = true;
        var hasPartialField = false;

        for (var i = 0; i < walkedCount; i++)
        {
            var ancestor = pathFromLeaf[i];
            var existsInSchema = ancestor.Field.ExistsInSchema(transitionToSchemaName);
            var isPartial = existsInSchema && ancestor.Field.IsPartial(transitionToSchemaName);

            // The suffix below a viewer field that the target schema does not define is
            // re-traversed from Query.viewer in the target schema.
            if (!existsInSchema
                && queryViewerField is not null
                && ancestor.Field.IsViewerFieldFor(queryViewerField)
                && queryViewerField.ExistsInSchema(transitionToSchemaName)
                && (!hasPartialField
                    || ServesSuffix(i - 1, GetProvidedSelectionSet(queryViewerField, transitionToSchemaName))))
            {
                return [];
            }

            // The ancestor's field must also exist in the target schema so the
            // suffix can be re-traversed. If it does not, deeper ancestors are
            // also blocked by the same missing field and we can stop here.
            if (!existsInSchema)
            {
                pathIsTraversable = false;
                break;
            }

            // A partial (@external) field is only served when the field above it
            // provides it, so the ancestor cannot be re-fetched through its lookup.
            if (isPartial)
            {
                hasPartialField = true;
                continue;
            }

            // Path items are always constructed with object declaring types,
            // but the record property is typed as the broader
            // MutableComplexTypeDefinition; guard for that.
            if (ancestor.Type is not MutableObjectTypeDefinition ancestorObjectType)
            {
                continue;
            }

            var ancestorLookups =
                lookupCache.GetPossibleFusionLookupDirectives(ancestorObjectType, transitionToSchemaName);

            // A partial field below the ancestor must be provided on the suffix
            // that the ancestor's lookup starts.
            if (ancestorLookups.Count == 0
                || (hasPartialField && !ServesSuffix(i, parentProvides: null)))
            {
                continue;
            }

            // The path above the ancestor starts at the path item we held right
            // before the ancestor was accessed (the position at which we were
            // holding ancestor.Type and from which the ancestor's lookup must be
            // satisfiable).
            foreach (var ancestorLookup in ancestorLookups)
            {
                if (TryLookup(ancestorLookup, ancestorObjectType, parentIndex: i + 1, errors))
                {
                    return [];
                }
            }
        }

        if (anchor is not null)
        {
            // Anchor fallback: the target schema holds the object of the anchor item and serves
            // every field on the path below it.
            if (pathIsTraversable
                && anchor.CanHold(transitionToSchemaName, unavailableSchemaNames)
                && (!hasPartialField
                    || ServesSuffix(
                        walkedCount - 1,
                        anchor.GetProvidedSelectionSet(transitionToSchemaName, unavailableSchemaNames))))
            {
                return [];
            }
        }
        else if (pathIsTraversable
            && (!hasPartialField || ServesSuffix(pathFromLeaf.Count - 1, parentProvides: null)))
        {
            // One-to-one path fallback: every field on the path is served by the
            // target schema, so the same traversal applies there without any
            // entity call.
            return [];
        }

        // Nothing worked. If no lookup was even tried (no direct lookup on the
        // type, and no ancestor on the path had a lookup either) the
        // accumulated error list is still empty, so emit the canonical
        // "no lookups" error.
        if (errors.Count == 0)
        {
            errors.Add(
                budget.CreateError(
                    string.Format(
                        noLookupsFoundForTypeMessageFormat,
                        type.Name,
                        transitionToSchemaName)));
        }

        return [.. errors];

        // Determines whether the target schema serves the path items from startIndex down to the
        // leaf when it re-traverses them below an object for which it provides parentProvides.
        bool ServesSuffix(int startIndex, SelectionSetNode? parentProvides)
        {
            for (var j = startIndex; j >= 0; j--)
            {
                var item = pathFromLeaf[j];

                if (!ServesOnRetraversal(
                    schema,
                    item.Field,
                    item.Type,
                    transitionToSchemaName,
                    parentProvides,
                    GetProvidedSelectionSet(item.Field, transitionToSchemaName),
                    out parentProvides))
                {
                    return false;
                }
            }

            return true;
        }

        bool TryLookup(
            IDirective lookupDirective,
            MutableObjectTypeDefinition contextType,
            int parentIndex,
            List<SatisfiabilityError> sink)
        {
            var lookupKeyArg = (string)lookupDirective.Arguments["key"].Value!;
            var lookupFieldArg = (string)lookupDirective.Arguments["field"].Value!;
            var lookupPathArg = (string?)lookupDirective.Arguments["path"].Value;

            var lookupRequirements = ParseSelectionSet($"{{ {lookupKeyArg} }}");
            var lookupFieldName = ParseFieldDefinition(lookupFieldArg).Name.Value;
            ImmutableArray<SatisfiabilityError> requirementErrors = [];

            if (!budget.IsExhausted)
            {
                IReadOnlyList<SatisfiabilityPathItem> pathFromParent =
                    parentIndex < pathFromLeaf.Count ? [.. pathFromLeaf.Skip(parentIndex)] : [];

                requirementErrors =
                    validateLookupRequirements(contextType, pathFromParent, lookupRequirements);

                if (requirementErrors.IsEmpty && !budget.IsExhausted)
                {
                    return true;
                }
            }

            // Once the budget is used up, the search of the lookup key does not decide whether the
            // lookup works, which is only known when the transition fails.
            if (budget.IsExhausted && !transitionFails)
            {
                sink.Add(SatisfiabilityError.Unexplained);
                return false;
            }

            var lookupName = lookupPathArg is null
                ? lookupFieldName
                : $"{lookupPathArg}.{lookupFieldName}";
            var message =
                string.Format(
                    unableToSatisfyRequirementForLookupMessageFormat,
                    lookupRequirements.ToString(indented: false),
                    lookupName,
                    transitionToSchemaName);

            sink.Add(
                transitionFails
                    ? budget.CreateKnownError(message, requirementErrors)
                    : budget.CreateError(message, requirementErrors));

            return false;
        }
    }

    /// <summary>
    /// Determines whether <paramref name="schemaName"/> serves <paramref name="field"/> of
    /// <paramref name="declaringType"/> when it re-traverses the path below an object of
    /// <paramref name="declaringType"/> for which it provides <paramref name="parentProvides"/>.
    /// A partial (@external) field is only served when <paramref name="parentProvides"/> selects it.
    /// </summary>
    /// <param name="schema">The merged schema.</param>
    /// <param name="field">The re-traversed field.</param>
    /// <param name="declaringType">The type declaring <paramref name="field"/>.</param>
    /// <param name="schemaName">The schema re-traversing the path.</param>
    /// <param name="parentProvides">
    /// The selection set the schema provides for the object declaring <paramref name="field"/>, or
    /// <c>null</c> if it provides none.
    /// </param>
    /// <param name="fieldProvides">
    /// The @provides selection set of <paramref name="field"/> in the schema, or <c>null</c> if it has none.
    /// </param>
    /// <param name="providedSelectionSet">
    /// The selection set the schema provides below <paramref name="field"/>, or <c>null</c> if it provides none.
    /// </param>
    /// <returns><c>true</c> if the schema serves the field; otherwise, <c>false</c>.</returns>
    public static bool ServesOnRetraversal(
        MutableSchemaDefinition schema,
        MutableOutputFieldDefinition field,
        MutableComplexTypeDefinition declaringType,
        string schemaName,
        SelectionSetNode? parentProvides,
        SelectionSetNode? fieldProvides,
        out SelectionSetNode? providedSelectionSet)
    {
        providedSelectionSet = null;

        if (!field.ExistsInSchema(schemaName))
        {
            return false;
        }

        SelectionSetNode? selectedByParent = null;
        var isProvided =
            parentProvides is not null
            && SatisfiabilityPathItemExtensions.TryGetFieldSelectionSet(
                schema,
                parentProvides,
                declaringType,
                field,
                declaringType,
                out selectedByParent);

        if (!isProvided && field.IsPartial(schemaName))
        {
            return false;
        }

        providedSelectionSet = CombineSelectionSets(selectedByParent, fieldProvides);
        return true;
    }

    /// <summary>
    /// Gets the @provides selection set of <paramref name="field"/> in <paramref name="schemaName"/>,
    /// or <c>null</c> if it has none.
    /// </summary>
    public static SelectionSetNode? GetProvidedSelectionSet(
        MutableOutputFieldDefinition field,
        string schemaName)
        => field.GetFusionFieldProvides(schemaName) is { } provides
            ? ParseSelectionSet($"{{ {provides} }}")
            : null;

    /// <summary>
    /// Gets a selection set with the selections of both selection sets, either of which may be <c>null</c>.
    /// </summary>
    public static SelectionSetNode? CombineSelectionSets(SelectionSetNode? left, SelectionSetNode? right)
    {
        if (left is null)
        {
            return right;
        }

        if (right is null)
        {
            return left;
        }

        return new SelectionSetNode([.. left.Selections, .. right.Selections]);
    }
}
