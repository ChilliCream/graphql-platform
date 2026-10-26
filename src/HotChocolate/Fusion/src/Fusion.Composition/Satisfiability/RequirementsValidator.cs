using System.Collections.Immutable;
using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.Satisfiability;

internal sealed class RequirementsValidator(
    MutableSchemaDefinition schema,
    FusionLookupDirectiveCache lookupCache,
    SatisfiabilityFacts facts,
    MutableOutputFieldDefinition? queryViewerField,
    bool includeSatisfiabilityPaths)
{
    private readonly FusionLookupDirectiveCache _lookupCache = lookupCache;
    private readonly SatisfiabilityFacts _facts = facts;

    /// <summary>
    /// Gets the number of requirement cycles that the walks found.
    /// </summary>
    public int CycleCount { get; private set; }

    /// <summary>
    /// Validates that <paramref name="requirements"/> can be resolved for
    /// <paramref name="contextType"/> at the position described by <paramref name="parentPathItem"/>.
    /// </summary>
    /// <param name="requirements">The requirement selection set.</param>
    /// <param name="contextType">The object type the requirements are selected on.</param>
    /// <param name="parentPathItem">The path item holding the type, or null at a root type.</param>
    /// <param name="ancestors">
    /// The path items above <paramref name="parentPathItem"/>, from the nearest to the root. With an
    /// anchor in <paramref name="scope"/>, they end at the item of the anchor.
    /// </param>
    /// <param name="scope">The part of the access path that the walk reads above its first item.</param>
    /// <param name="excludeSchemaName">
    /// The schema that must not resolve the requirement, or null to allow every schema.
    /// </param>
    /// <param name="budget">The budget for the explanation entries of the search.</param>
    /// <param name="allowNestedFieldsFromExcludedSchema">
    /// Whether the excluded schema may resolve the fields below the first level of the requirement.
    /// </param>
    /// <param name="cycleDetectionPath">The shared cycle detection path, or null to start a new one.</param>
    /// <param name="unavailableSchemaNames">
    /// Schemas whose data is unavailable at the position for any purpose, including lookup keys
    /// and nested requirements.
    /// </param>
    /// <returns>The satisfiability errors, empty when the requirements are satisfiable.</returns>
    public ImmutableArray<SatisfiabilityError> Validate(
        SelectionSetNode requirements,
        MutableObjectTypeDefinition contextType,
        SatisfiabilityPathItem? parentPathItem,
        IReadOnlyList<SatisfiabilityPathItem> ancestors,
        RequirementScope scope,
        string? excludeSchemaName,
        SatisfiabilityExplanationBudget budget,
        bool allowNestedFieldsFromExcludedSchema = false,
        SatisfiabilityPath? cycleDetectionPath = null,
        ImmutableHashSet<string>? unavailableSchemaNames = null)
        => Validate(
            requirements,
            contextType,
            parentPathItem,
            ancestors,
            scope,
            excludeSchemaName,
            budget,
            allowNestedFieldsFromExcludedSchema,
            cycleDetectionPath,
            unavailableSchemaNames ?? [],
            readsArrivalData: false,
            routeRestriction: null,
            out _);

    /// <summary>
    /// Validates that <paramref name="requirements"/> can be resolved for <paramref name="contextType"/> at the
    /// position described by <paramref name="parentPathItem"/>, reading first-level fields from the calls of the
    /// access path when <paramref name="readsArrivalData"/> is set, and from the holding call or a call of another
    /// schema that resolves the field returning the object only in <paramref name="routeRestriction"/> unless it is
    /// <c>null</c>. <paramref name="routes"/> receives the schemas of the calls of either kind that the walk read
    /// when it read more than one and one of another schema, and <c>null</c> otherwise.
    /// </summary>
    private ImmutableArray<SatisfiabilityError> Validate(
        SelectionSetNode requirements,
        MutableObjectTypeDefinition contextType,
        SatisfiabilityPathItem? parentPathItem,
        IReadOnlyList<SatisfiabilityPathItem> ancestors,
        RequirementScope scope,
        string? excludeSchemaName,
        SatisfiabilityExplanationBudget budget,
        bool allowNestedFieldsFromExcludedSchema,
        SatisfiabilityPath? cycleDetectionPath,
        ImmutableHashSet<string> unavailableSchemaNames,
        bool readsArrivalData,
        string? routeRestriction,
        out SortedSet<string>? routes)
    {
        var context = new RequirementsValidatorContext(
            contextType,
            parentPathItem,
            ancestors,
            scope,
            excludeSchemaName,
            allowNestedFieldsFromExcludedSchema,
            cycleDetectionPath,
            unavailableSchemaNames,
            readsArrivalData,
            routeRestriction,
            budget);

        var errors = new List<SatisfiabilityError>();

        foreach (var selection in requirements.Selections)
        {
            if (budget.IsExhausted)
            {
                break;
            }

            // Wrap each top-level selection in a selection set.
            var selectionSet = new SelectionSetNode([selection]);

            var requirementErrors = Visit(selectionSet, context);

            if (!requirementErrors.IsEmpty)
            {
                errors.Add(
                    budget.CreateError(
                        string.Format(
                            RequirementsValidator_UnableToSatisfyRequirement,
                            selection.ToString(indented: false)),
                        requirementErrors));
            }
        }

        routes = context.ReadsFieldRoute && context.RouteReads.Count > 1 ? context.RouteReads : null;
        return [.. errors];
    }

    private ImmutableArray<SatisfiabilityError> Visit(
        SelectionSetNode selectionSet,
        RequirementsValidatorContext context)
    {
        var errors = new List<SatisfiabilityError>();

        foreach (var selection in selectionSet.Selections)
        {
            if (context.Budget.IsExhausted)
            {
                break;
            }

            switch (selection)
            {
                case FieldNode fieldNode:
                    var fieldErrors = Visit(fieldNode, context);

                    if (fieldErrors.Length != 0)
                    {
                        var type = context.TypeContext.Peek();
                        var message =
                            includeSatisfiabilityPaths
                                ? string.Format(
                                    RequirementsValidator_UnableToAccessFieldOnPath,
                                    type.Name,
                                    fieldNode.Name.Value,
                                    string.Join(" -> ", context.Path.Reverse()))
                                : string.Format(
                                    RequirementsValidator_UnableToAccessField,
                                    type.Name,
                                    fieldNode.Name.Value);

                        errors.Add(context.Budget.CreateError(message, [.. fieldErrors]));
                    }

                    break;

                case InlineFragmentNode inlineFragmentNode:
                    if (inlineFragmentNode.TypeCondition is null)
                    {
                        break;
                    }

                    var fragmentType = schema.Types[inlineFragmentNode.TypeCondition.Name.Value];

                    // The fragment only applies when the current type is one of its possible types.
                    if (schema.GetPossibleTypes(fragmentType).Contains(context.TypeContext.Peek()))
                    {
                        errors.AddRange(Visit(inlineFragmentNode.SelectionSet, context));
                    }

                    break;
            }
        }

        return [.. errors];
    }

    private ImmutableArray<SatisfiabilityError> Visit(
        FieldNode fieldNode,
        RequirementsValidatorContext context)
    {
        var errors = new List<SatisfiabilityError>();
        var type = context.TypeContext.Peek();

        if (!type.Fields.TryGetField(fieldNode.Name.Value, out var field))
        {
            errors.Add(
                context.Budget.CreateError(
                    string.Format(
                        RequirementsValidator_FieldDoesNotExistOnType,
                        fieldNode.Name.Value,
                        type.Name)));

            return [.. errors];
        }

        var previousPathItem = context.Path.TryPeek(out var item) ? item : null;
        var previousSchemaName = previousPathItem?.SchemaName;

        // The excluded schema never resolves a first-level field of the requirement, and resolves a
        // nested field only when the context allows it.
        var excludeSchemaName =
            context.Depth == 0 || !context.AllowNestedFieldsFromExcludedSchema
                ? context.ExcludeSchemaName
                : null;
        // The cache tells an access path item, whose holding call may depend on unavailable data, apart
        // from an item that the walk created.
        var previousIsArrival = previousPathItem is not null && IsArrivalItem(previousPathItem, context);
        var cacheKey = new FieldAccessCacheKey(field, type, previousSchemaName, excludeSchemaName, previousIsArrival);

        if (context.FieldAccessCache.Contains(cacheKey))
        {
            return [];
        }

        var schemaNames = field.GetSchemaNames();
        if (excludeSchemaName is not null)
        {
            schemaNames = schemaNames.Remove(excludeSchemaName);
        }
        if (!context.UnavailableSchemaNames.IsEmpty)
        {
            schemaNames = schemaNames.RemoveAll(context.UnavailableSchemaNames.Contains);
        }
        var fieldType = field.Type.AsTypeDefinition();
        var optionCount = 0;
        var skippedDueToProvidedSelectionSet = false;

        // The fixpoint facts are computed with the data of every schema available, so they can
        // only confirm a transition or requirement when no schema is unavailable.
        var canUseFacts = context.UnavailableSchemaNames.IsEmpty;

        foreach (var schemaName in schemaNames)
        {
            if (context.Budget.IsExhausted)
            {
                break;
            }

            SelectionSetNode? providedSelectionSet = null;

            if (previousPathItem?.ProvidedSelectionSet is not null
                && previousSchemaName == schemaName
                && !previousPathItem.TryGetProvidedSelectionSet(
                    field,
                    type,
                    schemaName,
                    schema,
                    out providedSelectionSet))
            {
                skippedDueToProvidedSelectionSet = true;
                continue;
            }

            // A partial (@external) field is never a resolution candidate in its declaring schema;
            // only an event stream message can make it an option. @provides never does (PR #231).
            if (field.IsPartial(schemaName)
                && previousPathItem?.ProvidesViaEventStream(field, type, schemaName, schema) != true)
            {
                continue;
            }

            var pathItem = new SatisfiabilityPathItem(field, type, schemaName);

            // Validate that we are not in a cycle.
            if (!context.CycleDetectionPath.Push(pathItem))
            {
                CycleCount++;
                errors.Add(
                    context.Budget.CreateError(
                        string.Format(
                            RequirementsValidator_CycleDetected,
                            context.CycleDetectionPath,
                            pathItem)));

                continue;
            }

            // Validate transition between source schemas, which reads the holding schema directly only
            // when the call holding the type was made without the unavailable data. The fixpoint answers
            // the direct-lookup route in O(1); only when it cannot confirm the transition, or when a
            // provided selection set narrows the context, do we fall back to the full recursion that
            // builds the error.
            var transitionRequired =
                (previousSchemaName != schemaName
                    && (previousSchemaName is null
                        || previousPathItem?.ProvidedSelectionSet is not null
                        || !canUseFacts
                        || !_facts.CanTransition(type, schemaName, previousSchemaName)))
                || (previousIsArrival
                    && previousSchemaName == schemaName
                    && !canUseFacts
                    && !IsHoldingCallAvailable(previousPathItem!, context));
            var arrivalRead = ArrivalRead.None;

            // A first-level field of requirement data is also read from a call of the access path that holds the
            // object, or from a call that resolves the field returning it, if the walk may read that call.
            if (previousIsArrival && context.ReadsArrivalData && context.Depth == 0)
            {
                arrivalRead = transitionRequired
                    ? GetArrivalRead(previousPathItem!, schemaName, context)
                    : previousSchemaName == schemaName ? ArrivalRead.Holder : ArrivalRead.None;

                if (arrivalRead is not ArrivalRead.None)
                {
                    transitionRequired = !context.AllowsRoute(arrivalRead, schemaName);
                    arrivalRead = transitionRequired ? ArrivalRead.None : arrivalRead;
                }
            }

            if (transitionRequired)
            {
                var transitionErrors = ValidateSourceSchemaTransition(
                    type,
                    context,
                    transitionToSchemaName: schemaName,
                    [.. context.Path, .. context.Ancestors]);

                if (!transitionErrors.IsEmpty)
                {
                    errors.Add(
                        context.Budget.CreateError(
                            string.Format(
                                RequirementsValidator_UnableToTransitionBetweenSchemas,
                                previousSchemaName,
                                schemaName,
                                pathItem),
                            transitionErrors));

                    context.CycleDetectionPath.Pop();

                    continue;
                }
            }

            // Validate field requirements (@require). The fixpoint answers whether the requirement
            // holds in O(1); only when it does not, or when a provided selection set narrows the
            // context, do we re-run the recursion to build the error tree.
            var requirements = field.GetFusionRequiresRequirements(schemaName);

            if (requirements is not null
                && (previousPathItem?.ProvidedSelectionSet is not null
                    || !canUseFacts
                    || !_facts.IsFieldResolvableOn(type, field, schemaName)))
            {
                // The path above the requiring field is the requirement path followed by the path
                // above the context.
                IReadOnlyList<SatisfiabilityPathItem> ancestors =
                    [.. context.Path.Skip(1), .. context.Ancestors];

                var requirementErrors =
                    Validate(
                        requirements,
                        type,
                        previousPathItem,
                        ancestors,
                        context.Scope,
                        excludeSchemaName: schemaName,
                        context.Budget,
                        allowNestedFieldsFromExcludedSchema: true,
                        cycleDetectionPath: context.CycleDetectionPath,
                        unavailableSchemaNames: context.UnavailableSchemaNames);

                if (requirementErrors.IsEmpty)
                {
                    requirementErrors =
                        ValidateReentry(
                            requirements,
                            type,
                            field,
                            schemaName,
                            previousPathItem,
                            ancestors,
                            context.Scope,
                            RequirementsValidator_NoLookupForRequiringField,
                            RequirementsValidator_UnableToSatisfyRequirementForLookup,
                            context.Budget,
                            cycleDetectionPath: context.CycleDetectionPath,
                            unavailableSchemaNames: context.UnavailableSchemaNames);
                }

                if (requirementErrors.Length != 0)
                {
                    errors.Add(
                        context.Budget.CreateError(
                            string.Format(
                                SatisfiabilityValidator_UnableToSatisfyRequirement,
                                requirements.ToString(indented: false),
                                pathItem),
                            requirementErrors));

                    context.CycleDetectionPath.Pop();

                    continue;
                }
            }

            optionCount++;
            context.CycleDetectionPath.Pop();

            context.Path.Push(pathItem with { ProvidedSelectionSet = providedSelectionSet });

            if (fieldNode.SelectionSet is null)
            {
                context.Path.Pop();
                errors.Clear();
                context.AddRoute(arrivalRead, schemaName);
                break;
            }

            var possibleTypes = fieldType.GetPossibleTypes(schemaName, schema);
            var childErrors = new List<SatisfiabilityError>();

            context.Depth++;

            foreach (var possibleType in possibleTypes)
            {
                if (context.Budget.IsExhausted)
                {
                    break;
                }

                context.TypeContext.Push(possibleType);

                var requirementErrors = Visit(fieldNode.SelectionSet, context);

                if (requirementErrors.IsEmpty)
                {
                    context.TypeContext.Pop();
                    continue;
                }

                childErrors.AddRange(requirementErrors);

                context.TypeContext.Pop();
            }

            context.Depth--;

            if (childErrors.Count == 0)
            {
                errors.Clear();
            }
            else
            {
                errors.AddRange(childErrors);
            }

            context.Path.Pop();

            if (errors.Count == 0)
            {
                context.AddRoute(arrivalRead, schemaName);
                break;
            }
        }

        // Only a successful access is reused; a failed access is validated again where it recurs.
        if (optionCount > 0 && errors.Count == 0)
        {
            context.FieldAccessCache.Add(cacheKey);
        }

        if (schemaNames.Length == 0
            || (optionCount == 0 && errors.Count == 0 && skippedDueToProvidedSelectionSet))
        {
            errors.Add(
                context.Budget.CreateError(
                    string.Format(
                        RequirementsValidator_NoOtherSchemasContainField,
                        type.Name,
                        field.Name)));
        }

        return [.. errors];
    }

    /// <summary>
    /// Validates that <paramref name="type"/>, held by the first item of <paramref name="pathFromLeaf"/>,
    /// can be entered in <paramref name="transitionToSchemaName"/>.
    /// </summary>
    private ImmutableArray<SatisfiabilityError> ValidateSourceSchemaTransition(
        MutableObjectTypeDefinition type,
        RequirementsValidatorContext context,
        string transitionToSchemaName,
        SatisfiabilityPathItem[] pathFromLeaf)
    {
        return SourceSchemaTransitionHelper.ValidateSourceSchemaTransition(
            schema,
            _lookupCache,
            type,
            transitionToSchemaName,
            pathFromLeaf,
            (contextType, pathFromParent, lookupRequirements) =>
                Validate(
                    lookupRequirements,
                    contextType,
                    pathFromParent.Count > 0 ? pathFromParent[0] : null,
                    pathFromParent.Count > 1 ? [.. pathFromParent.Skip(1)] : [],
                    context.Scope,
                    excludeSchemaName: transitionToSchemaName,
                    context.Budget,
                    cycleDetectionPath: context.CycleDetectionPath,
                    unavailableSchemaNames: context.UnavailableSchemaNames),
            RequirementsValidator_NoLookupsFoundForType,
            RequirementsValidator_UnableToSatisfyRequirementForLookup,
            ReroutesViewerFields(context, pathFromLeaf) ? queryViewerField : null,
            context.Anchor,
            context.UnavailableSchemaNames,
            context.Budget,
            transitionFails: false);
    }

    /// <summary>
    /// Determines whether a viewer field on <paramref name="pathFromLeaf"/> is re-rooted at
    /// <c>Query.viewer</c>, which holds on mutation and subscription paths of a walk that may take
    /// data from re-rooted calls.
    /// </summary>
    private bool ReroutesViewerFields(RequirementsValidatorContext context, SatisfiabilityPathItem[] pathFromLeaf)
        => context.Scope.AllowsReRootedCalls
            && (context.Anchor?.ReroutesViewerFields
                ?? (pathFromLeaf.Length > 0 && !ReferenceEquals(pathFromLeaf[^1].Type, schema.QueryType)));

    /// <summary>
    /// Determines whether <paramref name="item"/> is an item of the access path, either the item of the
    /// anchor or an item of the arrival path, rather than an item that the requirement walk created.
    /// </summary>
    private static bool IsArrivalItem(SatisfiabilityPathItem item, RequirementsValidatorContext context)
    {
        if (context.Anchor is { } anchor)
        {
            return ReferenceEquals(item, anchor.Item);
        }

        return context.ArrivalPath is { } arrivalPath && IndexOf(arrivalPath, item) >= 0;
    }

    /// <summary>
    /// Determines whether the call that holds the type of the access path item <paramref name="item"/>
    /// was made without the unavailable data of <paramref name="context"/>.
    /// </summary>
    private bool IsHoldingCallAvailable(SatisfiabilityPathItem item, RequirementsValidatorContext context)
    {
        if (context.Anchor is { } anchor)
        {
            return anchor.IsHolderAvailable(context.UnavailableSchemaNames);
        }

        return IsCallAvailable(context.ArrivalPath!, IndexOf(context.ArrivalPath!, item), context);
    }

    private static int IndexOf(SatisfiabilityPathItem[] arrivalPath, SatisfiabilityPathItem item)
    {
        for (var i = 0; i < arrivalPath.Length; i++)
        {
            if (ReferenceEquals(arrivalPath[i], item))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Determines whether the call that holds the type of <c>arrivalPath[index]</c> was made without the
    /// unavailable data of <paramref name="context"/>. That holds for a root call, for a node lookup, for the
    /// call holding the item above when it continues in the same schema, and for a new call into the
    /// schema of the item at the object above it.
    /// </summary>
    private bool IsCallAvailable(
        SatisfiabilityPathItem[] arrivalPath,
        int index,
        RequirementsValidatorContext context)
    {
        var item = arrivalPath[index];

        if (context.UnavailableSchemaNames.Contains(item.SchemaName))
        {
            return false;
        }

        if (index == arrivalPath.Length - 1
            || arrivalPath[index + 1].SchemaName == SatisfiabilityPathItem.GatewaySchemaName)
        {
            return true;
        }

        if (arrivalPath[index + 1].SchemaName == item.SchemaName
            && IsCallAvailable(arrivalPath, index + 1, context))
        {
            return true;
        }

        return item.Type is MutableObjectTypeDefinition objectType
            && ValidateSourceSchemaTransition(
                objectType,
                context,
                item.SchemaName,
                arrivalPath[(index + 1)..]).IsEmpty;
    }

    /// <summary>
    /// Gets the call of the access path that a requirement read at the object held by the access path item
    /// <paramref name="item"/> takes the data of <paramref name="schemaName"/> from without the unavailable data of
    /// <paramref name="context"/>: the call that holds the object, the call that holds its parent and serves the
    /// field returning it without requirements, or, when the schema holding the parent does not serve that field so
    /// and a single schema is unavailable, a call of a schema that resolves that field from the parent. A call made
    /// with the unavailable data is read only when a single schema is unavailable and the call does not depend on
    /// the last call of that schema on the path, which resolves the requiring field while the schema serves every
    /// field below it.
    /// </summary>
    private ArrivalRead GetArrivalRead(
        SatisfiabilityPathItem item,
        string schemaName,
        RequirementsValidatorContext context)
    {
        if (context.Anchor is { } anchor)
        {
            return anchor.GetArrivalRead(schemaName, context.UnavailableSchemaNames);
        }

        var arrivalPath = context.ArrivalPath!;
        var index = IndexOf(arrivalPath, item);

        if (context.Scope.BelowReRootedViewer![index])
        {
            return ArrivalRead.None;
        }

        if (item.SchemaName == schemaName)
        {
            return GetCallState(arrivalPath, index, context) is ArrivalCallState.Readable
                ? ArrivalRead.Holder
                : ArrivalRead.None;
        }

        if (GetParentItem(arrivalPath, index) is not { ProvidedSelectionSet: null } parentItem
            || item.Type is not MutableObjectTypeDefinition parentType)
        {
            return ArrivalRead.None;
        }

        var parentSchemaName = parentItem.SchemaName;
        var parentServesField = ServesField(item.Field, parentSchemaName);

        // The call that holds the parent continues to the object when its schema serves the field without
        // requirements.
        if (parentServesField && item.Field.GetFusionRequiresRequirements(parentSchemaName) is null)
        {
            return parentSchemaName == schemaName && IsParentCallReadable(arrivalPath, index, context)
                ? ArrivalRead.Parent
                : ArrivalRead.None;
        }

        return context.UnavailableSchemaNames.Count == 1
            && (parentServesField || !parentItem.Provides(item.Field, parentType, parentSchemaName, schema))
            && ServesField(item.Field, schemaName)
            && (parentSchemaName != schemaName || IsParentCallReadable(arrivalPath, index, context))
            && ResolvesFieldAtParent(index, schemaName, context)
                ? ArrivalRead.Route
                : ArrivalRead.None;
    }

    /// <summary>
    /// Determines whether a requirement read at the object of <c>arrivalPath[index]</c> without the unavailable
    /// data of <paramref name="context"/> takes data from the call that holds its parent.
    /// </summary>
    private bool IsParentCallReadable(
        SatisfiabilityPathItem[] arrivalPath,
        int index,
        RequirementsValidatorContext context)
        => GetCallState(arrivalPath, index + 1, context) switch
        {
            ArrivalCallState.Readable => true,
            ArrivalCallState.Pending => IsPendingCallReleased(arrivalPath, index, context),
            _ => false
        };

    /// <summary>
    /// Determines whether <paramref name="schemaName"/> resolves the field of the access path item at
    /// <paramref name="index"/> as an option at the visit of the object that declares the field.
    /// </summary>
    private static bool ResolvesFieldAtParent(int index, string schemaName, RequirementsValidatorContext context)
        => context.Scope.ResolvesFieldAtParent!(index, schemaName, context.Budget);

    /// <summary>
    /// Determines whether <paramref name="schemaName"/> serves <paramref name="field"/> without it being partial.
    /// </summary>
    private static bool ServesField(MutableOutputFieldDefinition field, string schemaName)
        => field.ExistsInSchema(schemaName) && !field.IsPartial(schemaName);

    /// <summary>
    /// Gets whether a requirement read without the unavailable data of <paramref name="context"/> takes data from
    /// the call that holds the object of <c>arrivalPath[index]</c>.
    /// </summary>
    private ArrivalCallState GetCallState(
        SatisfiabilityPathItem[] arrivalPath,
        int index,
        RequirementsValidatorContext context)
    {
        var item = arrivalPath[index];
        var unavailableSchemaNames = context.UnavailableSchemaNames;

        if (unavailableSchemaNames.Contains(item.SchemaName) || item.ProvidedSelectionSet is not null)
        {
            return ArrivalCallState.None;
        }

        if (IsCallMadeWithoutUnavailableData(arrivalPath, index, context))
        {
            return ArrivalCallState.Readable;
        }

        var parentItem = GetParentItem(arrivalPath, index);

        if (parentItem?.SchemaName == item.SchemaName)
        {
            return GetCallState(arrivalPath, index + 1, context) switch
            {
                ArrivalCallState.Readable => ArrivalCallState.Readable,
                ArrivalCallState.Pending when IsPendingCallReleased(arrivalPath, index, context)
                    => ArrivalCallState.Readable,
                ArrivalCallState.Pending => ArrivalCallState.Pending,
                _ => ArrivalCallState.None
            };
        }

        if (unavailableSchemaNames.Count != 1)
        {
            return ArrivalCallState.None;
        }

        return parentItem?.SchemaName == unavailableSchemaNames.First()
            && !IsPendingCallReleased(arrivalPath, index, context)
                ? ArrivalCallState.Pending
                : ArrivalCallState.Readable;
    }

    /// <summary>
    /// Determines whether a call above <c>arrivalPath[index]</c> that depends on the last call of the only
    /// unavailable schema of <paramref name="context"/> no longer depends on the call that resolves the requiring
    /// field at the object of the item, because that schema does not serve the field of the item or holds the
    /// object.
    /// </summary>
    private static bool IsPendingCallReleased(
        SatisfiabilityPathItem[] arrivalPath,
        int index,
        RequirementsValidatorContext context)
    {
        var item = arrivalPath[index];
        var unavailableSchemaNames = context.UnavailableSchemaNames;

        return unavailableSchemaNames.Count != 1
            || unavailableSchemaNames.Contains(item.SchemaName)
            || !item.Field.ExistsInSchema(unavailableSchemaNames.First());
    }

    /// <summary>
    /// Gets the item above <c>arrivalPath[index]</c>, or <c>null</c> at a root field or a node lookup.
    /// </summary>
    private static SatisfiabilityPathItem? GetParentItem(SatisfiabilityPathItem[] arrivalPath, int index)
        => index + 1 < arrivalPath.Length
            && arrivalPath[index + 1].SchemaName != SatisfiabilityPathItem.GatewaySchemaName
                ? arrivalPath[index + 1]
                : null;

    /// <summary>
    /// Determines whether the schema of <c>arrivalPath[index]</c> holds its object without the unavailable data
    /// of <paramref name="context"/>, through the call that holds it or by entering the object again.
    /// </summary>
    private bool IsCallMadeWithoutUnavailableData(
        SatisfiabilityPathItem[] arrivalPath,
        int index,
        RequirementsValidatorContext context)
    {
        var item = arrivalPath[index];

        if (IsCallAvailable(arrivalPath, index, context))
        {
            return true;
        }

        var objectType = index > 0 ? arrivalPath[index - 1].Type : context.TypeContext.Peek();

        return !context.UnavailableSchemaNames.Contains(item.SchemaName)
            && objectType is MutableObjectTypeDefinition objectTypeDefinition
            && ValidateSourceSchemaTransition(
                objectTypeDefinition,
                context,
                item.SchemaName,
                arrivalPath[index..]).IsEmpty;
    }

    /// <summary>
    /// Validates that <paramref name="schemaName"/> can resolve <paramref name="field"/> once its
    /// requirements have been fetched from other schemas. This holds when the schema has a lookup for
    /// <paramref name="type"/> whose key is resolvable while the type is held on the schema, or when the
    /// requirements can be resolved without data from the schema or from calls re-rooted at <c>Query.viewer</c>.
    /// </summary>
    /// <param name="requirements">The requirement selection set of the field.</param>
    /// <param name="type">The object type declaring the field.</param>
    /// <param name="field">The field with requirements.</param>
    /// <param name="schemaName">The schema resolving the field.</param>
    /// <param name="parentPathItem">The path item holding the type, or null at a root type.</param>
    /// <param name="ancestors">
    /// The path items above <paramref name="parentPathItem"/>, from the nearest to the root. With an
    /// anchor in <paramref name="scope"/>, they end at the item of the anchor.
    /// </param>
    /// <param name="scope">The part of the access path that the walk reads above its first item.</param>
    /// <param name="noLookupMessageFormat">The message format used when the schema cannot be re-entered.</param>
    /// <param name="unableToSatisfyRequirementForLookupMessageFormat">
    /// The message format used when a lookup key cannot be satisfied.
    /// </param>
    /// <param name="budget">The budget for the explanation entries of the search.</param>
    /// <param name="cycleDetectionPath">The shared cycle detection path, or null to start a new one.</param>
    /// <param name="unavailableSchemaNames">
    /// Schemas whose data is unavailable at the position for any purpose.
    /// </param>
    /// <returns>The satisfiability errors, empty when the field can be resolved.</returns>
    public ImmutableArray<SatisfiabilityError> ValidateReentry(
        SelectionSetNode requirements,
        MutableObjectTypeDefinition type,
        MutableOutputFieldDefinition field,
        string schemaName,
        SatisfiabilityPathItem? parentPathItem,
        IReadOnlyList<SatisfiabilityPathItem> ancestors,
        RequirementScope scope,
        string noLookupMessageFormat,
        string unableToSatisfyRequirementForLookupMessageFormat,
        SatisfiabilityExplanationBudget budget,
        SatisfiabilityPath? cycleDetectionPath = null,
        ImmutableHashSet<string>? unavailableSchemaNames = null)
    {
        if (schema.IsRootOperationType(type))
        {
            return [];
        }

        unavailableSchemaNames ??= [];

        var lookupErrors = new List<SatisfiabilityError>();

        foreach (var lookup in _lookupCache.GetPossibleFusionLookupDirectives(type, schemaName))
        {
            if (budget.IsExhausted)
            {
                break;
            }

            var lookupKeyArg = (string)lookup.Arguments[WellKnownArgumentNames.Key].Value!;
            var lookupFieldArg = (string)lookup.Arguments[WellKnownArgumentNames.Field].Value!;
            var lookupPathArg = (string?)lookup.Arguments[WellKnownArgumentNames.Path].Value;
            var lookupRequirements = ParseSelectionSet($"{{ {lookupKeyArg} }}");

            // The key is fetched by the first call to the schema, so the schema itself is a valid
            // source for the key fields.
            var keyErrors =
                Validate(
                    lookupRequirements,
                    type,
                    parentPathItem,
                    ancestors,
                    scope,
                    excludeSchemaName: null,
                    budget,
                    cycleDetectionPath: cycleDetectionPath,
                    unavailableSchemaNames: unavailableSchemaNames);

            if (keyErrors.IsEmpty)
            {
                return [];
            }

            var lookupFieldName = ParseFieldDefinition(lookupFieldArg).Name.Value;
            var lookupName = lookupPathArg is null
                ? lookupFieldName
                : $"{lookupPathArg}.{lookupFieldName}";

            lookupErrors.Add(
                budget.CreateError(
                    string.Format(
                        unableToSatisfyRequirementForLookupMessageFormat,
                        lookupRequirements.ToString(indented: false),
                        lookupName,
                        schemaName),
                    keyErrors));
        }

        // Without a lookup the field can only be resolved by a call that holds the type, which works when
        // the requirements depend neither on that call nor on a call re-rooted at Query.viewer.
        if (CanResolveInHoldingCall(
                requirements,
                type,
                schemaName,
                parentPathItem,
                ancestors,
                scope.WithoutReRootedCalls(),
                budget,
                cycleDetectionPath,
                unavailableSchemaNames.Add(schemaName)))
        {
            return [];
        }

        return
        [
            budget.CreateError(
                string.Format(noLookupMessageFormat, type.Name, field.Name, schemaName),
                [.. lookupErrors])
        ];
    }

    /// <summary>
    /// Determines whether <paramref name="schemaName"/> can enter <paramref name="type"/> again through one of its
    /// lookups whose key resolves at the position described by <paramref name="parentPathItem"/>.
    /// </summary>
    public bool CanReenterThroughLookup(
        MutableObjectTypeDefinition type,
        string schemaName,
        SatisfiabilityPathItem? parentPathItem,
        IReadOnlyList<SatisfiabilityPathItem> ancestors,
        RequirementScope scope,
        SatisfiabilityExplanationBudget budget)
    {
        foreach (var lookup in _lookupCache.GetPossibleFusionLookupDirectives(type, schemaName))
        {
            if (Validate(
                    _lookupCache.GetLookupKey(lookup),
                    type,
                    parentPathItem,
                    ancestors,
                    scope,
                    excludeSchemaName: null,
                    budget).IsEmpty)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether <paramref name="requirements"/> can be resolved for <paramref name="type"/> without the
    /// data of <paramref name="unavailableSchemaNames"/>, which include the requiring schema
    /// <paramref name="schemaName"/>, so that a call of that schema holding the type resolves the requiring field.
    /// First-level fields read from the call that holds the object and from calls of other schemas that resolve
    /// the field returning it all come from one of these calls.
    /// </summary>
    private bool CanResolveInHoldingCall(
        SelectionSetNode requirements,
        MutableObjectTypeDefinition type,
        string schemaName,
        SatisfiabilityPathItem? parentPathItem,
        IReadOnlyList<SatisfiabilityPathItem> ancestors,
        RequirementScope scope,
        SatisfiabilityExplanationBudget budget,
        SatisfiabilityPath? cycleDetectionPath,
        ImmutableHashSet<string> unavailableSchemaNames)
    {
        if (!Validate(
                requirements,
                type,
                parentPathItem,
                ancestors,
                scope,
                excludeSchemaName: schemaName,
                budget,
                allowNestedFieldsFromExcludedSchema: false,
                cycleDetectionPath,
                unavailableSchemaNames,
                readsArrivalData: true,
                routeRestriction: null,
                out var routes).IsEmpty)
        {
            return false;
        }

        if (routes is null)
        {
            return true;
        }

        // First-level fields read from these calls must all come from the calls of one schema.
        foreach (var route in routes)
        {
            if (Validate(
                    requirements,
                    type,
                    parentPathItem,
                    ancestors,
                    scope,
                    excludeSchemaName: schemaName,
                    budget,
                    allowNestedFieldsFromExcludedSchema: false,
                    cycleDetectionPath,
                    unavailableSchemaNames,
                    readsArrivalData: true,
                    routeRestriction: route,
                    out _).IsEmpty)
            {
                return true;
            }
        }

        return false;
    }
}

internal sealed class RequirementsValidatorContext
{
    private readonly string? _routeRestriction;

    public RequirementsValidatorContext(
        MutableObjectTypeDefinition contextType,
        SatisfiabilityPathItem? parentPathItem,
        IReadOnlyList<SatisfiabilityPathItem> ancestors,
        RequirementScope scope,
        string? excludeSchemaName,
        bool allowNestedFieldsFromExcludedSchema,
        SatisfiabilityPath? cycleDetectionPath,
        ImmutableHashSet<string> unavailableSchemaNames,
        bool readsArrivalData,
        string? routeRestriction,
        SatisfiabilityExplanationBudget budget)
    {
        TypeContext.Push(contextType);

        if (parentPathItem is not null)
        {
            Path.Push(parentPathItem);
        }

        Ancestors = ancestors;
        Scope = scope;
        ExcludeSchemaName = excludeSchemaName;
        AllowNestedFieldsFromExcludedSchema = allowNestedFieldsFromExcludedSchema;
        CycleDetectionPath = cycleDetectionPath ?? [];
        UnavailableSchemaNames = unavailableSchemaNames;
        ReadsArrivalData = readsArrivalData;
        _routeRestriction = routeRestriction;
        Budget = budget;
    }

    public Stack<MutableObjectTypeDefinition> TypeContext { get; } = [];

    /// <summary>
    /// Gets the requirement path, from the item holding the type being visited down to the item holding
    /// the context type, which may repeat an item.
    /// </summary>
    public Stack<SatisfiabilityPathItem> Path { get; } = [];

    /// <summary>
    /// Gets the path items above the first item of <see cref="Path"/>, from the nearest to the root.
    /// </summary>
    public IReadOnlyList<SatisfiabilityPathItem> Ancestors { get; }

    /// <summary>
    /// Gets the part of the access path that the walk reads above the first item of <see cref="Path"/>.
    /// </summary>
    public RequirementScope Scope { get; }

    /// <summary>
    /// Gets the anchor that the walk stops at, or <c>null</c> when it walks the path up to the root.
    /// </summary>
    public PathAnchor? Anchor => Scope.Anchor;

    /// <summary>
    /// Gets the access path items, from the leaf to the root, that the walk reads without an
    /// <see cref="Anchor"/>, or <c>null</c> if it treats every call on the path as available.
    /// </summary>
    public SatisfiabilityPathItem[]? ArrivalPath => Scope.ArrivalPath;

    /// <summary>
    /// Gets the schema that must not resolve the requirement, or null when every schema is allowed.
    /// </summary>
    public string? ExcludeSchemaName { get; }

    /// <summary>
    /// Gets a value indicating whether the excluded schema may resolve the fields below the first
    /// level of the requirement.
    /// </summary>
    public bool AllowNestedFieldsFromExcludedSchema { get; }

    /// <summary>
    /// Gets or sets the number of requirement fields above the field being visited, which is zero
    /// for a first-level field.
    /// </summary>
    public int Depth { get; set; }

    public SatisfiabilityPath CycleDetectionPath { get; }

    /// <summary>
    /// Gets the schemas whose data is unavailable at the position for any purpose, including
    /// lookup keys and nested requirements.
    /// </summary>
    public ImmutableHashSet<string> UnavailableSchemaNames { get; }

    /// <summary>
    /// Gets whether a first-level field of the requirement is read from a call of the access path that holds
    /// the object, or from a call that resolves the field returning it.
    /// </summary>
    public bool ReadsArrivalData { get; }

    /// <summary>
    /// Gets the schemas of the calls that hold the object, or of other schemas that resolve the field returning
    /// it, that the walk read a first-level field from.
    /// </summary>
    public SortedSet<string> RouteReads { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets whether the walk read a first-level field from a call of a schema that resolves the field returning
    /// the object as one of several schemas that can.
    /// </summary>
    public bool ReadsFieldRoute { get; private set; }

    /// <summary>
    /// Determines whether the walk may read a first-level field of <paramref name="schemaName"/> from the call
    /// that <paramref name="read"/> identifies.
    /// </summary>
    public bool AllowsRoute(ArrivalRead read, string schemaName)
        => read is ArrivalRead.Parent || _routeRestriction is null || _routeRestriction == schemaName;

    /// <summary>
    /// Records that the walk read a first-level field of <paramref name="schemaName"/> from the call that
    /// <paramref name="read"/> identifies.
    /// </summary>
    public void AddRoute(ArrivalRead read, string schemaName)
    {
        if (read is ArrivalRead.Holder or ArrivalRead.Route)
        {
            RouteReads.Add(schemaName);
            ReadsFieldRoute |= read is ArrivalRead.Route;
        }
    }

    /// <summary>
    /// Gets the budget for the explanation entries of the search.
    /// </summary>
    public SatisfiabilityExplanationBudget Budget { get; }

    public HashSet<FieldAccessCacheKey> FieldAccessCache { get; } = [];
}
