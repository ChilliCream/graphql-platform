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
    {
        var context = new RequirementsValidatorContext(
            contextType,
            parentPathItem,
            ancestors,
            scope,
            excludeSchemaName,
            allowNestedFieldsFromExcludedSchema,
            cycleDetectionPath,
            unavailableSchemaNames ?? [],
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
            if ((previousSchemaName != schemaName
                    && (previousSchemaName is null
                        || previousPathItem?.ProvidedSelectionSet is not null
                        || !canUseFacts
                        || !_facts.CanTransition(type, schemaName, previousSchemaName)))
                || (previousIsArrival
                    && previousSchemaName == schemaName
                    && !canUseFacts
                    && !IsHoldingCallAvailable(previousPathItem!, context)))
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
                            holdingCallIsReRooted: false,
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
    /// Validates that <paramref name="schemaName"/> can resolve <paramref name="field"/> once its
    /// requirements have been fetched from other schemas. This holds when the schema has a lookup for
    /// <paramref name="type"/> whose key is resolvable while the type is held on the schema, or when the
    /// requirements can be resolved without data from the schema.
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
    /// <param name="holdingCallIsReRooted">
    /// Whether the call that already holds the type in the schema is re-rooted at <c>Query.viewer</c>, so
    /// that it takes no requirement data from another re-rooted call.
    /// </param>
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
        bool holdingCallIsReRooted,
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

        // Without a lookup the field can only be resolved by the call that already holds the type,
        // which works when the requirements do not depend on data produced by that call. A call
        // re-rooted at Query.viewer takes no requirement data from another re-rooted call.
        if (Validate(
                requirements,
                type,
                parentPathItem,
                ancestors,
                holdingCallIsReRooted ? scope.WithoutReRootedCalls() : scope,
                excludeSchemaName: schemaName,
                budget,
                cycleDetectionPath: cycleDetectionPath,
                unavailableSchemaNames: unavailableSchemaNames.Add(schemaName)).IsEmpty)
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
}

internal sealed class RequirementsValidatorContext
{
    public RequirementsValidatorContext(
        MutableObjectTypeDefinition contextType,
        SatisfiabilityPathItem? parentPathItem,
        IReadOnlyList<SatisfiabilityPathItem> ancestors,
        RequirementScope scope,
        string? excludeSchemaName,
        bool allowNestedFieldsFromExcludedSchema,
        SatisfiabilityPath? cycleDetectionPath,
        ImmutableHashSet<string> unavailableSchemaNames,
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
    /// Gets the budget for the explanation entries of the search.
    /// </summary>
    public SatisfiabilityExplanationBudget Budget { get; }

    public HashSet<FieldAccessCacheKey> FieldAccessCache { get; } = [];
}
