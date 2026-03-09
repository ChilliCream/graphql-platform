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
    bool includeSatisfiabilityPaths = false)
{
    public ImmutableArray<SatisfiabilityError> Validate(
        SelectionSetNode requirements,
        MutableObjectTypeDefinition contextType,
        SatisfiabilityPathItem? parentPathItem,
        string excludeSchemaName,
        SatisfiabilityPath? cycleDetectionPath = null)
    {
        var context = new RequirementsValidatorContext(
            contextType,
            parentPathItem,
            excludeSchemaName,
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

        var schemaNames = field.GetSchemaNames().Remove(context.ExcludeSchemaName);
        var fieldType = field.Type.AsTypeDefinition();

        foreach (var schemaName in schemaNames)
        {
            // If the field is marked as partial, it must be provided by the current schema for it
            // to be an option.
            if (field.IsPartial(schemaName)
                && previousPathItem?.Provides(field, type, schemaName, schema) != true)
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

            // Validate transition between source schemas.
            if (previousSchemaName != schemaName)
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

            // Validate field requirements (@require).
            var requirements = field.GetFusionRequiresRequirements(schemaName);

            if (requirements is not null)
            {
                var requirementErrors =
                    new RequirementsValidator(schema, includeSatisfiabilityPaths).Validate(
                        requirements,
                        type,
                        context.Path.Peek(),
                        excludeSchemaName: schemaName,
                        context.CycleDetectionPath);

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

            context.CycleDetectionPath.Pop();

            context.Path.Push(pathItem);

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

        if (schemaNames.Length == 0)
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
        var errors = new List<SatisfiabilityError>();

        var lookupDirectives =
            schema.GetPossibleFusionLookupDirectives(type, transitionToSchemaName);

        if (!lookupDirectives.Any() && !CanTransitionToSchemaThroughPath(context.Path, transitionToSchemaName))
        {
            errors.Add(
                new SatisfiabilityError(
                    string.Format(
                        RequirementsValidator_NoLookupsFoundForType,
                        type.Name,
                        transitionToSchemaName)));

            return [.. errors];
        }

        foreach (var lookupDirective in lookupDirectives)
        {
            var lookupKeyArg = (string)lookupDirective.Arguments["key"].Value!;
            var lookupFieldArg = (string)lookupDirective.Arguments["field"].Value!;
            var lookupPathArg = (string?)lookupDirective.Arguments["path"].Value;

            var lookupRequirements = ParseSelectionSet($"{{ {lookupKeyArg} }}");
            var lookupFieldName = ParseFieldDefinition(lookupFieldArg).Name.Value;

            // Ensure that lookup requirements are satisfied.
            var requirementErrors =
                Validate(
                    lookupRequirements,
                    type,
                    context.Path.Peek(),
                    excludeSchemaName: transitionToSchemaName,
                    context.CycleDetectionPath);

            if (requirementErrors.IsEmpty)
            {
                return [];
            }

            var lookupName = lookupPathArg is null
                ? lookupFieldName
                : $"{lookupPathArg}.{lookupFieldName}";

            errors.Add(
                new SatisfiabilityError(
                    string.Format(
                        RequirementsValidator_UnableToSatisfyRequirementForLookup,
                        lookupRequirements.ToString(indented: false),
                        lookupName,
                        transitionToSchemaName),
                    requirementErrors));
        }

        return [.. errors];
    }

    /// <summary>
    /// We check whether the path we're currently on exists one-to-one
    /// on the given schema or whether a type on the path has a lookup
    /// on the given schema.
    /// </summary>
    private bool CanTransitionToSchemaThroughPath(
        SatisfiabilityPath path,
        string schemaName)
    {
        foreach (var pathItem in path)
        {
            var lookupDirectives =
                schema.GetPossibleFusionLookupDirectives(
                    pathItem.Type,
                    schemaName);

            var hasLookups = lookupDirectives.Count > 0;
            var fieldExists = pathItem.Field.ExistsInSchema(schemaName);

            if (hasLookups && fieldExists)
            {
                return true;
            }

            if (!fieldExists)
            {
                return false;
            }
        }

        return true;
    }
}

internal sealed class RequirementsValidatorContext
{
    public RequirementsValidatorContext(
        MutableObjectTypeDefinition contextType,
        SatisfiabilityPathItem? parentPathItem,
        string excludeSchemaName,
        SatisfiabilityPath? cycleDetectionPath = null)
    {
        TypeContext.Push(contextType);

        if (parentPathItem is not null)
        {
            Path.Push(parentPathItem);
        }

        ExcludeSchemaName = excludeSchemaName;
        CycleDetectionPath = cycleDetectionPath ?? [];
    }

    public Stack<MutableObjectTypeDefinition> TypeContext { get; } = [];

    public SatisfiabilityPath Path { get; } = [];

    public string ExcludeSchemaName { get; }

    public SatisfiabilityPath CycleDetectionPath { get; }

    public HashSet<FieldAccessCacheKey> FieldAccessCache { get; } = [];
}
