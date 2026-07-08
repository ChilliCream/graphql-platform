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

namespace HotChocolate.Fusion;

internal sealed class SatisfiabilityValidator
{
    private readonly SatisfiabilityOptions _options;
    private readonly RequirementsValidator _requirementsValidator;
    private readonly FusionLookupDirectiveCache _lookupCache;
    private readonly SatisfiabilityFacts _facts;
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
        _lookupCache = new FusionLookupDirectiveCache(schema);
        _facts = new SatisfiabilityFactsBuilder(schema, _lookupCache).Build();
        _requirementsValidator =
            new RequirementsValidator(
                schema,
                _lookupCache,
                _facts,
                _options.IncludeSatisfiabilityPaths);
    }

    public CompositionResult Validate()
    {
        var visited = new HashSet<(MutableObjectTypeDefinition, string?)>();
        var worklist = new Queue<WorkItem>();

        MutableObjectTypeDefinition?[] rootTypes =
            [_schema.QueryType, _schema.MutationType, _schema.SubscriptionType];

        foreach (var rootType in rootTypes)
        {
            if (rootType is not null)
            {
                worklist.Enqueue(new WorkItem(rootType, null));
            }
        }

        while (worklist.Count > 0)
        {
            var work = worklist.Dequeue();
            var prevSchema = work.Path?.Item.SchemaName;

            if (!visited.Add((work.ObjectType, prevSchema)))
            {
                continue;
            }

            VisitObjectType(work.ObjectType, work.Path, worklist, visited);
        }

        ValidateProvidesDeliverability();

        return _log.HasErrors
            ? ErrorHelper.SatisfiabilityValidationFailed()
            : CompositionResult.Success();
    }

    // Every field selected by an @provides must be deliverable by the providing source schema.
    // A field that is @external (partial) on the providing schema is deliverable by construction,
    // that is the very promise of @provides. A field that the providing schema owns, however, must
    // genuinely be resolvable there, including satisfying its own @require. Missing or malformed
    // provided fields are already rejected by the pre-merge shape rules (PROVIDES_INVALID_FIELDS).
    private void ValidateProvidesDeliverability()
    {
        foreach (var type in _schema.Types.OfType<MutableObjectTypeDefinition>())
        {
            foreach (var field in type.Fields)
            {
                foreach (var schemaName in field.GetSchemaNames())
                {
                    var provides = field.GetFusionFieldProvides(schemaName);

                    if (provides is null)
                    {
                        continue;
                    }

                    var providedType = field.Type.AsTypeDefinition();

                    if (providedType.Kind is not TypeKind.Object
                        and not TypeKind.Interface
                        and not TypeKind.Union)
                    {
                        continue;
                    }

                    var selectionSet = ParseSelectionSet($"{{ {provides} }}");

                    foreach (var possibleType in providedType.GetPossibleTypes(schemaName, _schema))
                    {
                        ValidateProvidedSelectionSet(
                            selectionSet,
                            possibleType,
                            schemaName,
                            type,
                            field);
                    }
                }
            }
        }
    }

    private void ValidateProvidedSelectionSet(
        SelectionSetNode selectionSet,
        MutableObjectTypeDefinition currentType,
        string schemaName,
        MutableObjectTypeDefinition providingType,
        MutableOutputFieldDefinition providingField)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode fieldNode:
                    ValidateProvidedField(
                        fieldNode,
                        currentType,
                        schemaName,
                        providingType,
                        providingField);
                    break;

                case InlineFragmentNode { TypeCondition: null } inlineFragment:
                    ValidateProvidedSelectionSet(
                        inlineFragment.SelectionSet,
                        currentType,
                        schemaName,
                        providingType,
                        providingField);
                    break;

                case InlineFragmentNode inlineFragment:
                    if (_schema.Types.TryGetType(inlineFragment.TypeCondition.Name.Value, out var fragmentType))
                    {
                        foreach (var possibleType in fragmentType.GetPossibleTypes(schemaName, _schema))
                        {
                            ValidateProvidedSelectionSet(
                                inlineFragment.SelectionSet,
                                possibleType,
                                schemaName,
                                providingType,
                                providingField);
                        }
                    }

                    break;
            }
        }
    }

    private void ValidateProvidedField(
        FieldNode fieldNode,
        MutableObjectTypeDefinition currentType,
        string schemaName,
        MutableObjectTypeDefinition providingType,
        MutableOutputFieldDefinition providingField)
    {
        if (!currentType.Fields.TryGetField(fieldNode.Name.Value, out var field))
        {
            return;
        }

        // A field that is @external on the providing schema is delivered by the @provides promise
        // itself. Only a field the providing schema owns must be resolvable in its own right, and a
        // plain owned field without an @require is trivially so, so the fact table is consulted only
        // when an @require has to be satisfied.
        if (!field.IsPartial(schemaName)
            && field.GetFusionRequiresRequirements(schemaName) is not null
            && !_facts.IsFieldResolvableOn(currentType, field, schemaName))
        {
            _log.Write(
                LogEntryBuilder.New()
                    .SetMessage(
                        SatisfiabilityValidator_ProvidesFieldNotResolvable,
                        field.Name,
                        providingType.Name,
                        providingField.Name,
                        schemaName)
                    .SetCode(LogEntryCodes.ProvidesFieldsNotResolvable)
                    .SetSeverity(LogSeverity.Error)
                    .Build());

            return;
        }

        if (fieldNode.SelectionSet is null)
        {
            return;
        }

        var fieldType = field.Type.AsTypeDefinition();

        if (fieldType.Kind is not TypeKind.Object and not TypeKind.Interface and not TypeKind.Union)
        {
            return;
        }

        foreach (var possibleType in fieldType.GetPossibleTypes(schemaName, _schema))
        {
            ValidateProvidedSelectionSet(
                fieldNode.SelectionSet,
                possibleType,
                schemaName,
                providingType,
                providingField);
        }
    }

    private void VisitObjectType(
        MutableObjectTypeDefinition objectType,
        PathNode? path,
        Queue<WorkItem> worklist,
        HashSet<(MutableObjectTypeDefinition, string?)> visited)
    {
        foreach (var field in objectType.Fields)
        {
            if (field.HasFusionInaccessibleDirective())
            {
                continue;
            }

            // Fields implemented by the gateway are marked with @fusion__gateway_field and are
            // not resolved from a single source schema, so ordinary source-schema satisfiability
            // does not apply. When such a field returns the Node interface, its resolvability is
            // validated against the per-type node lookups instead.
            if (field.HasFusionGatewayFieldDirective())
            {
                if (field.Type.NamedType() is IInterfaceTypeDefinition { Name: WellKnownTypeNames.Node } nodeType)
                {
                    VisitNodeField(objectType, field, nodeType, path, worklist);
                }

                continue;
            }

            VisitOutputField(field, objectType, path, worklist, visited);
        }
    }

    private void VisitOutputField(
        MutableOutputFieldDefinition field,
        MutableObjectTypeDefinition type,
        PathNode? path,
        Queue<WorkItem> worklist,
        HashSet<(MutableObjectTypeDefinition, string?)> visited)
    {
        var previousSchemaName = path?.Item.SchemaName;
        var schemaNames = field.GetSchemaNames(first: previousSchemaName);
        var eventStreamSchemaNames = field.GetFusionEventStreamSchemaNames();

        if (!eventStreamSchemaNames.IsDefaultOrEmpty)
        {
            schemaNames = previousSchemaName is not null && eventStreamSchemaNames.Contains(previousSchemaName)
                ? eventStreamSchemaNames.Remove(previousSchemaName).Insert(0, previousSchemaName)
                : eventStreamSchemaNames;
        }

        var cycle = false;
        var errors = new List<SatisfiabilityError>();
        var optionCount = 0;
        var fieldType = field.Type.AsTypeDefinition();

        foreach (var schemaName in schemaNames)
        {
            SelectionSetNode? providedSelectionSet = null;

            if (path?.Item.ProvidedSelectionSet is not null
                && previousSchemaName == schemaName
                && !path.Item.TryGetProvidedSelectionSet(
                    field,
                    type,
                    schemaName,
                    _schema,
                    out providedSelectionSet))
            {
                continue;
            }

            // A partial (@external) field is never a resolution candidate in its declaring schema;
            // only an event stream message can make it an option. @provides never does (PR #231).
            if (field.IsPartial(schemaName)
                && path?.Item.ProvidesViaEventStream(field, type, schemaName, _schema) != true)
            {
                continue;
            }

            var eventStreamMessage = field.GetFusionEventStreamMessage(schemaName);
            var pathItem = new SatisfiabilityPathItem(
                field,
                type,
                schemaName)
            {
                ProvidedSelectionSet = eventStreamMessage ?? providedSelectionSet,
                ProvidedByEventStream = eventStreamMessage is not null
            };

            // Validate that we are not in a cycle by checking if this path item
            // already appears in the ancestor chain.
            if (path.ContainsItem(pathItem))
            {
                cycle = true;
                continue;
            }

            // Validate transition between source schemas. The fixpoint answers the direct-lookup
            // route in O(1); only when it cannot confirm the transition do we fall back to the full
            // recursion, which also covers the parent-call and one-to-one routes and builds the error.
            // A provided selection set (event stream message or @provides) narrows the context in a
            // way the fixpoint does not model, so we always defer to the recursion in that case.
            if (previousSchemaName is not null
                && previousSchemaName != schemaName
                && (path?.Item.ProvidedSelectionSet is not null
                    || !_facts.CanTransition(type, schemaName, previousSchemaName)))
            {
                var transitionErrors = ValidateSourceSchemaTransition(type, path, schemaName);

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

            // Validate field requirements (@require). The fixpoint answers whether the requirement
            // holds in O(1); only when it does not, or when a provided selection set narrows the
            // context, do we re-run the recursion to build the error tree.
            var requirements = field.GetFusionRequiresRequirements(schemaName);

            if (requirements is not null
                && (path?.Item.ProvidedSelectionSet is not null
                    || !_facts.IsFieldResolvableOn(type, field, schemaName)))
            {
                var requirementErrors =
                    _requirementsValidator.Validate(
                        requirements,
                        type,
                        path?.Item,
                        excludeSchemaName: schemaName,
                        allowIntermediatesFromExcludedSchema: true);

                if (requirementErrors.Length != 0)
                {
                    errors.Add(
                        new SatisfiabilityError(
                            string.Format(
                                SatisfiabilityValidator_UnableToSatisfyRequirement,
                                requirements.ToString(indented: false),
                                pathItem),
                            requirementErrors));

                    continue;
                }
            }

            optionCount++;

            // Enqueue child types for later processing.
            if (fieldType is MutableComplexTypeDefinition or MutableUnionTypeDefinition)
            {
                var fieldPath = new PathNode(pathItem, path);
                var possibleTypes = fieldType.GetPossibleTypes(schemaName, _schema);

                if (pathItem.ProvidedSelectionSet is { } pathItemProvidedSelectionSet)
                {
                    possibleTypes = FilterPossibleTypesByProvidedSelectionSet(
                        possibleTypes,
                        pathItemProvidedSelectionSet);
                }

                foreach (var possibleType in possibleTypes)
                {
                    if (!visited.Contains((possibleType, schemaName)))
                    {
                        worklist.Enqueue(new WorkItem(possibleType, fieldPath));
                    }
                }
            }

            // When we reach a leaf field, we can break early as we only need a single option.
            if (fieldType.IsLeafType())
            {
                break;
            }
        }

        // Log an error if there are no options for accessing the field, and there is no cycle
        // (which would imply an option for accessing the same field repeatedly).
        // (f.e. relatedProduct.relatedProduct.relatedProduct)
        if (optionCount == 0 && !cycle)
        {
            var qualifiedFieldName = $"{type.Name}.{field.Name}";

            if (_options.IgnoredNonAccessibleFields.TryGetValue(qualifiedFieldName, out var ignoredPaths)
                && ignoredPaths.Contains(path.ToPathString()))
            {
                return;
            }

            var message =
                _options.IncludeSatisfiabilityPaths
                    ? string.Format(
                        SatisfiabilityValidator_UnableToAccessFieldOnPath,
                        type.Name,
                        field.Name,
                        path.ToPathString())
                    : string.Format(
                        SatisfiabilityValidator_UnableToAccessField,
                        type.Name,
                        field.Name);

            var error = new SatisfiabilityError(message, [.. errors]);

            _log.Write(
                LogEntryBuilder.New()
                    .SetMessage(error.ToString())
                    .SetCode(LogEntryCodes.UnsatisfiableQueryPath)
                    .SetSeverity(LogSeverity.Error)
                    .SetExtension("error", error)
                    .Build());
        }
    }

    private void VisitNodeField(
        MutableObjectTypeDefinition queryType,
        MutableOutputFieldDefinition nodeField,
        IInterfaceTypeDefinition nodeType,
        PathNode? path,
        Queue<WorkItem> worklist)
    {
        foreach (var possibleType in _schema.GetPossibleTypes(nodeType))
        {
            var byIdLookups = _lookupCache.GetPossibleFusionLookupDirectivesById(possibleType);

            var hasNodeLookup = false;

            var nodePathItem = new SatisfiabilityPathItem(nodeField, queryType, "*");
            var nodePath = new PathNode(nodePathItem, path);

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
                var lookupPath = new PathNode(lookupPathItem, nodePath);

                worklist.Enqueue(new WorkItem(possibleType, lookupPath));
            }

            if (!hasNodeLookup)
            {
                var error = new SatisfiabilityError(
                    string.Format(SatisfiabilityValidator_NodeTypeHasNoNodeLookup, possibleType.Name));

                _log.Write(
                    LogEntryBuilder.New()
                        .SetMessage(error.ToString())
                        .SetCode(LogEntryCodes.UnsatisfiableQueryPath)
                        .SetSeverity(LogSeverity.Error)
                        .SetExtension("error", error)
                        .Build());
            }
        }
    }

    private ImmutableArray<SatisfiabilityError> ValidateSourceSchemaTransition(
        MutableObjectTypeDefinition type,
        PathNode? path,
        string transitionToSchemaName)
    {
        return SourceSchemaTransitionHelper.ValidateSourceSchemaTransition(
            _lookupCache,
            type,
            transitionToSchemaName,
            [.. path.EnumerateFromLeaf()],
            (contextType, parentPathItem, lookupRequirements) =>
                _requirementsValidator.Validate(
                    lookupRequirements,
                    contextType,
                    parentPathItem,
                    excludeSchemaName: transitionToSchemaName),
            SatisfiabilityValidator_NoLookupsFoundForType,
            SatisfiabilityValidator_UnableToSatisfyRequirementForLookup);
    }

    private IEnumerable<MutableObjectTypeDefinition> FilterPossibleTypesByProvidedSelectionSet(
        IEnumerable<MutableObjectTypeDefinition> possibleTypes,
        SelectionSetNode selectionSet)
    {
        HashSet<string>? providedTypeNames = null;

        CollectRootTypeConditions(selectionSet, ref providedTypeNames);

        if (providedTypeNames is null)
        {
            return possibleTypes;
        }

        return possibleTypes.Where(t => providedTypeNames.Contains(t.Name));
    }

    private void CollectRootTypeConditions(
        SelectionSetNode selectionSet,
        ref HashSet<string>? providedTypeNames)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is not InlineFragmentNode inlineFragment)
            {
                continue;
            }

            if (inlineFragment.TypeCondition is { } typeCondition)
            {
                if (!_schema.Types.TryGetType(typeCondition.Name.Value, out var type))
                {
                    continue;
                }

                providedTypeNames ??= [];

                foreach (var possibleType in _schema.GetPossibleTypes(type))
                {
                    providedTypeNames.Add(possibleType.Name);
                }
            }
            else
            {
                CollectRootTypeConditions(inlineFragment.SelectionSet, ref providedTypeNames);
            }
        }
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
