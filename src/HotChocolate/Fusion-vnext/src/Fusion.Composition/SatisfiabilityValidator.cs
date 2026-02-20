using System.Collections.Immutable;
using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Fusion.Satisfiability;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;
using FieldNames = HotChocolate.Fusion.WellKnownFieldNames;

namespace HotChocolate.Fusion;

internal sealed class SatisfiabilityValidator
{
    private readonly SatisfiabilityOptions _options;
    private readonly RequirementsValidator _requirementsValidator;
    private readonly MutableSchemaDefinition _schema;
    private readonly ICompositionLog _log;

    public SatisfiabilityValidator(
        MutableSchemaDefinition schema,
        ICompositionLog log,
        SatisfiabilityOptions? options = null)
    {
        _schema = schema;
        _log = log;
        _options = options ?? new SatisfiabilityOptions();
        _requirementsValidator = new RequirementsValidator(schema, _options.IncludeSatisfiabilityPaths);
    }

    public CompositionResult Validate()
    {
        var context = new SatisfiabilityValidatorContext();

        MutableObjectTypeDefinition?[] rootTypes =
            [_schema.QueryType, _schema.MutationType, _schema.SubscriptionType];

        foreach (var rootType in rootTypes)
        {
            if (rootType is not null)
            {
                VisitObjectType(rootType, context);
            }
        }

        return _log.HasErrors
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

            // The node and nodes fields are "virtual" fields that might not directly map
            // to an underlying source schema, so we have to validate them differently.
            if (field.Name is FieldNames.Node or FieldNames.Nodes
                && objectType == _schema.QueryType
                && _schema.Types.TryGetType<IInterfaceTypeDefinition>(WellKnownTypeNames.Node, out var nodeType)
                && field.Type.NamedType() == nodeType)
            {
                if (field.Name == FieldNames.Nodes)
                {
                    // The node and nodes fields always appear in pairs, so we can skip nodes entirely
                    // and only do the validation once for the node field.
                    continue;
                }

                VisitNodeField(objectType, field, nodeType, context);

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
                && previousPathItem?.Provides(field, type, schemaName, _schema) != true)
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
                        previousPathItem,
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
                var possibleTypes = fieldType.GetPossibleTypes(schemaName, _schema);

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
            var qualifiedFieldName = $"{type.Name}.{field.Name}";

            if (_options.IgnoredNonAccessibleFields.TryGetValue(qualifiedFieldName, out var ignoredPaths)
                && ignoredPaths.Contains(context.Path.ToString()))
            {
                return;
            }

            var message =
                _options.IncludeSatisfiabilityPaths
                    ? string.Format(
                        SatisfiabilityValidator_UnableToAccessFieldOnPath,
                        type.Name,
                        field.Name,
                        context.Path)
                    : string.Format(
                        SatisfiabilityValidator_UnableToAccessField,
                        type.Name,
                        field.Name);

            var error = new SatisfiabilityError(message, [.. errors]);

            _log.Write(
                LogEntryBuilder.New()
                    .SetMessage(error.ToString())
                    .SetCode(LogEntryCodes.Unsatisfiable)
                    .SetSeverity(LogSeverity.Error)
                    .SetExtension("error", error)
                    .Build());
        }
    }

    private void VisitNodeField(
        MutableObjectTypeDefinition queryType,
        MutableOutputFieldDefinition nodeField,
        IInterfaceTypeDefinition nodeType,
        SatisfiabilityValidatorContext context)
    {
        foreach (var possibleType in _schema.GetPossibleTypes(nodeType))
        {
            var byIdLookups = _schema
                .GetPossibleFusionLookupDirectivesById(possibleType);

            var hasNodeLookup = false;

            var nodePathItem = new SatisfiabilityPathItem(nodeField, queryType, "*");
            context.Path.Push(nodePathItem);

            foreach (var lookup in byIdLookups)
            {
                var schemaName = (string)lookup.Arguments[WellKnownArgumentNames.Schema].Value!;
                var fieldDirectiveArgument = (string)lookup.Arguments[WellKnownArgumentNames.Field].Value!;
                var lookupFieldDefinition = ParseFieldDefinition(fieldDirectiveArgument);
                var lookupFieldTypeName = lookupFieldDefinition.Type.NamedType().Name.Value;

                if (!_schema.Types.TryGetType(lookupFieldTypeName, out var lookupFieldNamedType))
                {
                    continue;
                }

                var lookupFieldType = CreateType(lookupFieldDefinition.Type, lookupFieldNamedType).ExpectOutputType();

                if (lookupFieldType is IInterfaceTypeDefinition { Name: "Node" })
                {
                    hasNodeLookup = true;
                }

                var lookupField = new MutableOutputFieldDefinition(lookupFieldDefinition.Name.Value, lookupFieldType);

                var lookupPathItem = new SatisfiabilityPathItem(lookupField, queryType, schemaName);
                context.Path.Push(lookupPathItem);

                VisitObjectType(possibleType, context);

                context.Path.Pop();
            }

            if (!hasNodeLookup)
            {
                var error = new SatisfiabilityError(
                    string.Format(SatisfiabilityValidator_NodeTypeHasNoNodeLookup, possibleType.Name));

                _log.Write(
                    LogEntryBuilder.New()
                        .SetMessage(error.ToString())
                        .SetCode(LogEntryCodes.Unsatisfiable)
                        .SetSeverity(LogSeverity.Error)
                        .SetExtension("error", error)
                        .Build());
            }

            context.Path.Pop();
        }
    }

    private ImmutableArray<SatisfiabilityError> ValidateSourceSchemaTransition(
        MutableObjectTypeDefinition type,
        SatisfiabilityValidatorContext context,
        string transitionToSchemaName)
    {
        var errors = new List<SatisfiabilityError>();

        var lookupDirectives =
            _schema.GetPossibleFusionLookupDirectives(type, transitionToSchemaName);

        if (!lookupDirectives.Any() && !CanTransitionToSchemaThroughPath(context.Path, transitionToSchemaName))
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
                _schema.GetPossibleFusionLookupDirectives(
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

    private static IType CreateType(ITypeNode typeNode, ITypeDefinition namedType)
    {
        if (typeNode is NonNullTypeNode nonNullType)
        {
            return new NonNullType(CreateType(nonNullType.InnerType(), namedType));
        }

        if (typeNode is ListTypeNode listType)
        {
            return new ListType(CreateType(listType.Type, namedType));
        }

        return namedType;
    }
}

internal sealed class SatisfiabilityValidatorContext
{
    public Stack<MutableObjectTypeDefinition> TypeContext { get; } = [];

    public SatisfiabilityPath Path { get; } = [];

    public SatisfiabilityPath CycleDetectionPath { get; } = [];

    public HashSet<FieldAccessCacheKey> FieldAccessCache { get; } = [];
}
