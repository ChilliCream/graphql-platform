using System.Collections.Immutable;
using System.Diagnostics;
using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.Satisfiability;

internal sealed class RequirementsValidator(MutableSchemaDefinition schema)
{
    public ImmutableArray<SatisfiabilityError> Validate(
        SelectionSetNode requirements,
        RequirementKind requirementKind,
        MutableObjectTypeDefinition contextType,
        SatisfiabilityPath parentPath,
        string excludeSchemaName,
        SatisfiabilityPath? cycleDetectionPath = null)
    {
        //Debug.WriteLine($"New context created. Parent path '{parentPath}'. Exclude schema '{excludeSchemaName}'.");

        var context = new RequirementsValidatorContext(
            requirementKind,
            contextType,
            parentPath,
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

        //Debug.WriteLine($"Failing selections: {errors.Count} of {requirements.Selections.Count}");

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
                    //Debug.Indent();
                    var fieldErrors = Visit(fieldNode, context);
                    //Debug.Unindent();

                    if (fieldErrors.Length != 0)
                    {
                        var type = context.TypeContext.Peek();

                        errors.Add(new SatisfiabilityError(
                            string.Format(
                                RequirementsValidator_UnableToAccessFieldOnPath,
                                type.Name,
                                fieldNode.Name.Value,
                                context.Path),
                            [.. fieldErrors]));
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

                        //Debug.Indent();
                        var requirementErrors = Visit(inlineFragmentNode.SelectionSet, context);
                        //Debug.Unindent();

                        if (requirementErrors.Any())
                        {
                            errors.Add(
                                new SatisfiabilityError(
                                    $"... can't satisfy requirement {inlineFragmentNode.SelectionSet.ToString(indented: false)} on type {possibleType.Name}",//tmp
                                    requirementErrors));
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

        if (context.FieldAccessCache.Contains((field, type, previousSchemaName)))
        {
            //Debug.WriteLine($"CACHE HIT: '{previousSchemaName ?? "ROOT"}:{type.Name}.{field.Name}'.");
            return [];
        }

        var schemaNames = field.GetSchemaNames().Remove(context.ExcludeSchemaName); // todo exclude at level 1 only?
        var fieldType = field.Type.AsTypeDefinition();

        foreach (var schemaName in schemaNames)
        {
            // If the field is marked as partial, it must be provided by the current schema for it
            // to be an option.
            if (field.IsPartial(schemaName)
                && previousPathItem?.Provides(field, type, schemaName, schema) != true)
            {
                //Debug.WriteLine($"Skipping partial field '{field.Name}' on path '{context.Path}'. Not provided.");
                continue;
            }

            var pathItem = new SatisfiabilityPathItem(field, type, schemaName);

            //Debug.WriteLine($"Checking satisfiability for field '{pathItem}'.");

            // Validate that we are not in a cycle.
            if (!context.CycleDetectionPath.Push(pathItem))
            {
                errors.Add(
                    new SatisfiabilityError(
                        string.Format(
                            RequirementsValidator_CycleDetected,
                            context.CycleDetectionPath,
                            pathItem)));

                //Debug.WriteLine(errors[^1]);

                continue;
            }

            // Validate transition between source schemas.
            if (previousSchemaName != schemaName)
            {
                //Debug.WriteLine($"Validating transition between schemas '{previousSchemaName}' and '{schemaName}'.");
                //Debug.Indent();

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

                    //Debug.Unindent();
                    //Debug.WriteLine("Transition failed.");

                    context.CycleDetectionPath.Pop();

                    continue;
                }

                //Debug.Unindent();
                //Debug.WriteLine("Transition validated.");
            }

            // Validate field requirements (@require).
            var requirements = field.GetFusionRequiresRequirements(schemaName);

            if (requirements is not null)
            {
                //Debug.Indent();
                var requirementErrors =
                    new RequirementsValidator(schema).Validate(
                        requirements,
                        RequirementKind.Field,
                        type,
                        context.Path,
                        excludeSchemaName: schemaName,
                        context.CycleDetectionPath);
                //Debug.Unindent();

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
            //Debug.WriteLine(context.Path);

            if (fieldNode.SelectionSet is null)
            {
                //Debug.WriteLine("LEAF NODE - BREAK");
                context.Path.Pop();
                errors.Clear();
                break;
            }

            var possibleTypes = fieldType.GetPossibleTypes(schemaName, schema);
            var childErrors = new List<SatisfiabilityError>();

            foreach (var possibleType in possibleTypes)
            {
                //Debug.WriteLine($"Pushing '{possibleType.Name}' to context.");
                context.TypeContext.Push(possibleType);

                //Debug.Indent();
                var requirementErrors = Visit(fieldNode.SelectionSet, context);

                if (requirementErrors.IsEmpty)
                {
                    //Debug.Unindent();
                    //Debug.WriteLine($"Popping '{fieldType.Name}' from context.");
                    context.TypeContext.Pop();
                    continue;
                }

                childErrors.AddRange(requirementErrors);

                //Debug.Unindent();

                //Debug.WriteLine($"Popping '{fieldType.Name}' from context.");
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
            //Debug.WriteLine($"[{context.Path}] (AFTER POP)");

            if (errors.Count == 0)
            {
                //Debug.WriteLine("BREAK!");
                break;
            }
        }

        context.FieldAccessCache.Add((field, type, previousSchemaName));

        if (schemaNames.Length == 0)
        {
            errors.Add(
                new SatisfiabilityError(
                    string.Format(
                        RequirementsValidator_NoOtherSchemasContainField,
                        type.Name,
                        field.Name)));
        }

        //Debug.WriteLine(errors.Count == 0 ? $"Found option for '{type.Name}.{field.Name}'." : $"Option not found for '{type.Name}.{field.Name}'.");

        return [.. errors];
    }

    private ImmutableArray<SatisfiabilityError> ValidateSourceSchemaTransition(
        MutableObjectTypeDefinition type,
        RequirementsValidatorContext context,
        string transitionToSchemaName)
    {
        var errors = new List<SatisfiabilityError>();

        // Get the list of union types that contain the current type.
        var unionTypes =
            schema.Types.OfType<MutableUnionTypeDefinition>().Where(u => u.Types.Contains(type));

        // Get the list of lookups for the current type in the destination schema.
        var lookupDirectives =
            type.GetFusionLookupDirectives(transitionToSchemaName, unionTypes).ToImmutableArray();

        if (!lookupDirectives.Any())
        {
            errors.Add(
                new SatisfiabilityError(
                    string.Format(
                        RequirementsValidator_NoLookupsFoundForType,
                        type.Name,
                        transitionToSchemaName)));

            //Debug.WriteLine(errors[^1]);

            return [.. errors];
        }

        //Debug.WriteLine($"{lookupDirectives.Length} lookups for type '{type.Name}' in schema '{transitionToSchemaName}'.");

        foreach (var lookupDirective in lookupDirectives)
        {
            var lookupKeyArg = (string)lookupDirective.Arguments["key"].Value!;
            var lookupFieldArg = (string)lookupDirective.Arguments["field"].Value!;
            var lookupPathArg = (string?)lookupDirective.Arguments["path"].Value;

            var lookupRequirements = ParseSelectionSet($"{{ {lookupKeyArg} }}");
            var lookupFieldName = ParseFieldDefinition(lookupFieldArg).Name.Value;

            //Debug.WriteLine($"Analyzing lookup field: {lookupFieldName}.");

            // Ensure that lookup requirements are satisfied.
            //Debug.Indent();

            var requirementErrors =
                Validate(
                    lookupRequirements,
                    RequirementKind.Lookup,
                    type,
                    context.Path,
                    excludeSchemaName: transitionToSchemaName,
                    context.CycleDetectionPath);

            //Debug.Unindent();

            if (requirementErrors.IsEmpty)
            {
                //Debug.WriteLine($"All requirements satisfied for lookup '{lookupFieldName}'.");
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
}

internal sealed class RequirementsValidatorContext
{
    public RequirementsValidatorContext(
        RequirementKind requirementKind,
        MutableObjectTypeDefinition contextType,
        SatisfiabilityPath parentPath,
        string excludeSchemaName,
        SatisfiabilityPath? cycleDetectionPath = null)
    {
        //Debug.WriteLine($"Pushing '{contextType.Name}' to context.");
        TypeContext.Push(contextType);
        RequirementKind = requirementKind;
        ParentPath = parentPath;
        Path.Push(parentPath.Peek());
        ExcludeSchemaName = excludeSchemaName;
        CycleDetectionPath = cycleDetectionPath ?? [];
    }

    public RequirementKind RequirementKind { get; }

    public Stack<MutableObjectTypeDefinition> TypeContext { get; } = [];

    // At which path this requirement was found.
    public SatisfiabilityPath ParentPath { get; }

    public SatisfiabilityPath Path { get; } = [];

    public string ExcludeSchemaName { get; }

    public SatisfiabilityPath CycleDetectionPath { get; }

    public HashSet<(MutableOutputFieldDefinition, MutableObjectTypeDefinition, string?)> FieldAccessCache { get; } = [];
}
