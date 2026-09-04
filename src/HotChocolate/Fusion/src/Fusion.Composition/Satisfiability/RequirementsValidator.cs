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
    bool includeSatisfiabilityPaths)
{
    private readonly FusionLookupDirectiveCache _lookupCache = lookupCache;
    private readonly SatisfiabilityFacts _facts = facts;

    /// <summary>
    /// Validates that <paramref name="requirements"/> can be resolved for
    /// <paramref name="contextType"/> at the position described by <paramref name="parentPathItem"/>.
    /// </summary>
    /// <param name="requirements">The requirement selection set.</param>
    /// <param name="contextType">The object type the requirements are selected on.</param>
    /// <param name="parentPathItem">The path item holding the type, or null at a root type.</param>
    /// <param name="excludeSchemaName">
    /// The schema the requirement leaves must not be resolved from, or null to allow every schema.
    /// </param>
    /// <param name="allowIntermediatesFromExcludedSchema">
    /// Whether intermediate fields of the requirement may be resolved from the excluded schema.
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
        string? excludeSchemaName,
        bool allowIntermediatesFromExcludedSchema = false,
        SatisfiabilityPath? cycleDetectionPath = null,
        ImmutableHashSet<string>? unavailableSchemaNames = null)
    {
        var context = new RequirementsValidatorContext(
            contextType,
            parentPathItem,
            excludeSchemaName,
            allowIntermediatesFromExcludedSchema,
            cycleDetectionPath,
            unavailableSchemaNames ?? []);

        var errors = new List<SatisfiabilityError>();

        foreach (var selection in requirements.Selections)
        {
            // Wrap each top-level selection in a selection set.
            var selectionSet = new SelectionSetNode([selection]);

            var requirementErrors = Visit(selectionSet, context);

            if (!requirementErrors.IsEmpty)
            {
                errors.Add(new SatisfiabilityError(
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
                                    context.Path)
                                : string.Format(
                                    RequirementsValidator_UnableToAccessField,
                                    type.Name,
                                    fieldNode.Name.Value);

                        errors.Add(new SatisfiabilityError(message, [.. fieldErrors]));
                    }

                    break;

                case InlineFragmentNode inlineFragmentNode:
                    if (inlineFragmentNode.TypeCondition is null)
                    {
                        break;
                    }

                    var fragmentType = schema.Types[inlineFragmentNode.TypeCondition.Name.Value];
                    var fragmentPossibleTypes = schema.GetPossibleTypes(fragmentType);

                    foreach (var possibleType in fragmentPossibleTypes)
                    {
                        context.TypeContext.Push(possibleType);

                        var requirementErrors = Visit(inlineFragmentNode.SelectionSet, context);

                        if (requirementErrors.Any())
                        {
                            errors.AddRange(requirementErrors);
                        }

                        context.TypeContext.Pop();
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
                new SatisfiabilityError(
                    string.Format(
                        RequirementsValidator_FieldDoesNotExistOnType,
                        fieldNode.Name.Value,
                        type.Name)));

            return [.. errors];
        }

        var previousPathItem = context.Path.TryPeek(out var item) ? item : null;
        var previousSchemaName = previousPathItem?.SchemaName;
        var cacheKey = new FieldAccessCacheKey(field, type, previousSchemaName);

        if (context.FieldAccessCache.Contains(cacheKey))
        {
            return [];
        }

        // Leaf fields in the requirement must be sourced from outside the
        // excluded schema. Intermediate fields (with a sub-selection) may also
        // be sourced from the excluded schema when validating a field-level
        // @require: those intermediates are navigation steps the gateway can
        // resolve locally in the requiring schema as part of executing the
        // requiring field. For lookup-key validation the excluded schema has
        // not been entered yet, so intermediates must also come from outside
        // it (default behavior).
        var schemaNames = field.GetSchemaNames();
        if (context.ExcludeSchemaName is { } excludeSchemaName
            && (fieldNode.SelectionSet is null || !context.AllowIntermediatesFromExcludedSchema))
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

        foreach (var schemaName in schemaNames)
        {
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
                errors.Add(
                    new SatisfiabilityError(
                        string.Format(
                            RequirementsValidator_CycleDetected,
                            context.CycleDetectionPath,
                            pathItem)));

                continue;
            }

            // Validate transition between source schemas. The fixpoint answers the direct-lookup
            // route in O(1); only when it cannot confirm the transition, or when a provided selection
            // set narrows the context, do we fall back to the full recursion that builds the error.
            if (previousSchemaName != schemaName
                && (previousSchemaName is null
                    || previousPathItem?.ProvidedSelectionSet is not null
                    || !_facts.CanTransition(type, schemaName, previousSchemaName)))
            {
                var transitionErrors = ValidateSourceSchemaTransition(
                    type,
                    context,
                    transitionToSchemaName: schemaName);

                if (transitionErrors.Any())
                {
                    errors.Add(
                        new SatisfiabilityError(
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
                    || !_facts.IsFieldResolvableOn(type, field, schemaName)))
            {
                var requirementErrors =
                    Validate(
                        requirements,
                        type,
                        previousPathItem,
                        excludeSchemaName: schemaName,
                        allowIntermediatesFromExcludedSchema: true,
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
                            RequirementsValidator_NoLookupForRequiringField,
                            RequirementsValidator_UnableToSatisfyRequirementForLookup,
                            cycleDetectionPath: context.CycleDetectionPath,
                            unavailableSchemaNames: context.UnavailableSchemaNames);
                }

                if (requirementErrors.Length != 0)
                {
                    errors.Add(
                        new SatisfiabilityError(
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

            foreach (var possibleType in possibleTypes)
            {
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

        context.FieldAccessCache.Add(cacheKey);

        if (schemaNames.Length == 0
            || (optionCount == 0 && errors.Count == 0 && skippedDueToProvidedSelectionSet))
        {
            errors.Add(
                new SatisfiabilityError(
                    string.Format(
                        RequirementsValidator_NoOtherSchemasContainField,
                        type.Name,
                        field.Name)));
        }

        return [.. errors];
    }

    private ImmutableArray<SatisfiabilityError> ValidateSourceSchemaTransition(
        MutableObjectTypeDefinition type,
        RequirementsValidatorContext context,
        string transitionToSchemaName)
    {
        return SourceSchemaTransitionHelper.ValidateSourceSchemaTransition(
            _lookupCache,
            type,
            transitionToSchemaName,
            [.. context.Path],
            (contextType, parentPathItem, lookupRequirements) =>
                Validate(
                    lookupRequirements,
                    contextType,
                    parentPathItem,
                    excludeSchemaName: transitionToSchemaName,
                    cycleDetectionPath: context.CycleDetectionPath,
                    unavailableSchemaNames: context.UnavailableSchemaNames),
            RequirementsValidator_NoLookupsFoundForType,
            RequirementsValidator_UnableToSatisfyRequirementForLookup);
    }

    /// <summary>
    /// Validates that <paramref name="schemaName"/> can resolve <paramref name="field"/> once its
    /// requirements have been fetched from other schemas. This holds when the requirements can be
    /// resolved without data from the schema, or when the schema has a lookup for
    /// <paramref name="type"/> whose key is resolvable while the type is held on the schema.
    /// </summary>
    /// <param name="requirements">The requirement selection set of the field.</param>
    /// <param name="type">The object type declaring the field.</param>
    /// <param name="field">The field with requirements.</param>
    /// <param name="schemaName">The schema resolving the field.</param>
    /// <param name="parentPathItem">The path item holding the type, or null at a root type.</param>
    /// <param name="noLookupMessageFormat">The message format used when the schema cannot be re-entered.</param>
    /// <param name="unableToSatisfyRequirementForLookupMessageFormat">
    /// The message format used when a lookup key cannot be satisfied.
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
        string noLookupMessageFormat,
        string unableToSatisfyRequirementForLookupMessageFormat,
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
                    excludeSchemaName: null,
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
                new SatisfiabilityError(
                    string.Format(
                        unableToSatisfyRequirementForLookupMessageFormat,
                        lookupRequirements.ToString(indented: false),
                        lookupName,
                        schemaName),
                    keyErrors));
        }

        // Without a lookup the field can only be resolved by the call that already holds the type,
        // which works when the requirements do not depend on data produced by that call.
        var independentErrors =
            Validate(
                requirements,
                type,
                parentPathItem,
                excludeSchemaName: schemaName,
                cycleDetectionPath: cycleDetectionPath,
                unavailableSchemaNames: unavailableSchemaNames.Add(schemaName));

        if (independentErrors.IsEmpty)
        {
            return [];
        }

        return
        [
            new SatisfiabilityError(
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
        string? excludeSchemaName,
        bool allowIntermediatesFromExcludedSchema,
        SatisfiabilityPath? cycleDetectionPath,
        ImmutableHashSet<string> unavailableSchemaNames)
    {
        TypeContext.Push(contextType);

        if (parentPathItem is not null)
        {
            Path.Push(parentPathItem);
        }

        ExcludeSchemaName = excludeSchemaName;
        AllowIntermediatesFromExcludedSchema = allowIntermediatesFromExcludedSchema;
        CycleDetectionPath = cycleDetectionPath ?? [];
        UnavailableSchemaNames = unavailableSchemaNames;
    }

    public Stack<MutableObjectTypeDefinition> TypeContext { get; } = [];

    public SatisfiabilityPath Path { get; } = [];

    /// <summary>
    /// Gets the schema the requirement leaves must not be resolved from, or null when every
    /// schema is allowed.
    /// </summary>
    public string? ExcludeSchemaName { get; }

    public bool AllowIntermediatesFromExcludedSchema { get; }

    public SatisfiabilityPath CycleDetectionPath { get; }

    /// <summary>
    /// Gets the schemas whose data is unavailable at the position for any purpose, including
    /// lookup keys and nested requirements.
    /// </summary>
    public ImmutableHashSet<string> UnavailableSchemaNames { get; }

    public HashSet<FieldAccessCacheKey> FieldAccessCache { get; } = [];
}
