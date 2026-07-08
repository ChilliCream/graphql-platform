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
    private readonly SourceSchemaTransitionCache _transitionCache;
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
        _transitionCache = new SourceSchemaTransitionCache();
        _requirementsValidator =
            new RequirementsValidator(schema, _lookupCache, _transitionCache, _options.IncludeSatisfiabilityPaths);
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

        return _log.HasErrors
            ? ErrorHelper.SatisfiabilityValidationFailed()
            : CompositionResult.Success();
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

            // If the field is marked as partial, it must be provided by the current schema for it
            // to be an option.
            if (field.IsPartial(schemaName)
                && path?.Item.Provides(field, type, schemaName, _schema) != true)
            {
                continue;
            }

            var pathItem = new SatisfiabilityPathItem(
                field,
                type,
                schemaName)
            {
                ProvidedSelectionSet =
                    field.GetFusionEventStreamMessage(schemaName) ?? providedSelectionSet
            };

            // Validate that we are not in a cycle by checking if this path item
            // already appears in the ancestor chain.
            if (path.ContainsItem(pathItem))
            {
                cycle = true;
                continue;
            }

            // Validate transition between source schemas.
            if (previousSchemaName is not null && previousSchemaName != schemaName)
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

            // Validate field requirements (@require).
            var requirements = field.GetFusionRequiresRequirements(schemaName);

            if (requirements is not null)
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
            _transitionCache,
            cycleDetectionPath: null,
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
