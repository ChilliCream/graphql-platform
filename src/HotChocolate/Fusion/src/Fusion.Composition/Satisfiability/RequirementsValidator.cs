using System.Collections.Immutable;
using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Satisfiability;

internal sealed class RequirementsValidator(
    MutableSchemaDefinition schema,
    FusionLookupDirectiveCache lookupCache,
    SatisfiabilityFacts facts,
    bool includeSatisfiabilityPaths)
{
    private readonly FusionLookupDirectiveCache _lookupCache = lookupCache;
    private readonly SatisfiabilityFacts _facts = facts;

    public ImmutableArray<SatisfiabilityError> Validate(
        SelectionSetNode requirements,
        MutableObjectTypeDefinition contextType,
        SatisfiabilityPathItem? parentPathItem,
        string excludeSchemaName,
        bool allowIntermediatesFromExcludedSchema = false,
        SatisfiabilityPath? cycleDetectionPath = null)
    {
        var context = new RequirementsValidatorContext(
            contextType,
            parentPathItem,
            excludeSchemaName,
            allowIntermediatesFromExcludedSchema,
            cycleDetectionPath);

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
        if (fieldNode.SelectionSet is null || !context.AllowIntermediatesFromExcludedSchema)
        {
            schemaNames = schemaNames.Remove(context.ExcludeSchemaName);
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
                    new RequirementsValidator(
                        schema,
                        _lookupCache,
                        _facts,
                        includeSatisfiabilityPaths).Validate(
                        requirements,
                        type,
                        context.Path.Peek(),
                        excludeSchemaName: schemaName,
                        allowIntermediatesFromExcludedSchema: true,
                        cycleDetectionPath: context.CycleDetectionPath);

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
                    cycleDetectionPath: context.CycleDetectionPath),
            RequirementsValidator_NoLookupsFoundForType,
            RequirementsValidator_UnableToSatisfyRequirementForLookup);
    }
}

internal sealed class RequirementsValidatorContext
{
    public RequirementsValidatorContext(
        MutableObjectTypeDefinition contextType,
        SatisfiabilityPathItem? parentPathItem,
        string excludeSchemaName,
        bool allowIntermediatesFromExcludedSchema = false,
        SatisfiabilityPath? cycleDetectionPath = null)
    {
        TypeContext.Push(contextType);

        if (parentPathItem is not null)
        {
            Path.Push(parentPathItem);
        }

        ExcludeSchemaName = excludeSchemaName;
        AllowIntermediatesFromExcludedSchema = allowIntermediatesFromExcludedSchema;
        CycleDetectionPath = cycleDetectionPath ?? [];
    }

    public Stack<MutableObjectTypeDefinition> TypeContext { get; } = [];

    public SatisfiabilityPath Path { get; } = [];

    public string ExcludeSchemaName { get; }

    public bool AllowIntermediatesFromExcludedSchema { get; }

    public SatisfiabilityPath CycleDetectionPath { get; }

    public HashSet<FieldAccessCacheKey> FieldAccessCache { get; } = [];
}
