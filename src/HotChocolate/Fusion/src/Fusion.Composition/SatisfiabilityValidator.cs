using System.Collections.Immutable;
using System.Diagnostics;
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
    private readonly Dictionary<(ITypeDefinition, string), MutableObjectTypeDefinition[]> _possibleTypes = [];
    private readonly HashSet<(MutableObjectTypeDefinition, MutableOutputFieldDefinition, string?, bool)>
        _dispatchedNodeFields = [];
    private readonly HashSet<(MutableObjectTypeDefinition, MutableOutputFieldDefinition, string?)>
        _reportedFields = [];

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
        var enqueued = new HashSet<VisitKey>();
        var worklist = new Queue<WorkItem>();
        _dispatchedNodeFields.Clear();
        _reportedFields.Clear();
        InitializeViewerFallback();

        MutableObjectTypeDefinition?[] rootTypes =
            [_schema.QueryType, _schema.MutationType, _schema.SubscriptionType];

        foreach (var rootType in rootTypes)
        {
            if (rootType is not null)
            {
                Enqueue(new WorkItem(CreateRootVisitKey(rootType), null), worklist, enqueued);
            }
        }

        while (worklist.Count > 0)
        {
            var work = worklist.Dequeue();
            VisitObjectType(work, worklist, enqueued);
        }

        ValidateInterfaceObjectBindings();

        return _log.HasErrors
            ? ErrorHelper.SatisfiabilityValidationFailed()
            : CompositionResult.Success();
    }

    /// <summary>
    /// Enqueues <paramref name="work"/> unless a work item with an equal key was enqueued before.
    /// </summary>
    private static void Enqueue(in WorkItem work, Queue<WorkItem> worklist, HashSet<VisitKey> enqueued)
    {
        if (enqueued.Add(work.Key))
        {
            worklist.Enqueue(work);
        }
    }

    private void VisitObjectType(
        in WorkItem work,
        Queue<WorkItem> worklist,
        HashSet<VisitKey> enqueued)
    {
        var objectType = work.Key.ObjectType;
        bool? canReenterWithLookup = null;

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
                    VisitNodeField(objectType, field, nodeType, work, worklist, enqueued);
                }

                continue;
            }

            VisitOutputField(field, work, ref canReenterWithLookup, worklist, enqueued);
        }
    }

    /// <summary>
    /// Validates the access of <paramref name="field"/> on the object of <paramref name="work"/>.
    /// <paramref name="canReenterWithLookup"/> caches, for the visit, whether the object can be
    /// re-entered in its schema through a lookup, or is <c>null</c> until first needed.
    /// </summary>
    private void VisitOutputField(
        MutableOutputFieldDefinition field,
        in WorkItem work,
        ref bool? canReenterWithLookup,
        Queue<WorkItem> worklist,
        HashSet<VisitKey> enqueued)
    {
        var type = work.Key.ObjectType;
        var path = work.Path;
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

            // A field the provided selection set omits is resolved by re-entering the schema
            // through a lookup whose key the provided data satisfies.
            if (path?.Item.ProvidedSelectionSet is not null
                && previousSchemaName == schemaName
                && !path.Item.TryGetProvidedSelectionSet(
                    field,
                    type,
                    schemaName,
                    _schema,
                    out providedSelectionSet)
                && !(canReenterWithLookup ??=
                    HasSatisfiableLookup(type, schemaName, path.Item, excludeSchemaName: null)))
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

            // A field that returns this object type and repeats the field holding this object
            // (relatedProduct.relatedProduct) is an option without being visited again.
            if (path is not null
                && path.Item.ProvidedSelectionSet is null
                && fieldType is MutableObjectTypeDefinition
                && path.Item == pathItem)
            {
                cycle = true;
                continue;
            }

            // The fixpoint confirms direct lookups in O(1); otherwise the reach of the visit key
            // holds the outcome of every transition route, and the path is only walked to explain
            // a failure.
            if (previousSchemaName is not null
                && previousSchemaName != schemaName
                && (path?.Item.ProvidedSelectionSet is not null
                    || !_facts.CanTransition(type, schemaName, previousSchemaName)))
            {
                var canTransition = _schemaSets.Contains(work.Key.Reach, schemaName);

                Debug.Assert(
                    canTransition == ValidateSourceSchemaTransition(type, path, schemaName).IsEmpty,
                    "The reach of the visit key disagrees with the source schema transition helper.");

                if (!canTransition)
                {
                    errors.Add(
                        new SatisfiabilityError(
                            string.Format(
                                SatisfiabilityValidator_UnableToTransitionBetweenSchemas,
                                previousSchemaName,
                                schemaName,
                                pathItem),
                            ValidateSourceSchemaTransition(type, path, schemaName)));

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
                // Requirement data read along the path is resolved against the whole path above
                // the holding item.
                IReadOnlyList<SatisfiabilityPathItem> ancestors =
                    path?.Parent is { } parentPath ? [.. parentPath.EnumerateFromLeaf()] : [];

                var requirementErrors =
                    _requirementsValidator.Validate(
                        requirements,
                        type,
                        path?.Item,
                        ancestors,
                        excludeSchemaName: schemaName,
                        allowNestedFieldsFromExcludedSchema: true);

                // The requirement data is fetched from other schemas, so the schema must be able
                // to resolve the field afterwards, either without its own data or by re-entering
                // the type through a lookup.
                if (requirementErrors.IsEmpty)
                {
                    requirementErrors =
                        _requirementsValidator.ValidateReentry(
                            requirements,
                            type,
                            field,
                            schemaName,
                            path?.Item,
                            ancestors,
                            SatisfiabilityValidator_NoLookupForRequiringField,
                            SatisfiabilityValidator_UnableToSatisfyRequirementForLookup);
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

                    continue;
                }
            }

            optionCount++;

            // Enqueue child types for later processing.
            if (fieldType is MutableComplexTypeDefinition or MutableUnionTypeDefinition)
            {
                var fieldPath = new PathNode(pathItem, path);
                var possibleTypes = GetPossibleTypes(fieldType, schemaName);

                if (pathItem.ProvidedSelectionSet is { } pathItemProvidedSelectionSet)
                {
                    possibleTypes = FilterPossibleTypesByProvidedSelectionSet(
                        possibleTypes,
                        pathItemProvidedSelectionSet);
                }

                foreach (var possibleType in possibleTypes)
                {
                    Enqueue(
                        new WorkItem(CreateVisitKey(possibleType, pathItem, work), fieldPath),
                        worklist,
                        enqueued);
                }
            }

            // When we reach a leaf field, we can break early as we only need a single option.
            if (fieldType.IsLeafType())
            {
                break;
            }
        }

        // Log an error if there are no options for accessing the field and the field does not
        // repeat the field holding this object (f.e. relatedProduct.relatedProduct). The error is
        // logged once for each field and schema holding the object.
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

            if (!_reportedFields.Add((type, field, previousSchemaName)))
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
        in WorkItem work,
        Queue<WorkItem> worklist,
        HashSet<VisitKey> enqueued)
    {
        // Node lookups depend on the path only through the schema holding the query type and the
        // viewer fallback, so they are dispatched once for each combination.
        if (!_dispatchedNodeFields.Add((queryType, nodeField, work.Key.SchemaName, work.Key.ViewerFallback)))
        {
            return;
        }

        if (_nodeResolution is NodeResolution.SourceSchema)
        {
            VisitSourceSchemaNodeField(queryType, nodeField, nodeType, work, worklist, enqueued);
            return;
        }

        var path = work.Path;

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

                Enqueue(
                    new WorkItem(CreateNodeVisitKey(possibleType, lookupPathItem, work), lookupPath),
                    worklist,
                    enqueued);
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
        in WorkItem work,
        Queue<WorkItem> worklist,
        HashSet<VisitKey> enqueued)
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
        var nodePath = new PathNode(nodePathItem, work.Path);

        foreach (var (schemaName, lookupField) in sourceNodeLookups)
        {
            var lookupPathItem = new SatisfiabilityPathItem(lookupField, queryType, schemaName);
            var lookupPath = new PathNode(lookupPathItem, nodePath);

            // Only validate the concrete Node implementations declared by this source. In
            // particular, an Apollo interface-object stand-in is not Node lookup coverage.
            foreach (var possibleType in nodeType.GetPossibleTypes(schemaName, _schema))
            {
                coveredTypes.Add(possibleType);
                Enqueue(
                    new WorkItem(CreateNodeVisitKey(possibleType, lookupPathItem, work), lookupPath),
                    worklist,
                    enqueued);
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
        SatisfiabilityPathItem[] pathFromLeaf = [.. path.EnumerateFromLeaf()];

        return SourceSchemaTransitionHelper.ValidateSourceSchemaTransition(
            _schema,
            _lookupCache,
            type,
            transitionToSchemaName,
            pathFromLeaf,
            (contextType, pathFromParent, lookupRequirements) =>
                _requirementsValidator.Validate(
                    lookupRequirements,
                    contextType,
                    pathFromParent.Count > 0 ? pathFromParent[0] : null,
                    ancestors: [],
                    excludeSchemaName: transitionToSchemaName),
            SatisfiabilityValidator_NoLookupsFoundForType,
            SatisfiabilityValidator_UnableToSatisfyRequirementForLookup,
            GetQueryViewerField(pathFromLeaf));
    }

    /// <summary>
    /// Gets the object types that <paramref name="type"/> can resolve to in <paramref name="schemaName"/>.
    /// </summary>
    private MutableObjectTypeDefinition[] GetPossibleTypes(ITypeDefinition type, string schemaName)
    {
        if (!_possibleTypes.TryGetValue((type, schemaName), out var possibleTypes))
        {
            possibleTypes = type is MutableObjectTypeDefinition objectType
                ? [objectType]
                : [.. type.GetPossibleTypes(schemaName, _schema)];
            _possibleTypes.Add((type, schemaName), possibleTypes);
        }

        return possibleTypes;
    }

    private MutableObjectTypeDefinition[] FilterPossibleTypesByProvidedSelectionSet(
        MutableObjectTypeDefinition[] possibleTypes,
        SelectionSetNode selectionSet)
    {
        HashSet<string>? providedTypeNames = null;

        CollectRootTypeConditions(selectionSet, ref providedTypeNames);

        if (providedTypeNames is null)
        {
            return possibleTypes;
        }

        return [.. possibleTypes.Where(t => providedTypeNames.Contains(t.Name))];
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
