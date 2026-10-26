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
    private readonly Dictionary<(ITypeDefinition, string), MutableObjectTypeDefinition[]> _possibleTypes = [];
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
        _lookupCache = new FusionLookupDirectiveCache(schema);
        _facts = new SatisfiabilityFactsBuilder(schema, _lookupCache).Build();
        _requirementsValidator =
            new RequirementsValidator(
                schema,
                _lookupCache,
                _facts,
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
        var cycle = false;
        var errors = new List<SatisfiabilityError>();
        var optionCount = 0;
        var fieldType = field.Type.AsTypeDefinition();

        foreach (var schemaName in GetOptionSchemaNames(field, previousSchemaName))
        {
            var pathItem = TryResolveField(field, work, schemaName, ref canReenterWithLookup, ref cycle, errors);

            if (pathItem is null)
            {
                continue;
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
            if (IsExemptFromReporting(type, field, path))
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

    /// <summary>
    /// Gets the schemas that are candidates for resolving <paramref name="field"/>, starting with
    /// <paramref name="previousSchemaName"/> when it is a candidate.
    /// </summary>
    private static ImmutableArray<string> GetOptionSchemaNames(
        MutableOutputFieldDefinition field,
        string? previousSchemaName)
    {
        var schemaNames = field.GetSchemaNames(first: previousSchemaName);
        var eventStreamSchemaNames = field.GetFusionEventStreamSchemaNames();

        if (!eventStreamSchemaNames.IsDefaultOrEmpty)
        {
            schemaNames = previousSchemaName is not null && eventStreamSchemaNames.Contains(previousSchemaName)
                ? eventStreamSchemaNames.Remove(previousSchemaName).Insert(0, previousSchemaName)
                : eventStreamSchemaNames;
        }

        return schemaNames;
    }

    /// <summary>
    /// Gets the path item that accesses <paramref name="field"/> on the object of
    /// <paramref name="work"/> in <paramref name="schemaName"/>, or <c>null</c> when the schema is
    /// not an option. <paramref name="cycle"/> is set when the access repeats the field holding the
    /// object, and the reason a schema is not an option is added to <paramref name="errors"/> unless
    /// it is <c>null</c>.
    /// </summary>
    private SatisfiabilityPathItem? TryResolveField(
        MutableOutputFieldDefinition field,
        in WorkItem work,
        string schemaName,
        ref bool? canReenterWithLookup,
        ref bool cycle,
        List<SatisfiabilityError>? errors)
    {
        var type = work.Key.ObjectType;
        var path = work.Path;
        var previousSchemaName = path?.Item.SchemaName;
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
            return null;
        }

        // A partial (@external) field is never a resolution candidate in its declaring schema;
        // only an event stream message can make it an option. @provides never does (PR #231).
        if (field.IsPartial(schemaName)
            && path?.Item.ProvidesViaEventStream(field, type, schemaName, _schema) != true)
        {
            return null;
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
            && field.Type.AsTypeDefinition() is MutableObjectTypeDefinition
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
                AgreesWithTransitionHelper(canTransition, type, path, schemaName),
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

            var requirementErrors = errors is null
                ? ValidateRequirements(requirements, type, field, schemaName, path?.Item, ancestors, _decisionBudget)
                : GetRequirementExplanation(requirements, type, field, schemaName, path?.Item, ancestors);

            if (requirementErrors.Length != 0)
            {
                errors?.Add(
                    new SatisfiabilityError(
                        string.Format(
                            SatisfiabilityValidator_UnableToSatisfyRequirement,
                            requirements.ToString(indented: false),
                            pathItem),
                        requirementErrors));

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
        bool? canReenterWithLookup = null;

        foreach (var field in type.Fields)
        {
            if (field.HasFusionInaccessibleDirective() || field.HasFusionGatewayFieldDirective())
            {
                continue;
            }

            var resolved = false;
            var cycle = false;

            foreach (var schemaName in GetOptionSchemaNames(field, previousSchemaName))
            {
                if (TryResolveField(field, work, schemaName, ref canReenterWithLookup, ref cycle, errors: null)
                    is not null)
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
            explanation = ValidateSourceSchemaTransition(type, path, schemaName, budget);
            _explanationEntryCount += budget.EntryCount;
            _transitionExplanations.Add(schemaName, explanation);
        }

        return explanation;
    }

    /// <summary>
    /// Gets the explanation of why <paramref name="schemaName"/> cannot resolve <paramref name="field"/>
    /// with its <paramref name="requirements"/>, which is empty when it can. Past its entry budget, the
    /// explanation only keeps the entries that hold entries created before.
    /// </summary>
    private ImmutableArray<SatisfiabilityError> GetRequirementExplanation(
        SelectionSetNode requirements,
        MutableObjectTypeDefinition type,
        MutableOutputFieldDefinition field,
        string schemaName,
        SatisfiabilityPathItem? parentPathItem,
        IReadOnlyList<SatisfiabilityPathItem> ancestors)
    {
        // The search decides whether the requirement holds, so the budget lets it finish.
        var budget = SatisfiabilityExplanationBudget.Limited(MaxExplanationEntries, stopsSearch: false);
        var explanation =
            ValidateRequirements(requirements, type, field, schemaName, parentPathItem, ancestors, budget);
        _explanationEntryCount += budget.EntryCount;

        return explanation;
    }

    /// <summary>
    /// Validates that <paramref name="schemaName"/> can resolve <paramref name="field"/> of
    /// <paramref name="type"/>, held by <paramref name="parentPathItem"/>, with its
    /// <paramref name="requirements"/>, and returns the errors, which are empty when it can.
    /// </summary>
    private ImmutableArray<SatisfiabilityError> ValidateRequirements(
        SelectionSetNode requirements,
        MutableObjectTypeDefinition type,
        MutableOutputFieldDefinition field,
        string schemaName,
        SatisfiabilityPathItem? parentPathItem,
        IReadOnlyList<SatisfiabilityPathItem> ancestors,
        SatisfiabilityExplanationBudget budget)
    {
        var errors =
            _requirementsValidator.Validate(
                requirements,
                type,
                parentPathItem,
                ancestors,
                excludeSchemaName: schemaName,
                budget,
                allowNestedFieldsFromExcludedSchema: true);

        // The requirement data is fetched from other schemas, so the schema must be able
        // to resolve the field afterwards, either without its own data or by re-entering
        // the type through a lookup.
        if (errors.IsEmpty)
        {
            errors =
                _requirementsValidator.ValidateReentry(
                    requirements,
                    type,
                    field,
                    schemaName,
                    parentPathItem,
                    ancestors,
                    SatisfiabilityValidator_NoLookupForRequiringField,
                    SatisfiabilityValidator_UnableToSatisfyRequirementForLookup,
                    budget);
        }

        return errors;
    }

    /// <summary>
    /// Determines whether the source schema transition helper agrees with
    /// <paramref name="canTransition"/>, which is assumed when it cannot decide within the entry budget
    /// of an explanation.
    /// </summary>
    private bool AgreesWithTransitionHelper(
        bool canTransition,
        MutableObjectTypeDefinition type,
        PathNode? path,
        string schemaName)
    {
        var budget = SatisfiabilityExplanationBudget.DecisionOnly(MaxExplanationEntries);
        var errors = ValidateSourceSchemaTransition(type, path, schemaName, budget);

        return budget.IsExhausted || canTransition == errors.IsEmpty;
    }

    private ImmutableArray<SatisfiabilityError> ValidateSourceSchemaTransition(
        MutableObjectTypeDefinition type,
        PathNode? path,
        string transitionToSchemaName,
        SatisfiabilityExplanationBudget budget)
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
                    excludeSchemaName: transitionToSchemaName,
                    budget),
            SatisfiabilityValidator_NoLookupsFoundForType,
            SatisfiabilityValidator_UnableToSatisfyRequirementForLookup,
            GetQueryViewerField(pathFromLeaf),
            budget);
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
