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
/// to a parent entity call via an ancestor on the path, and otherwise check
/// whether the current path can be traversed one-to-one in the target schema.
/// </summary>
internal static class SourceSchemaTransitionHelper
{
    /// <summary>
    /// Validates whether a transition to <paramref name="transitionToSchemaName"/>
    /// is possible for <paramref name="type"/>.
    /// </summary>
    /// <param name="schema">The merged schema being validated.</param>
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
    /// parent entity call) and the path item that anchors the validation
    /// context. Caller-owned so each validator can supply its own state
    /// (cycle detection, excluded schema, etc.).
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
    /// <returns>
    /// An empty array if the transition is satisfiable; otherwise the
    /// accumulated errors describing every direct and parent-call lookup that
    /// was tried, plus a "no lookups" error if no lookup was available at all.
    /// </returns>
    public static ImmutableArray<SatisfiabilityError> ValidateSourceSchemaTransition(
        MutableSchemaDefinition schema,
        MutableObjectTypeDefinition type,
        string transitionToSchemaName,
        IReadOnlyList<SatisfiabilityPathItem> pathFromLeaf,
        Func<MutableObjectTypeDefinition,
            SatisfiabilityPathItem?,
            SelectionSetNode,
            ImmutableArray<SatisfiabilityError>> validateLookupRequirements,
        string noLookupsFoundForTypeMessageFormat,
        string unableToSatisfyRequirementForLookupMessageFormat)
    {
        var errors = new List<SatisfiabilityError>();
        var leafPathItem = pathFromLeaf.Count > 0 ? pathFromLeaf[0] : null;

        var lookupDirectives =
            schema.GetPossibleFusionLookupDirectives(type, transitionToSchemaName);

        // Direct lookup on `type` in the target schema.
        foreach (var lookupDirective in lookupDirectives)
        {
            if (TryLookup(lookupDirective, type, leafPathItem))
            {
                return [];
            }
        }

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
        // through the normal field-access rules (@require, @partial,
        // @provides). The main satisfiability loop covers most cases
        // implicitly by visiting every reachable (type, schema) combination
        // and running those checks there. The gap is when this parent-call
        // enables a (type, schema) combination that the forward-from-Query
        // traversal does not otherwise reach (e.g. a target schema whose only
        // entry point for the ancestor type is the @lookup itself, which is
        // @inaccessible and skipped by the main loop).
        var pathIsTraversable = true;

        for (var i = 0; i < pathFromLeaf.Count; i++)
        {
            var ancestor = pathFromLeaf[i];

            // The ancestor's field must also exist in the target schema so the
            // suffix can be re-traversed. If it does not, deeper ancestors are
            // also blocked by the same missing field and we can stop here.
            if (!ancestor.Field.ExistsInSchema(transitionToSchemaName))
            {
                pathIsTraversable = false;
                break;
            }

            // Path items are always constructed with object declaring types,
            // but the record property is typed as the broader
            // MutableComplexTypeDefinition; guard for that.
            if (ancestor.Type is not MutableObjectTypeDefinition ancestorObjectType)
            {
                continue;
            }

            var ancestorLookups =
                schema.GetPossibleFusionLookupDirectives(ancestorObjectType, transitionToSchemaName);

            if (ancestorLookups.Count == 0)
            {
                continue;
            }

            // The "prefix leaf" is the path item we held right before the
            // ancestor was accessed (the position at which we were holding
            // ancestor.Type and from which the ancestor's lookup must be
            // satisfiable).
            var prefixLeaf = i + 1 < pathFromLeaf.Count ? pathFromLeaf[i + 1] : null;

            foreach (var ancestorLookup in ancestorLookups)
            {
                if (TryLookup(ancestorLookup, ancestorObjectType, prefixLeaf))
                {
                    return [];
                }
            }
        }

        // One-to-one path fallback: every field on the path exists in the
        // target schema, so the same traversal applies there without any
        // entity call.
        if (pathIsTraversable)
        {
            return [];
        }

        // Nothing worked. If no lookup was even tried (no direct lookup on the
        // type, and no ancestor on the path had a lookup either) the
        // accumulated error list is still empty, so emit the canonical
        // "no lookups" error.
        if (errors.Count == 0)
        {
            errors.Add(
                new SatisfiabilityError(
                    string.Format(
                        noLookupsFoundForTypeMessageFormat,
                        type.Name,
                        transitionToSchemaName)));
        }

        return [.. errors];

        bool TryLookup(
            IDirective lookupDirective,
            MutableObjectTypeDefinition contextType,
            SatisfiabilityPathItem? parentPathItem)
        {
            var lookupKeyArg = (string)lookupDirective.Arguments["key"].Value!;
            var lookupFieldArg = (string)lookupDirective.Arguments["field"].Value!;
            var lookupPathArg = (string?)lookupDirective.Arguments["path"].Value;

            var lookupRequirements = ParseSelectionSet($"{{ {lookupKeyArg} }}");
            var lookupFieldName = ParseFieldDefinition(lookupFieldArg).Name.Value;

            var requirementErrors =
                validateLookupRequirements(contextType, parentPathItem, lookupRequirements);

            if (requirementErrors.IsEmpty)
            {
                return true;
            }

            var lookupName = lookupPathArg is null
                ? lookupFieldName
                : $"{lookupPathArg}.{lookupFieldName}";

            errors.Add(
                new SatisfiabilityError(
                    string.Format(
                        unableToSatisfyRequirementForLookupMessageFormat,
                        lookupRequirements.ToString(indented: false),
                        lookupName,
                        transitionToSchemaName),
                    requirementErrors));

            return false;
        }
    }
}
