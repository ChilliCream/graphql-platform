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
    /// <summary>
    /// The number of entries after which a transition or requirement explanation is cut.
    /// </summary>
    private const int MaxExplanationEntries = 1000;

    private readonly SatisfiabilityOptions _options;
    private readonly ApolloFederationCompatibilityOptions _apolloFederationCompatibility;
    private readonly IReadOnlySet<string> _apolloFederationSchemaNames;
    private readonly NodeResolution _nodeResolution;
    private readonly RequirementsValidator _requirementsValidator;
    private readonly FusionLookupDirectiveCache _lookupCache;
    private readonly SatisfiabilityFacts _facts;
    private readonly MutableSchemaDefinition _schema;
    private readonly ICompositionLog _log;
    private readonly FieldOptionTable _fieldOptions;
    private readonly HashSet<(MutableObjectTypeDefinition, MutableOutputFieldDefinition, string?, bool)>
        _dispatchedNodeFields = [];
    private readonly HashSet<(MutableObjectTypeDefinition, MutableOutputFieldDefinition, string?)>
        _reportedFields = [];
    private readonly SatisfiabilityExplanationBudget _decisionBudget =
        SatisfiabilityExplanationBudget.DecisionOnly(maxEntries: null);
    private readonly Dictionary<string, ImmutableArray<SatisfiabilityError>> _transitionExplanations = [];
    private long _explanationEntryCount;

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
        _schemaSets = new SchemaSetTable(GetSourceSchemaNames(schema));
        _fieldOptions = new FieldOptionTable(schema, _schemaSets);
        _lookupCache = new FusionLookupDirectiveCache(schema);
        _facts = new SatisfiabilityFactsBuilder(schema, _lookupCache).Build();
        _queryViewerField = GetQueryViewerField(schema);
        _queryViewerSchemas = _queryViewerField is null
            ? []
            : [.. _queryViewerField.GetSchemaNames().Select(_schemaSets.GetIndex)];
        _requirementsValidator =
            new RequirementsValidator(
                schema,
                _lookupCache,
                _facts,
                _queryViewerField,
                _options.IncludeSatisfiabilityPaths);
    }

    /// <summary>
    /// Gets the number of explanation entries that the source schema transition and requirement
    /// explanations created.
    /// </summary>
    internal long ExplanationEntryCount => _explanationEntryCount;

    public CompositionResult Validate()
    {
        var enqueued = new HashSet<VisitKey>();
        var worklist = new Queue<WorkItem>();
        _dispatchedNodeFields.Clear();
        _reportedFields.Clear();
        InitializeViewerFallback();
        InitializeExclusions();

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
        _transitionExplanations.Clear();

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

            VisitOutputField(field, work, worklist, enqueued);
        }
    }

    /// <summary>
    /// Validates the access of <paramref name="field"/> on the object of <paramref name="work"/>.
    /// </summary>
    private void VisitOutputField(
        MutableOutputFieldDefinition field,
        in WorkItem work,
        Queue<WorkItem> worklist,
        HashSet<VisitKey> enqueued)
    {
        var type = work.Key.ObjectType;
        var path = work.Path;
        var previousSchemaName = path?.Item.SchemaName;
        var cycle = false;
        var optionCount = 0;
        var options = _fieldOptions.GetOptions(field);

        foreach (var option in options.StartingWith(previousSchemaName))
        {
            var pathItem = TryResolveField(options, option, work, ref cycle, errors: null);

            if (pathItem is null)
            {
                continue;
            }

            optionCount++;

            // Enqueue child types for later processing.
            if (options.ReturnsCompositeType)
            {
                var fieldPath = new PathNode(pathItem, path);
                var possibleTypes = _fieldOptions.GetPossibleTypes(options, option);

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
            if (options.ReturnsLeafType)
            {
                break;
            }
        }

        // Log an error if there are no options for accessing the field and the field does not
        // repeat the field holding this object (f.e. relatedProduct.relatedProduct). The error is
        // logged once for each field and schema holding the object.
        if (optionCount == 0 && !cycle)
        {
            if (IsExemptFromReporting(type, field, path))
            {
                return;
            }

            if (!_reportedFields.Add((type, field, previousSchemaName)))
            {
                return;
            }

            // The reasons are only collected for a reported field.
            var errors = new List<SatisfiabilityError>();

            foreach (var option in options.StartingWith(previousSchemaName))
            {
                TryResolveField(options, option, work, ref cycle, errors);
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

    /// <summary>
    /// Gets the path item that accesses the field of <paramref name="options"/> on the object of
    /// <paramref name="work"/> through <paramref name="option"/>, or <c>null</c> when the option does
    /// not apply. <paramref name="cycle"/> is set when the access repeats the field holding the
    /// object, and the reason the option does not apply is added to <paramref name="errors"/> unless
    /// it is <c>null</c>.
    /// </summary>
    private SatisfiabilityPathItem? TryResolveField(
        FieldOptions options,
        FieldOption option,
        in WorkItem work,
        ref bool cycle,
        List<SatisfiabilityError>? errors)
    {
        var field = options.Field;
        var schemaName = option.SchemaName;
        var type = work.Key.ObjectType;
        var path = work.Path;
        var previousSchemaName = path?.Item.SchemaName;
        SelectionSetNode? providedSelectionSet = null;

        // A field the provided selection set omits is resolved by a new call into the holding schema,
        // through a lookup whose key is resolvable at the object or above it.
        if (path?.Item.ProvidedSelectionSet is not null
            && previousSchemaName == schemaName
            && !path.Item.TryGetProvidedSelectionSet(
                field,
                type,
                schemaName,
                _schema,
                out providedSelectionSet))
        {
            Debug.Assert(
                AgreesWithWalk(
                    work.Key.HolderReentry,
                    budget => CanReenterHolderOnPath(type, path, budget)),
                "The visit key disagrees with the re-entry of the holding schema along the path.");

            if (!work.Key.HolderReentry)
            {
                return null;
            }
        }

        // A partial (@external) field is never a resolution candidate in its declaring schema;
        // only an event stream message can make it an option. @provides never does (PR #231).
        if (option.IsPartial
            && path?.Item.ProvidesViaEventStream(field, type, schemaName, _schema) != true)
        {
            return null;
        }

        var eventStreamMessage = option.EventStreamMessage;
        var pathItem = new SatisfiabilityPathItem(
            field,
            type,
            schemaName)
        {
            ProvidedSelectionSet = eventStreamMessage ?? providedSelectionSet,
            ProvidedByEventStream = eventStreamMessage is not null
        };

        // A field that returns this object type and repeats the field holding this object
        // (relatedProduct.relatedProduct) is an option without being visited again. This check and the
        // ignore lists are the only reads of the path that the visit key does not identify.
        if (path is not null
            && path.Item.ProvidedSelectionSet is null
            && options.FieldType is MutableObjectTypeDefinition
            && path.Item == pathItem)
        {
            cycle = true;
            return null;
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
                AgreesWithWalk(
                    canTransition,
                    budget => ValidateSourceSchemaTransition(
                        type,
                        path,
                        schemaName,
                        budget,
                        transitionFails: false,
                        reroutesViewerFields: true).IsEmpty),
                "The reach of the visit key disagrees with the source schema transition helper.");

            if (!canTransition)
            {
                errors?.Add(
                    new SatisfiabilityError(
                        string.Format(
                            SatisfiabilityValidator_UnableToTransitionBetweenSchemas,
                            previousSchemaName,
                            schemaName,
                            pathItem),
                        GetTransitionExplanation(type, path, schemaName)));

                return null;
            }
        }

        // Validate field requirements (@require). The fixpoint answers whether the requirement
        // holds in O(1); only when it does not, or when a provided selection set narrows the
        // context, does the requirement walk run, anchored at the visit key, and the path is only
        // walked to explain a failure.
        var requirements = option.Requirements;

        if (requirements is not null
            && (path?.Item.ProvidedSelectionSet is not null
                || !_facts.IsFieldResolvableOn(type, field, schemaName)))
        {
            var isSatisfiable =
                ValidateRequirements(requirements, field, schemaName, work, anchored: true, _decisionBudget).IsEmpty;

            var arrival = work;

            Debug.Assert(
                AgreesWithWalk(
                    isSatisfiable,
                    budget => ValidateRequirements(
                        requirements,
                        field,
                        schemaName,
                        arrival,
                        anchored: false,
                        budget).IsEmpty),
                "The visit key disagrees with the requirement walk along the path.");

            if (!isSatisfiable)
            {
                errors?.Add(
                    new SatisfiabilityError(
                        string.Format(
                            SatisfiabilityValidator_UnableToSatisfyRequirement,
                            requirements.ToString(indented: false),
                            pathItem),
                        GetRequirementExplanation(requirements, field, schemaName, work)));

                return null;
            }
        }

        return pathItem;
    }

    /// <summary>
    /// Determines whether a field without an option is not reported as an error, because it is
    /// reported at runtime or is ignored on <paramref name="path"/>.
    /// </summary>
    private bool IsExemptFromReporting(
        MutableObjectTypeDefinition type,
        MutableOutputFieldDefinition field,
        PathNode? path)
    {
        if (CanReportAtRuntimeForNonResolvableInterfaceObject(type, field))
        {
            return true;
        }

        // The ignore lists read the path beyond the visit key.
        var qualifiedFieldName = $"{type.Name}.{field.Name}";

        return _options.IgnoredNonAccessibleFields.TryGetValue(qualifiedFieldName, out var ignoredPaths)
            && ignoredPaths.Contains(path.ToPathString());
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

        // Gateway node resolution validates each possible type through every by-id lookup that
        // returns it, also when another by-id lookup resolves every field of the type.
        var path = work.Path;

        foreach (var possibleType in _schema.GetPossibleTypes(nodeType))
        {
            var byIdLookups = _lookupCache.GetPossibleFusionLookupDirectivesById(possibleType);

            var hasNodeLookup = false;

            var nodePathItem =
                new SatisfiabilityPathItem(nodeField, queryType, SatisfiabilityPathItem.GatewaySchemaName);
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
        var nodePathItem =
            new SatisfiabilityPathItem(nodeField, queryType, SatisfiabilityPathItem.GatewaySchemaName);
        var nodePath = new PathNode(nodePathItem, work.Path);
        var entries = new List<WorkItem>();

        foreach (var (schemaName, lookupField) in sourceNodeLookups)
        {
            var lookupPathItem = new SatisfiabilityPathItem(lookupField, queryType, schemaName);
            var lookupPath = new PathNode(lookupPathItem, nodePath);

            // Only validate the concrete Node implementations declared by this source. In
            // particular, an Apollo interface-object stand-in is not Node lookup coverage.
            foreach (var possibleType in nodeType.GetPossibleTypes(schemaName, _schema))
            {
                coveredTypes.Add(possibleType);
                entries.Add(new WorkItem(CreateNodeVisitKey(possibleType, lookupPathItem, work), lookupPath));
            }
        }

        foreach (var entry in SelectSourceSchemaNodeEntries((MutableInterfaceTypeDefinition)nodeType, entries))
        {
            Enqueue(entry, worklist, enqueued);
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

    /// <summary>
    /// Selects the node lookup entries of <paramref name="entries"/> to validate. Unless
    /// <paramref name="nodeType"/> has a uniformly owned field, a possible type that an entry fully
    /// resolves is validated only through the entries that fully resolve it.
    /// </summary>
    private List<WorkItem> SelectSourceSchemaNodeEntries(
        MutableInterfaceTypeDefinition nodeType,
        List<WorkItem> entries)
    {
        if (HasUniformlyOwnedField(nodeType))
        {
            return entries;
        }

        var entryCounts = new Dictionary<MutableObjectTypeDefinition, int>();

        foreach (var entry in entries)
        {
            entryCounts[entry.Key.ObjectType] = entryCounts.GetValueOrDefault(entry.Key.ObjectType) + 1;
        }

        var resolvesAllFields = new bool[entries.Count];
        var fullyResolvedTypes = new HashSet<MutableObjectTypeDefinition>();

        for (var i = 0; i < entries.Count; i++)
        {
            var type = entries[i].Key.ObjectType;

            if (entryCounts[type] > 1 && CanResolveAllFields(entries[i]))
            {
                resolvesAllFields[i] = true;
                fullyResolvedTypes.Add(type);
            }
        }

        var selected = new List<WorkItem>(entries.Count);

        for (var i = 0; i < entries.Count; i++)
        {
            if (resolvesAllFields[i] || !fullyResolvedTypes.Contains(entries[i].Key.ObjectType))
            {
                selected.Add(entries[i]);
            }
        }

        return selected;
    }

    /// <summary>
    /// Determines whether <paramref name="interfaceType"/> has an accessible field that every
    /// schema declaring it on the interface also owns on each object type implementing the
    /// interface there.
    /// </summary>
    private bool HasUniformlyOwnedField(MutableInterfaceTypeDefinition interfaceType)
    {
        foreach (var interfaceField in interfaceType.Fields)
        {
            if (!interfaceField.HasFusionInaccessibleDirective()
                && !IsOwnershipDivergent(interfaceType, interfaceField))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether a schema declares <paramref name="interfaceField"/> on
    /// <paramref name="interfaceType"/> without owning it on an object type that implements the
    /// interface in that schema.
    /// </summary>
    private bool IsOwnershipDivergent(
        MutableInterfaceTypeDefinition interfaceType,
        MutableOutputFieldDefinition interfaceField)
    {
        foreach (var schemaName in interfaceField.GetSchemaNames())
        {
            if (interfaceField.IsPartial(schemaName)
                || interfaceField.GetFusionRequiresRequirements(schemaName) is not null)
            {
                continue;
            }

            foreach (var possibleType in interfaceType.GetPossibleTypes(schemaName, _schema))
            {
                if (!possibleType.Fields.TryGetField(interfaceField.Name, out var field)
                    || !field.ExistsInSchema(schemaName)
                    || field.IsPartial(schemaName))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether the visit of <paramref name="work"/> finds an option for every field of
    /// its object type.
    /// </summary>
    private bool CanResolveAllFields(in WorkItem work)
    {
        var type = work.Key.ObjectType;
        var previousSchemaName = work.Path?.Item.SchemaName;

        foreach (var field in type.Fields)
        {
            if (field.HasFusionInaccessibleDirective() || field.HasFusionGatewayFieldDirective())
            {
                continue;
            }

            var resolved = false;
            var cycle = false;
            var options = _fieldOptions.GetOptions(field);

            foreach (var option in options.StartingWith(previousSchemaName))
            {
                if (TryResolveField(options, option, work, ref cycle, errors: null) is not null)
                {
                    resolved = true;
                    break;
                }
            }

            if (!resolved && !cycle && !IsExemptFromReporting(type, field, work.Path))
            {
                return false;
            }
        }

        return true;
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

    /// <summary>
    /// Gets the explanation of why the object of the visited work item, held on
    /// <paramref name="path"/>, cannot be held in <paramref name="schemaName"/>. The explanation is
    /// computed once for each schema in a visit and is cut when it exceeds its entry budget.
    /// </summary>
    private ImmutableArray<SatisfiabilityError> GetTransitionExplanation(
        MutableObjectTypeDefinition type,
        PathNode? path,
        string schemaName)
    {
        if (!_transitionExplanations.TryGetValue(schemaName, out var explanation))
        {
            // The reach decides the transition, so the budget stops the search.
            var budget = SatisfiabilityExplanationBudget.Limited(MaxExplanationEntries, stopsSearch: true);
            explanation = ValidateSourceSchemaTransition(
                type,
                path,
                schemaName,
                budget,
                transitionFails: true,
                reroutesViewerFields: true);
            _explanationEntryCount += budget.EntryCount;
            _transitionExplanations.Add(schemaName, explanation);
        }

        return explanation;
    }

    /// <summary>
    /// Gets the explanation of why <paramref name="schemaName"/> cannot resolve <paramref name="field"/>
    /// on the object of <paramref name="work"/> with its <paramref name="requirements"/>. Past its entry
    /// budget, the explanation only keeps the entries that hold entries created before.
    /// </summary>
    private ImmutableArray<SatisfiabilityError> GetRequirementExplanation(
        SelectionSetNode requirements,
        MutableOutputFieldDefinition field,
        string schemaName,
        in WorkItem work)
    {
        // The search walks the whole path, and the budget lets it finish.
        var budget = SatisfiabilityExplanationBudget.Limited(MaxExplanationEntries, stopsSearch: false);
        var explanation = ValidateRequirements(requirements, field, schemaName, work, anchored: false, budget);
        _explanationEntryCount += budget.EntryCount;

        // A walk of the path that finds no failure is explained by the walk anchored at the visit key.
        if (explanation.IsEmpty)
        {
            budget = SatisfiabilityExplanationBudget.Limited(MaxExplanationEntries, stopsSearch: false);
            explanation = ValidateRequirements(requirements, field, schemaName, work, anchored: true, budget);
            _explanationEntryCount += budget.EntryCount;
        }

        return explanation;
    }

    /// <summary>
    /// Validates that <paramref name="schemaName"/> can resolve <paramref name="field"/> on the object of
    /// <paramref name="work"/> with its <paramref name="requirements"/>, and returns the errors, which are
    /// empty when it can. An <paramref name="anchored"/> walk reads only the visit key; otherwise the walk
    /// reads the whole path.
    /// </summary>
    private ImmutableArray<SatisfiabilityError> ValidateRequirements(
        SelectionSetNode requirements,
        MutableOutputFieldDefinition field,
        string schemaName,
        in WorkItem work,
        bool anchored,
        SatisfiabilityExplanationBudget budget)
    {
        var type = work.Key.ObjectType;
        var path = work.Path;
        var scope = RequirementScope.Unanchored;
        IReadOnlyList<SatisfiabilityPathItem> ancestors = [];

        if (path is not null)
        {
            if (anchored)
            {
                scope = new RequirementScope(
                    new VisitAnchor(this, work.Key, path.Item),
                    ArrivalPath: null,
                    AllowsReRootedCalls: true);
            }
            else
            {
                SatisfiabilityPathItem[] arrivalPath = [.. path.EnumerateFromLeaf()];
                scope = new RequirementScope(Anchor: null, arrivalPath, AllowsReRootedCalls: true);
                ancestors = arrivalPath[1..];
            }
        }

        var errors =
            _requirementsValidator.Validate(
                requirements,
                type,
                path?.Item,
                ancestors,
                scope,
                excludeSchemaName: schemaName,
                budget,
                allowNestedFieldsFromExcludedSchema: true);

        // The schema must resolve the field after the requirement data is fetched from other schemas,
        // either without its own data or by re-entering the type through a lookup.
        if (errors.IsEmpty)
        {
            var holdingCallIsReRooted =
                path is not null
                && (anchored
                    ? _schemaSets.Contains(work.Key.ViewerOnly, schemaName)
                    : IsViewerOnlyOnPath(type, path, schemaName));

            errors =
                _requirementsValidator.ValidateReentry(
                    requirements,
                    type,
                    field,
                    schemaName,
                    path?.Item,
                    ancestors,
                    scope,
                    SatisfiabilityValidator_NoLookupForRequiringField,
                    SatisfiabilityValidator_UnableToSatisfyRequirementForLookup,
                    budget,
                    holdingCallIsReRooted);
        }

        return errors;
    }

    /// <summary>
    /// Determines whether <paramref name="schemaName"/> holds <paramref name="type"/> on
    /// <paramref name="path"/> only through a viewer field re-rooted at <c>Query.viewer</c>.
    /// </summary>
    private bool IsViewerOnlyOnPath(MutableObjectTypeDefinition type, PathNode path, string schemaName)
    {
        if (GetQueryViewerFieldForPath([.. path.EnumerateFromLeaf()]) is null)
        {
            return false;
        }

        // A walk that the budget cuts does not decide, which counts as a call that is not re-rooted.
        var budget = SatisfiabilityExplanationBudget.DecisionOnly(MaxExplanationEntries);
        var errors = ValidateSourceSchemaTransition(
            type,
            path,
            schemaName,
            budget,
            transitionFails: false,
            reroutesViewerFields: false);

        return !errors.IsEmpty && !budget.IsExhausted;
    }

    /// <summary>
    /// Determines whether a new call can enter the holding schema at <paramref name="type"/> held on
    /// <paramref name="path"/>, through a lookup of the type, or by entering the holding schema at an
    /// object above it and re-traversing the path.
    /// </summary>
    private bool CanReenterHolderOnPath(
        MutableObjectTypeDefinition type,
        PathNode path,
        SatisfiabilityExplanationBudget budget)
    {
        var item = path.Item;
        var holder = item.SchemaName;
        SatisfiabilityPathItem[] arrivalPath = [.. path.EnumerateFromLeaf()];

        foreach (var lookup in _lookupCache.GetPossibleFusionLookupDirectives(type, holder))
        {
            if (_requirementsValidator.Validate(
                _lookupCache.GetLookupKey(lookup),
                type,
                item,
                arrivalPath[1..],
                new RequirementScope(Anchor: null, arrivalPath, AllowsReRootedCalls: true),
                excludeSchemaName: null,
                budget).IsEmpty)
            {
                return true;
            }
        }

        if (path.Parent is not { } parentPath
            || item.Type is not MutableObjectTypeDefinition parentType
            || !item.Field.ExistsInSchema(holder)
            || item.Field.IsPartial(holder))
        {
            return false;
        }

        var parentItem = parentPath.Item;

        return parentItem.ProvidedSelectionSet is not null
            ? parentItem.SchemaName == holder && CanReenterHolderOnPath(parentType, parentPath, budget)
            : parentItem.SchemaName == holder
                || ValidateSourceSchemaTransition(
                    parentType,
                    parentPath,
                    holder,
                    budget,
                    transitionFails: false,
                    reroutesViewerFields: true).IsEmpty;
    }

    /// <summary>
    /// Determines whether a walk of the path agrees with the decision of the visit key, which
    /// <paramref name="isSatisfiable"/> holds. A walk that its budget cuts is inconclusive, and so is a
    /// failed walk that found a requirement cycle.
    /// </summary>
    private bool AgreesWithWalk(bool isSatisfiable, Func<SatisfiabilityExplanationBudget, bool> walk)
    {
        var budget = SatisfiabilityExplanationBudget.DecisionOnly(MaxExplanationEntries);
        var cycleCount = _requirementsValidator.CycleCount;
        var walkIsSatisfiable = walk(budget);

        return budget.IsExhausted
            || isSatisfiable == walkIsSatisfiable
            || (isSatisfiable && _requirementsValidator.CycleCount != cycleCount);
    }

    /// <summary>
    /// Validates that <paramref name="type"/>, held on <paramref name="path"/>, can be entered in
    /// <paramref name="transitionToSchemaName"/> by walking the whole path, which re-roots viewer fields at
    /// <c>Query.viewer</c> on mutation and subscription paths when <paramref name="reroutesViewerFields"/>
    /// is set.
    /// </summary>
    private ImmutableArray<SatisfiabilityError> ValidateSourceSchemaTransition(
        MutableObjectTypeDefinition type,
        PathNode? path,
        string transitionToSchemaName,
        SatisfiabilityExplanationBudget budget,
        bool transitionFails,
        bool reroutesViewerFields)
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
                    pathFromParent.Count > 1 ? [.. pathFromParent.Skip(1)] : [],
                    new RequirementScope(Anchor: null, pathFromLeaf, AllowsReRootedCalls: true),
                    excludeSchemaName: transitionToSchemaName,
                    budget),
            SatisfiabilityValidator_NoLookupsFoundForType,
            SatisfiabilityValidator_UnableToSatisfyRequirementForLookup,
            reroutesViewerFields ? GetQueryViewerFieldForPath(pathFromLeaf) : null,
            anchor: null,
            unavailableSchemaNames: [],
            budget,
            transitionFails);
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
