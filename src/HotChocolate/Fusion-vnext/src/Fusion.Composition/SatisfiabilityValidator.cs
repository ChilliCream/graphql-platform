using System.Collections.Immutable;
using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Results;
using HotChocolate.Fusion.Satisfiability;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion;

internal sealed class SatisfiabilityValidator(MutableSchemaDefinition schema, ICompositionLog log)
{
    private readonly RequirementsValidator _requirementsValidator = new(schema);

    public CompositionResult Validate()
    {
        var context = new SatisfiabilityValidatorContext();

        MutableObjectTypeDefinition?[] rootTypes =
            [schema.QueryType, schema.MutationType, schema.SubscriptionType];

        foreach (var rootType in rootTypes)
        {
            if (rootType is not null)
            {
                VisitObjectType(rootType, context);
            }
        }

        return log.HasErrors
            ? ErrorHelper.SatisfiabilityValidationFailed()
            : CompositionResult.Success();
    }

    private void VisitObjectType(
        MutableObjectTypeDefinition objectType,
        SatisfiabilityValidatorContext context)
    {
        context.TypeContext.Push(objectType);

        foreach (var field in objectType.Fields)
        {
            if (field.HasFusionInaccessibleDirective())
            {
                continue;
            }

            VisitOutputField(field, context);
        }

        context.TypeContext.Pop();
    }

    private void VisitOutputField(
        MutableOutputFieldDefinition field,
        SatisfiabilityValidatorContext context)
    {
        var type = context.TypeContext.Peek();
        var previousPathItem = context.Path.TryPeek(out var item) ? item : null;
        var previousSchemaName = previousPathItem?.SchemaName;
        var cacheKey = new FieldAccessCacheKey(field, type, previousSchemaName);

        if (context.FieldAccessCache.Contains(cacheKey))
        {
            return;
        }

        var schemaNames = field.GetSchemaNames(first: previousSchemaName);
        var cycle = false;
        var errors = new List<SatisfiabilityError>();
        var optionCount = 0;
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
                cycle = true;
                continue;
            }

            // Validate transition between source schemas.
            if (previousSchemaName is not null && previousSchemaName != schemaName)
            {
                var transitionErrors = ValidateSourceSchemaTransition(type, context, schemaName);

                if (transitionErrors.Any())
                {
                    errors.Add(
                        new SatisfiabilityError(
                            string.Format(
                                SatisfiabilityValidator_UnableToTransitionBetweenSchemas,
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
                    _requirementsValidator.Validate(
                        requirements,
                        type,
                        context.Path.Peek(),
                        excludeSchemaName: schemaName);

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

            context.Path.Push(pathItem);

            // Visit each of the possible types that the field may return.
            if (fieldType is MutableComplexTypeDefinition or MutableUnionTypeDefinition)
            {
                var possibleTypes = fieldType.GetPossibleTypes(schemaName, schema);

                foreach (var possibleType in possibleTypes)
                {
                    VisitObjectType(possibleType, context);
                }
            }

            context.Path.Pop();
            context.CycleDetectionPath.Pop();

            // When we reach a leaf field, we can break early as we only need a single option.
            if (fieldType.IsLeafType())
            {
                break;
            }
        }

        context.FieldAccessCache.Add(cacheKey);

        // Log an error if there are no options for accessing the field, and there is no cycle
        // (which would imply an option for accessing the same field repeatedly).
        // (f.e. relatedProduct.relatedProduct.relatedProduct)
        if (optionCount == 0 && !cycle)
        {
            var error = new SatisfiabilityError(
                string.Format(
                    SatisfiabilityValidator_UnableToAccessFieldOnPath,
                    type.Name,
                    field.Name,
                    context.Path),
                [.. errors]);

            log.Write(
                new LogEntry(error.ToString(), LogEntryCodes.Unsatisfiable, extension: error));
        }
    }

    private ImmutableArray<SatisfiabilityError> ValidateSourceSchemaTransition(
        MutableObjectTypeDefinition type,
        SatisfiabilityValidatorContext context,
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
                        SatisfiabilityValidator_NoLookupsFoundForType,
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
                _requirementsValidator.Validate(
                    lookupRequirements,
                    type,
                    context.Path.Peek(),
                    excludeSchemaName: transitionToSchemaName);

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
                        SatisfiabilityValidator_UnableToSatisfyRequirementForLookup,
                        lookupRequirements.ToString(indented: false),
                        lookupName,
                        transitionToSchemaName),
                    requirementErrors));
        }

        return [.. errors];
    }
}

internal sealed class SatisfiabilityValidatorContext
{
    public Stack<MutableObjectTypeDefinition> TypeContext { get; } = [];

    public SatisfiabilityPath Path { get; } = [];

    public SatisfiabilityPath CycleDetectionPath { get; } = [];

    public HashSet<FieldAccessCacheKey> FieldAccessCache { get; } = [];
}
