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

internal sealed partial class SatisfiabilityValidator
{
    private readonly SatisfiabilityOptions _options;
    private readonly ApolloFederationCompatibilityOptions _apolloFederationCompatibility;
    private readonly IReadOnlySet<string> _apolloFederationSchemaNames;
    private readonly NodeResolution _nodeResolution;
    private readonly RequirementsValidator _requirementsValidator;
    private readonly FusionLookupDirectiveCache _lookupCache;
    private readonly SatisfiabilityFacts _facts;
    private readonly MutableSchemaDefinition _schema;
    private readonly ICompositionLog _log;

    public SatisfiabilityValidator(
        MutableSchemaDefinition schema,
        ICompositionLog log,
        SatisfiabilityOptions? options = null,
        NodeResolution nodeResolution = NodeResolution.Gateway,
        ApolloFederationCompatibilityOptions? apolloFederationCompatibility = null,
        IReadOnlySet<string>? apolloFederationSchemaNames = null)
    {
        _schema = schema;
        _log = log;
        _options = options ?? new SatisfiabilityOptions();
        _nodeResolution = nodeResolution;
        _apolloFederationCompatibility =
            apolloFederationCompatibility ?? new ApolloFederationCompatibilityOptions();
        _apolloFederationSchemaNames = apolloFederationSchemaNames ?? new HashSet<string>();
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

        ValidateInterfaceObjectBindings();

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
            if (CanReportAtRuntimeForNonResolvableInterfaceObject(type, field))
            {
                return;
            }

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

    private bool CanReportAtRuntimeForNonResolvableInterfaceObject(
        MutableObjectTypeDefinition objectType,
        MutableOutputFieldDefinition field)
    {
        if (!_apolloFederationCompatibility.AllowNonResolvableInterfaceObjects)
        {
            return false;
        }

        var hasSource = false;

        foreach (var directive in field.Directives.AsEnumerable())
        {
            if (directive.Name != WellKnownDirectiveNames.FusionField
                || directive.Arguments[WellKnownArgumentNames.Schema]
                    is not EnumValueNode { Value: var schemaName })
            {
                continue;
            }

            hasSource = true;

            if (!_apolloFederationSchemaNames.Contains(schemaName)
                || objectType.ExistsInSchema(schemaName)
                || !IsProjectedFromInterfaceObject(objectType, field.Name, schemaName))
            {
                return false;
            }
        }

        return hasSource;
    }

    private static bool IsProjectedFromInterfaceObject(
        MutableObjectTypeDefinition objectType,
        string fieldName,
        string schemaName)
    {
        foreach (var interfaceType in objectType.Implements)
        {
            if (!MutableSchemaDefinitionExtensions
                    .GetInterfaceObjectSchemaNames(interfaceType)
                    .Contains(schemaName, StringComparer.Ordinal)
                || !interfaceType.Fields.TryGetField(fieldName, out var interfaceField)
                || !FieldHasSource(interfaceField, schemaName))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void VisitNodeField(
        MutableObjectTypeDefinition queryType,
        MutableOutputFieldDefinition nodeField,
        IInterfaceTypeDefinition nodeType,
        PathNode? path,
        Queue<WorkItem> worklist)
    {
        if (_nodeResolution is NodeResolution.SourceSchema)
        {
            VisitSourceSchemaNodeField(queryType, nodeField, nodeType, path, worklist);
            return;
        }

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

    private void VisitSourceSchemaNodeField(
        MutableObjectTypeDefinition queryType,
        MutableOutputFieldDefinition nodeField,
        IInterfaceTypeDefinition nodeType,
        PathNode? path,
        Queue<WorkItem> worklist)
    {
        var sourceNodeLookups = new List<(string SchemaName, MutableOutputFieldDefinition Field)>();

        foreach (var lookup in _lookupCache.GetPossibleFusionLookupDirectivesById(
            (MutableComplexTypeDefinition)nodeType))
        {
            var fieldDirectiveArgument = (string)lookup.Arguments[WellKnownArgumentNames.Field].Value!;
            var lookupFieldDefinition = ParseFieldDefinition(fieldDirectiveArgument);

            if (lookupFieldDefinition.Name.Value != WellKnownFieldNames.Node
                || lookupFieldDefinition.Type.NamedType().Name.Value != WellKnownTypeNames.Node
                || lookup.Arguments[WellKnownArgumentNames.Path] is not NullValueNode)
            {
                continue;
            }

            var schemaName = (string)lookup.Arguments[WellKnownArgumentNames.Schema].Value!;
            var lookupField = new MutableOutputFieldDefinition(
                lookupFieldDefinition.Name.Value,
                CreateType(lookupFieldDefinition.Type, nodeType).ExpectOutputType());

            sourceNodeLookups.Add((schemaName, lookupField));
        }

        var possibleTypes = _schema.GetPossibleTypes(nodeType).ToArray();

        if (sourceNodeLookups.Count == 0)
        {
            ReportMissingNodeLookups(possibleTypes);

            return;
        }

        var coveredTypes = new HashSet<MutableObjectTypeDefinition>();
        var nodePathItem = new SatisfiabilityPathItem(nodeField, queryType, "*");
        var nodePath = new PathNode(nodePathItem, path);

        foreach (var (schemaName, lookupField) in sourceNodeLookups)
        {
            var lookupPathItem = new SatisfiabilityPathItem(lookupField, queryType, schemaName);
            var lookupPath = new PathNode(lookupPathItem, nodePath);

            // Only validate the concrete Node implementations declared by this source. In
            // particular, an Apollo interface-object stand-in is not Node lookup coverage.
            foreach (var possibleType in nodeType.GetPossibleTypes(schemaName, _schema))
            {
                coveredTypes.Add(possibleType);
                worklist.Enqueue(new WorkItem(possibleType, lookupPath));
            }
        }

        if (coveredTypes.Count == 0)
        {
            ReportMissingNodeLookups(possibleTypes);
            return;
        }

        foreach (var possibleType in possibleTypes)
        {
            if (!coveredTypes.Contains(possibleType))
            {
                ReportMissingNodeLookup(possibleType, LogSeverity.Warning);
            }
        }
    }

    private void ReportMissingNodeLookups(
        IReadOnlyList<MutableObjectTypeDefinition> possibleTypes)
    {
        if (possibleTypes.Count == 0)
        {
            const string message =
                "No source schema provides a non-internal 'Query.node<Node>' lookup field.";

            _log.Write(
                LogEntryBuilder.New()
                    .SetMessage(message)
                    .SetCode(LogEntryCodes.UnsatisfiableQueryPath)
                    .SetSeverity(LogSeverity.Error)
                    .Build());
            return;
        }

        foreach (var possibleType in possibleTypes)
        {
            ReportMissingNodeLookup(possibleType, LogSeverity.Error);
        }
    }

    private void ReportMissingNodeLookup(
        MutableObjectTypeDefinition possibleType,
        LogSeverity severity)
    {
        var error = new SatisfiabilityError(
            string.Format(SatisfiabilityValidator_NodeTypeHasNoNodeLookup, possibleType.Name));

        _log.Write(
            LogEntryBuilder.New()
                .SetMessage(error.ToString())
                .SetCode(LogEntryCodes.UnsatisfiableQueryPath)
                .SetSeverity(severity)
                .SetExtension("error", error)
                .Build());
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
