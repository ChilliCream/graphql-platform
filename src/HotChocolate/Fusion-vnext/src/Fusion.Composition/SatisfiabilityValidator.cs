using System.Collections.Immutable;
using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Results;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion;

internal sealed class SatisfiabilityValidator
    : MutableSchemaDefinitionVisitor<SatisfiabilityValidatorContext>
{
    private readonly MutableSchemaDefinition _schema;
    private readonly ICompositionLog _log;
    private static RequirementsValidator _requirementsValidator = null!;

    public SatisfiabilityValidator(MutableSchemaDefinition schema, ICompositionLog log)
    {
        _schema = schema;
        _log = log;
        _requirementsValidator = new RequirementsValidator(schema);
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

    public override void VisitObjectType(
        MutableObjectTypeDefinition objectType,
        SatisfiabilityValidatorContext context)
    {
        context.TypeContext.Push(objectType);

        foreach (var field in objectType.Fields)
        {
            VisitOutputField(field, context);
        }

        context.TypeContext.Pop();
    }

    public override void VisitOutputField(
        MutableOutputFieldDefinition field,
        SatisfiabilityValidatorContext context)
    {
        var schemaNames = field.GetSchemaNames();
        var type = context.TypeContext.Peek();
        var errors = new List<SatisfiabilityError>();
        var previousSchemaName = context.Path.TryPeek(out var item) ? item.SchemaName : null;
        var optionCount = 0;
        var cycle = false;

        foreach (var schemaName in schemaNames)
        {
            var pathItem = new SatisfiabilityPathItem(field, type, schemaName);

            // Validate that we are not in a cycle.
            if (context.Path.Contains(pathItem))
            {
                cycle = true;
                continue;
            }

            // Validate transition between source schemas.
            if (previousSchemaName is not null && previousSchemaName != schemaName)
            {
                var transitionErrors = ValidateSourceSchemaTransition(
                    type,
                    transitionFromSchemaName: previousSchemaName,
                    transitionToSchemaName: schemaName);

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

                    continue;
                }
            }

            // Validate @require selections.
            var fusionRequiresMap = field.GetFusionRequiresMap(schemaName);
            var success = true;

            foreach (var fieldSelectionMap in fusionRequiresMap)
            {
                var fieldSelectionMapParser = new FieldSelectionMapParser(fieldSelectionMap);
                var selectedValue = fieldSelectionMapParser.Parse();
                var requirementErrors =
                    _requirementsValidator.Validate(
                        selectedValue,
                        type,
                        transitionFromSchemaName: previousSchemaName,
                        excludeSchemaName: schemaName);

                if (requirementErrors.Any())
                {
                    errors.Add(
                        new SatisfiabilityError(
                            string.Format(
                                SatisfiabilityValidator_UnableToSatisfyRequirement,
                                selectedValue.ToString(indented: false),
                                pathItem),
                            requirementErrors));

                    success = false;
                    break;
                }
            }

            context.Path.Push(pathItem);

            if (success)
            {
                optionCount++;
            }

            // TODO: Abstract types.

            // If the field returns a composite type, we need to visit that type.
            if (field.Type.InnerType() is MutableObjectTypeDefinition objectType)
            {
                VisitObjectType(objectType, context);
            }

            context.Path.Pop();
        }

        // Log an error if there are no options for accessing the field, and there is no cycle
        // (which would imply an option for accessing the same field repeatedly).
        // (f.e. relatedProduct.relatedProduct.relatedProduct)
        if (optionCount == 0 && !cycle)
        {
            var error = new SatisfiabilityError(
                string.Format(
                    SatisfiabilityValidator_UnableToAccessFieldOnPath,
                    type.Name,
                    field.Name, context.Path),
                [.. errors]);

            _log.Write(
                new LogEntry(error.ToString(), LogEntryCodes.Unsatisfiable, extension: error));
        }
    }

    private static ImmutableArray<SatisfiabilityError> ValidateSourceSchemaTransition(
        MutableComplexTypeDefinition type,
        string transitionFromSchemaName,
        string transitionToSchemaName)
    {
        var errors = new List<SatisfiabilityError>();

        // Get the list of lookups for the current type in the destination schema.
        var lookupDirectives =
            type.GetFusionLookupDirectives(transitionToSchemaName).ToImmutableArray();

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
            var lookupFieldArg = (string)lookupDirective.Arguments["field"].Value!;
            var lookupMapArg = (ListValueNode)lookupDirective.Arguments["map"];
            var lookupPathArg = (string?)lookupDirective.Arguments["path"].Value;

            var lookupFieldName = ParseFieldDefinition(lookupFieldArg).Name.Value;
            var lookupMap =
                lookupMapArg.Items.Select(i => ((StringValueNode)i).Value).ToImmutableArray();

            var lookupFieldPath =
                lookupPathArg is null
                    ? lookupFieldName
                    : $"{lookupPathArg}.{lookupFieldName}";

            var lookupPath = $"{transitionToSchemaName}:Query.{lookupFieldPath}";

            // Ensure that lookup requirements are satisfied.
            var satisfied = true;

            foreach (var fieldSelectionMap in lookupMap)
            {
                var fieldSelectionMapParser = new FieldSelectionMapParser(fieldSelectionMap);
                var selectedValue = fieldSelectionMapParser.Parse();
                var lookupErrors =
                    _requirementsValidator.Validate(
                        selectedValue,
                        type,
                        transitionFromSchemaName,
                        excludeSchemaName: transitionToSchemaName);

                if (lookupErrors.Any())
                {
                    errors.Add(
                        new SatisfiabilityError(
                            string.Format(
                                SatisfiabilityValidator_UnableToSatisfyRequirementForLookup,
                                selectedValue,
                                lookupPath),
                            lookupErrors));

                    satisfied = false;
                    break;
                }
            }

            // If the requirements for any of the lookups are satisfied, then we can return early.
            if (satisfied)
            {
                break;
            }
        }

        return [.. errors];
    }

    private sealed class RequirementsValidator(MutableSchemaDefinition schema)
        : FieldSelectionMapSyntaxVisitor<RequirementsValidatorContext>(Continue)
    {
        public ImmutableArray<SatisfiabilityError> Validate(
            SelectedValueNode selectedValue,
            MutableComplexTypeDefinition type,
            string? transitionFromSchemaName,
            string? excludeSchemaName = null)
        {
            var context = new RequirementsValidatorContext(
                type,
                transitionFromSchemaName,
                excludeSchemaName);

            Visit(selectedValue, context);

            return [.. context.Errors];
        }

        protected override ISyntaxVisitorAction Enter(
            PathNode node,
            RequirementsValidatorContext context)
        {
            if (node.TypeName is { } typeName)
            {
                if (!schema.Types.TryGetType(typeName.Value, out var concreteType))
                {
                    // The type condition in the path is invalid. The type does not exist.
                    return Break;
                }

                var type = context.TypeContext.Peek();

                if (schema.GetPossibleTypes(type).Contains(concreteType))
                {
                    context.TypeContext.Push(concreteType);
                }
                else
                {
                    // The concrete type is not a possible type of the abstract type.
                    return Break;
                }
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            PathNode node,
            RequirementsValidatorContext context)
        {
            if (node.TypeName is not null)
            {
                context.TypeContext.Pop();
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            PathSegmentNode node,
            RequirementsValidatorContext context)
        {
            if (context.TypeContext.Peek() is MutableComplexTypeDefinition complexType)
            {
                if (!complexType.Fields.TryGetField(node.FieldName.Value, out var field))
                {
                    // The field does not exist on the type.
                    return Break;
                }

                var result = HandleField(field, complexType, context);

                if (result != Continue)
                {
                    return result;
                }

                var fieldType = field.Type.NullableType();

                if (fieldType is MutableComplexTypeDefinition or MutableUnionTypeDefinition)
                {
                    if (node.PathSegment is null)
                    {
                        // The field returns a composite type and must have subselections.
                        return Break;
                    }

                    if (fieldType is MutableUnionTypeDefinition && node.TypeName is null)
                    {
                        // The field returns a union type and must have a type condition.
                        return Break;
                    }

                    context.TypeContext.Push(fieldType.AsTypeDefinition());
                }
                else
                {
                    if (node.PathSegment is null)
                    {
                        context.TypeContext.Push(fieldType.AsTypeDefinition());
                    }
                    else
                    {
                        // The field does not return a composite type and cannot have subselections.
                        return Break;
                    }
                }
            }

            if (node.TypeName is { } typeName)
            {
                if (!schema.Types.TryGetType(typeName.Value, out var concreteType))
                {
                    // The type condition in the path is invalid. The type does not exist.
                    return Break;
                }

                var type = context.TypeContext.Peek();

                if (schema.GetPossibleTypes(type).Contains(concreteType))
                {
                    context.TypeContext.Pop();
                    context.TypeContext.Push(concreteType);
                }
                else
                {
                    // The concrete type is not a possible type of the abstract type.
                    return Break;
                }
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            PathSegmentNode node,
            RequirementsValidatorContext context)
        {
            context.TypeContext.Pop();

            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            SelectedObjectFieldNode node,
            RequirementsValidatorContext context)
        {
            if (node.SelectedValue is null
                && context.TypeContext.Peek() is MutableComplexTypeDefinition complexType)
            {
                if (!complexType.Fields.TryGetField(node.Name.Value, out var field))
                {
                    // The field does not exist on the type.
                    return Skip;
                }

                return HandleField(field, complexType, context);
            }

            return Continue;
        }

        private ISyntaxVisitorAction HandleField(
            MutableOutputFieldDefinition field,
            MutableComplexTypeDefinition type,
            RequirementsValidatorContext context)
        {
            var schemaNames = field.GetSchemaNames();
            var errors = new List<SatisfiabilityError>();

            schemaNames =
                context.ExcludeSchemaName is null
                    ? schemaNames
                    : schemaNames.Remove(context.ExcludeSchemaName);

            var previousSchemaName =
                context.Path.TryPeek(out var item)
                    ? item.SchemaName
                    : context.TransitionFromSchemaName;

            var optionCount = 0;

            foreach (var schemaName in schemaNames)
            {
                var pathItem = new SatisfiabilityPathItem(field, type, schemaName);

                // Validate that we are not in a cycle.
                if (context.Path.Contains(pathItem))
                {
                    errors.Add(
                        new SatisfiabilityError(
                            string.Format(
                                SatisfiabilityValidator_CycleDetected,
                                context.Path,
                                pathItem)));

                    continue;
                }

                context.Path.Push(pathItem);

                // Validate transition between source schemas.
                if (previousSchemaName is not null && previousSchemaName != schemaName)
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
                                    SatisfiabilityValidator_UnableToTransitionBetweenSchemas,
                                    previousSchemaName,
                                    schemaName,
                                    pathItem),
                                transitionErrors));

                        continue;
                    }
                }

                // Validate @require selections.
                var fusionRequiresMap = field.GetFusionRequiresMap(schemaName);
                var success = true;

                foreach (var fieldSelectionMap in fusionRequiresMap)
                {
                    var fieldSelectionMapParser = new FieldSelectionMapParser(fieldSelectionMap);
                    var selectedValue = fieldSelectionMapParser.Parse();

                    Visit(selectedValue, context);

                    if (context.Errors.Count != 0)
                    {
                        errors.Add(
                            new SatisfiabilityError(
                                string.Format(
                                    SatisfiabilityValidator_UnableToSatisfyRequirement,
                                    selectedValue,
                                    pathItem),
                                [.. context.Errors]));

                        context.Errors.Clear();
                        success = false;
                        break;
                    }
                }

                if (success)
                {
                    optionCount++;
                }

                context.Path.Pop();
            }

            if (schemaNames.Length == 0)
            {
                errors.Add(
                    new SatisfiabilityError(SatisfiabilityValidator_NoOtherSchemasContainField));
            }

            if (optionCount == 0)
            {
                context.Errors.Add(
                    new SatisfiabilityError(
                        string.Format(
                            SatisfiabilityValidator_UnableToAccessRequiredField,
                            type.Name,
                            field.Name),
                        [.. errors]));

                return Skip;
            }

            return Continue;
        }

        private ImmutableArray<SatisfiabilityError> ValidateSourceSchemaTransition(
            MutableComplexTypeDefinition type,
            RequirementsValidatorContext context,
            string transitionToSchemaName)
        {
            var errors = new List<SatisfiabilityError>();

            // Get the list of lookups for the current type in the destination schema.
            var lookupDirectives =
                type.GetFusionLookupDirectives(transitionToSchemaName).ToImmutableArray();

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
                var lookupFieldArg = (string)lookupDirective.Arguments["field"].Value!;
                var lookupMapArg = (ListValueNode)lookupDirective.Arguments["map"];
                var lookupPathArg = (string?)lookupDirective.Arguments["path"].Value;

                var lookupFieldName = ParseFieldDefinition(lookupFieldArg).Name.Value;
                var lookupMap =
                    lookupMapArg.Items.Select(i => ((StringValueNode)i).Value).ToImmutableArray();

                var lookupFieldPath =
                    lookupPathArg is null
                        ? lookupFieldName
                        : $"{lookupPathArg}.{lookupFieldName}";

                var lookupPath = $"{transitionToSchemaName}:Query.{lookupFieldPath}";

                // Ensure that lookup requirements are satisfied.
                var satisfied = true;

                foreach (var fieldSelectionMap in lookupMap)
                {
                    var fieldSelectionMapParser = new FieldSelectionMapParser(fieldSelectionMap);
                    var selectedValue = fieldSelectionMapParser.Parse();

                    Visit(selectedValue, context);

                    if (context.Errors.Count != 0)
                    {
                        errors.Add(
                            new SatisfiabilityError(
                                string.Format(
                                    SatisfiabilityValidator_UnableToSatisfyRequirementForLookup,
                                    selectedValue,
                                    lookupPath),
                                [.. context.Errors]));

                        errors.Clear();
                        satisfied = false;
                        break;
                    }
                }

                // If the requirements for any of the lookups are satisfied, then we can return early.
                if (satisfied)
                {
                    break;
                }
            }

            return [.. errors];
        }
    }

    private sealed class RequirementsValidatorContext
    {
        public RequirementsValidatorContext(
            ITypeDefinition type,
            string? transitionFromSchemaName,
            string? excludeSchemaName)
        {
            TypeContext.Push(type);
            TransitionFromSchemaName = transitionFromSchemaName;
            ExcludeSchemaName = excludeSchemaName;
        }

        public Stack<ITypeDefinition> TypeContext { get; } = [];

        public SatisfiabilityPath Path { get; } = [];

        public string? TransitionFromSchemaName { get; }

        public string? ExcludeSchemaName { get; }

        public List<SatisfiabilityError> Errors { get; } = [];
    }
}

internal sealed class SatisfiabilityValidatorContext
{
    public Stack<MutableObjectTypeDefinition> TypeContext { get; } = [];

    public SatisfiabilityPath Path { get; } = [];
}
