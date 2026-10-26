using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Satisfiability;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

internal sealed partial class SatisfiabilityValidator
{
    private readonly Dictionary<MutableObjectTypeDefinition, Exclusion[]> _exclusions = [];
    private readonly Dictionary<(ExclusionReaches?, ExclusionReach), ExclusionReaches> _exclusionReaches = [];

    /// <summary>
    /// Finds, for each object type, the sets of schemas whose data a requirement check at the type or
    /// below it may treat as unavailable, and for each set the schemas that such a check may ask to
    /// hold the type in and those it may read requirement data from.
    /// </summary>
    private void InitializeExclusions()
    {
        _exclusions.Clear();

        var relevant = new Dictionary<MutableObjectTypeDefinition, Dictionary<SchemaSet, HashSet<int>>>();
        var readRelevant = new Dictionary<MutableObjectTypeDefinition, Dictionary<SchemaSet, HashSet<int>>>();
        var pending = new Queue<MutableObjectTypeDefinition>();
        var scan = new ExclusionScan();
        var scannedKeys = new HashSet<(MutableObjectTypeDefinition, SchemaSet)>();

        // A visit validates a requirement on a type that an event stream message provides, and on any
        // other type only when the facts cannot confirm it.
        var providedTypes = GetProvidedTypes();

        foreach (var type in _schema.Types.OfType<MutableObjectTypeDefinition>())
        {
            var isProvided = providedTypes.Contains(type);

            foreach (var field in type.Fields)
            {
                foreach (var schemaName in field.GetSchemaNames())
                {
                    if (field.GetFusionRequiresRequirements(schemaName) is not { } requirements
                        || (!isProvided && _facts.IsFieldResolvableOn(type, field, schemaName)))
                    {
                        continue;
                    }

                    scan.Clear();
                    CollectFirstLevelSchemas(requirements, type, scan.AnchorSchemas);
                    CollectFirstLevelSchemas(requirements, type, scan.ReadSchemas);
                    CollectExclusions(requirements, type, SchemaSet.Empty, true, scan);

                    // A requiring field whose schema cannot always re-enter the type through a lookup is
                    // resolved without the data of its schema.
                    if (!_schema.IsRootOperationType(type)
                        && (isProvided || !HasSelfServedLookup(type, schemaName)))
                    {
                        var unavailable = AddToSchemaSet(SchemaSet.Empty, schemaName);
                        scan.Sets.Add(unavailable);
                        CollectExclusions(requirements, type, unavailable, true, scan);
                    }

                    AddRelevantSchemas(relevant, type, scan.Sets, scan.AnchorSchemas, pending);
                    AddReadSchemas(readRelevant, type, scan);
                }
            }

            // A lookup key that selects a requiring field makes the schema of that field unavailable.
            scannedKeys.Add((type, SchemaSet.Empty));
            CollectLookupKeyExclusions(type, SchemaSet.Empty, relevant, readRelevant, scan, pending);
        }

        // A parent carries every set of its children, with the schemas that serve the field returning
        // the child.
        var parentFields = GetParentFields();

        while (pending.TryDequeue(out var type))
        {
            var typeRelevant = relevant[type];

            // A check without the data of a set validates the lookup keys of the type without the
            // same data, which makes the schemas of the requiring fields they select unavailable too.
            foreach (var set in typeRelevant.Keys.ToArray())
            {
                if (scannedKeys.Add((type, set)))
                {
                    CollectLookupKeyExclusions(type, set, relevant, readRelevant, scan, pending);
                }
            }

            foreach (var (set, indexes) in typeRelevant)
            {
                CloseOverLookupKeys(type, set, indexes);
            }

            if (!parentFields.TryGetValue(type, out var parents))
            {
                continue;
            }

            foreach (var (parent, field) in parents)
            {
                var changed = false;

                foreach (var (set, indexes) in typeRelevant)
                {
                    changed |= AddRelevantSchemas(
                        relevant,
                        parent,
                        set,
                        indexes.Where(index => field.ExistsInSchema(_schemaSets.GetSchemaName(index))));
                }

                if (changed)
                {
                    pending.Enqueue(parent);
                }
            }
        }

        // A check at a type reads requirement data at the type itself from these schemas.
        var localReadRelevant = readRelevant.ToDictionary(
            entry => entry.Key,
            entry => entry.Value.ToDictionary(set => set.Key, set => _schemaSets.Create([.. set.Value])));

        // A parent may read requirement data from the schemas of its children that serve the field
        // returning the child.
        var pendingRead = new Queue<MutableObjectTypeDefinition>(readRelevant.Keys);

        while (pendingRead.TryDequeue(out var type))
        {
            if (!parentFields.TryGetValue(type, out var parents))
            {
                continue;
            }

            foreach (var (parent, field) in parents)
            {
                var changed = false;

                foreach (var (set, indexes) in readRelevant[type])
                {
                    changed |= AddRelevantSchemas(
                        readRelevant,
                        parent,
                        set,
                        indexes.Where(index => field.ExistsInSchema(_schemaSets.GetSchemaName(index))));
                }

                if (changed)
                {
                    pendingRead.Enqueue(parent);
                }
            }
        }

        foreach (var (type, typeRelevant) in relevant)
        {
            readRelevant.TryGetValue(type, out var typeReadRelevant);
            localReadRelevant.TryGetValue(type, out var typeLocalReadRelevant);

            // A reach without the data of more schemas is created first.
            _exclusions.Add(
                type,
                [
                    .. typeRelevant
                        .OrderByDescending(entry => _schemaSets.Count(entry.Key))
                        .ThenBy(entry => entry.Key.Id)
                        .Select(entry => new Exclusion(
                            entry.Key,
                            GetSchemaNames(entry.Key),
                            _schemaSets.Create([.. entry.Value]),
                            typeReadRelevant?.TryGetValue(entry.Key, out var readIndexes) == true
                                ? _schemaSets.Create([.. readIndexes])
                                : SchemaSet.Empty,
                            typeLocalReadRelevant?.GetValueOrDefault(entry.Key) ?? SchemaSet.Empty))
                ]);
        }
    }

    /// <summary>
    /// Adds the schemas that <paramref name="scan"/> may read requirement data from to the schemas that a check
    /// at <paramref name="type"/> without the data of each set of the scan may read requirement data from.
    /// </summary>
    private void AddReadSchemas(
        Dictionary<MutableObjectTypeDefinition, Dictionary<SchemaSet, HashSet<int>>> readRelevant,
        MutableObjectTypeDefinition type,
        ExclusionScan scan)
    {
        foreach (var set in scan.Sets)
        {
            AddRelevantSchemas(readRelevant, type, set, scan.ReadSchemas);
        }
    }

    /// <summary>
    /// Adds the sets of unavailable schemas that the lookup keys of <paramref name="type"/> create when
    /// they are validated without the data of <paramref name="unavailable"/> to the exclusions of the type.
    /// </summary>
    private void CollectLookupKeyExclusions(
        MutableObjectTypeDefinition type,
        SchemaSet unavailable,
        Dictionary<MutableObjectTypeDefinition, Dictionary<SchemaSet, HashSet<int>>> relevant,
        Dictionary<MutableObjectTypeDefinition, Dictionary<SchemaSet, HashSet<int>>> readRelevant,
        ExclusionScan scan,
        Queue<MutableObjectTypeDefinition> pending)
    {
        foreach (var lookup in _lookupCache.GetPossibleFusionLookupDirectives(type))
        {
            var key = _lookupCache.GetLookupKey(lookup);

            scan.Clear();
            CollectFirstLevelSchemas(key, type, scan.AnchorSchemas);
            CollectExclusions(key, type, unavailable, true, scan);
            AddRelevantSchemas(relevant, type, scan.Sets, scan.AnchorSchemas, pending);
            AddReadSchemas(readRelevant, type, scan);
        }
    }

    /// <summary>
    /// Adds <paramref name="anchorSchemas"/> to the schemas that a check at <paramref name="type"/>
    /// without the data of each of <paramref name="sets"/> may ask for, and enqueues the type when it has
    /// a set.
    /// </summary>
    private void AddRelevantSchemas(
        Dictionary<MutableObjectTypeDefinition, Dictionary<SchemaSet, HashSet<int>>> relevant,
        MutableObjectTypeDefinition type,
        HashSet<SchemaSet> sets,
        HashSet<int> anchorSchemas,
        Queue<MutableObjectTypeDefinition> pending)
    {
        if (sets.Count == 0)
        {
            return;
        }

        foreach (var set in sets)
        {
            AddRelevantSchemas(relevant, type, set, anchorSchemas);
        }

        pending.Enqueue(type);
    }

    /// <summary>
    /// Adds <paramref name="indexes"/>, except the unavailable schemas of <paramref name="set"/>, to
    /// the schemas that a check at <paramref name="type"/> without data from <paramref name="set"/>
    /// may ask for, creating the entry for the set if it is missing, and returns whether the entry
    /// was created or a schema was added.
    /// </summary>
    private bool AddRelevantSchemas(
        Dictionary<MutableObjectTypeDefinition, Dictionary<SchemaSet, HashSet<int>>> relevant,
        MutableObjectTypeDefinition type,
        SchemaSet set,
        IEnumerable<int> indexes)
    {
        if (!relevant.TryGetValue(type, out var typeRelevant))
        {
            typeRelevant = [];
            relevant.Add(type, typeRelevant);
        }

        var changed = false;

        if (!typeRelevant.TryGetValue(set, out var schemas))
        {
            schemas = [];
            typeRelevant.Add(set, schemas);
            changed = true;
        }

        foreach (var index in indexes)
        {
            if (!_schemaSets.Contains(set, index))
            {
                changed |= schemas.Add(index);
            }
        }

        return changed;
    }

    /// <summary>
    /// Adds to <paramref name="indexes"/> the schemas that serve the key fields of the lookups of
    /// <paramref name="type"/> in the schemas of <paramref name="indexes"/>, except the unavailable
    /// schemas of <paramref name="set"/>.
    /// </summary>
    private void CloseOverLookupKeys(MutableObjectTypeDefinition type, SchemaSet set, HashSet<int> indexes)
    {
        var pending = new Queue<int>(indexes);
        var keySchemas = new HashSet<int>();

        while (pending.TryDequeue(out var index))
        {
            foreach (var lookup in _lookupCache.GetPossibleFusionLookupDirectives(type, _schemaSets.GetSchemaName(index)))
            {
                keySchemas.Clear();
                CollectFirstLevelSchemas(_lookupCache.GetLookupKey(lookup), type, keySchemas);

                foreach (var keySchema in keySchemas)
                {
                    if (!_schemaSets.Contains(set, keySchema) && indexes.Add(keySchema))
                    {
                        pending.Enqueue(keySchema);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Adds to <paramref name="indexes"/> the schemas that serve a field which
    /// <paramref name="selectionSet"/> selects on <paramref name="type"/> at its first level.
    /// </summary>
    private void CollectFirstLevelSchemas(
        SelectionSetNode selectionSet,
        MutableObjectTypeDefinition type,
        HashSet<int> indexes)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode fieldNode when type.Fields.TryGetField(fieldNode.Name.Value, out var field):
                    foreach (var schemaName in field.GetSchemaNames())
                    {
                        indexes.Add(_schemaSets.GetIndex(schemaName));
                    }

                    break;

                case InlineFragmentNode { TypeCondition: null } inlineFragment:
                    CollectFirstLevelSchemas(inlineFragment.SelectionSet, type, indexes);
                    break;

                case InlineFragmentNode inlineFragment
                    when _schema.Types.TryGetType(inlineFragment.TypeCondition.Name.Value, out var fragmentType)
                        && _schema.GetPossibleTypes(fragmentType).Contains(type):
                    CollectFirstLevelSchemas(inlineFragment.SelectionSet, type, indexes);
                    break;
            }
        }
    }

    /// <summary>
    /// Gets the object types that a path item may hold with a selection set that an event stream
    /// message provides.
    /// </summary>
    private HashSet<MutableObjectTypeDefinition> GetProvidedTypes()
    {
        var providedTypes = new HashSet<MutableObjectTypeDefinition>();

        foreach (var type in _schema.Types.OfType<MutableObjectTypeDefinition>())
        {
            foreach (var field in type.Fields)
            {
                foreach (var schemaName in field.GetFusionEventStreamSchemaNames())
                {
                    if (field.GetFusionEventStreamMessage(schemaName) is { } message)
                    {
                        CollectProvidedTypes(field, message, providedTypes);
                    }
                }
            }
        }

        return providedTypes;
    }

    /// <summary>
    /// Adds the possible types of <paramref name="field"/> to <paramref name="providedTypes"/>,
    /// and the types below them that <paramref name="selectionSet"/> provides a selection set for.
    /// </summary>
    private void CollectProvidedTypes(
        MutableOutputFieldDefinition field,
        SelectionSetNode selectionSet,
        HashSet<MutableObjectTypeDefinition> providedTypes)
    {
        if (field.Type.AsTypeDefinition() is not (MutableComplexTypeDefinition or MutableUnionTypeDefinition))
        {
            return;
        }

        foreach (var possibleType in _schema.GetPossibleTypes(field.Type.AsTypeDefinition()))
        {
            if (providedTypes.Add(possibleType))
            {
                CollectProvidedTypes(possibleType, selectionSet, providedTypes);
            }
        }
    }

    private void CollectProvidedTypes(
        MutableObjectTypeDefinition type,
        SelectionSetNode selectionSet,
        HashSet<MutableObjectTypeDefinition> providedTypes)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode { SelectionSet: { } fieldSelectionSet } fieldNode
                    when type.Fields.TryGetField(fieldNode.Name.Value, out var field):
                    CollectProvidedTypes(field, fieldSelectionSet, providedTypes);
                    break;

                case InlineFragmentNode inlineFragment:
                    CollectProvidedTypes(type, inlineFragment.SelectionSet, providedTypes);
                    break;
            }
        }
    }

    /// <summary>
    /// Determines whether <paramref name="type"/> has a lookup in <paramref name="schemaName"/>
    /// whose key selects only leaf fields that the schema itself serves.
    /// </summary>
    private bool HasSelfServedLookup(MutableObjectTypeDefinition type, string schemaName)
    {
        foreach (var lookup in _lookupCache.GetPossibleFusionLookupDirectives(type, schemaName))
        {
            var isSelfServed = true;

            foreach (var selection in _lookupCache.GetLookupKey(lookup).Selections)
            {
                if (selection is not FieldNode { SelectionSet: null } fieldNode
                    || !type.Fields.TryGetField(fieldNode.Name.Value, out var keyField)
                    || !keyField.ExistsInSchema(schemaName)
                    || keyField.IsPartial(schemaName)
                    || keyField.GetFusionRequiresRequirements(schemaName) is not null)
                {
                    isSelfServed = false;
                    break;
                }
            }

            if (isSelfServed)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Adds to the sets of <paramref name="scan"/> the unavailable schemas of the requiring fields that
    /// <paramref name="selectionSet"/> selects on <paramref name="type"/> while the schemas of
    /// <paramref name="unavailable"/> are unavailable, also through the lookup keys of the types below
    /// the type. When the selection set is validated at the object (<paramref name="atAnchor"/>), the
    /// schemas that serve the first level of a nested requirement are added to the anchor schemas of
    /// <paramref name="scan"/>.
    /// </summary>
    private void CollectExclusions(
        SelectionSetNode selectionSet,
        MutableObjectTypeDefinition type,
        SchemaSet unavailable,
        bool atAnchor,
        ExclusionScan scan)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode fieldNode when type.Fields.TryGetField(fieldNode.Name.Value, out var field):
                    foreach (var schemaName in field.GetSchemaNames())
                    {
                        if (field.GetFusionRequiresRequirements(schemaName) is not { } requirements
                            || !scan.VisitedFields.Add((type, field, schemaName, unavailable)))
                        {
                            continue;
                        }

                        if (atAnchor)
                        {
                            CollectFirstLevelSchemas(requirements, type, scan.AnchorSchemas);
                            CollectFirstLevelSchemas(requirements, type, scan.ReadSchemas);
                        }

                        var extended = AddToSchemaSet(unavailable, schemaName);
                        scan.Sets.Add(extended);
                        CollectExclusions(requirements, type, unavailable, atAnchor, scan);
                        CollectExclusions(requirements, type, extended, atAnchor, scan);
                    }

                    if (fieldNode.SelectionSet is { } fieldSelectionSet
                        && field.Type.AsTypeDefinition() is MutableComplexTypeDefinition or MutableUnionTypeDefinition)
                    {
                        foreach (var possibleType in _schema.GetPossibleTypes(field.Type.AsTypeDefinition()))
                        {
                            CollectExclusions(fieldSelectionSet, possibleType, unavailable, atAnchor: false, scan);

                            // A walk that enters the type below the object in another schema validates the
                            // keys of its lookups without the same data.
                            if (scan.VisitedLookupTypes.Add((possibleType, unavailable)))
                            {
                                foreach (var lookup in _lookupCache.GetPossibleFusionLookupDirectives(possibleType))
                                {
                                    CollectExclusions(
                                        _lookupCache.GetLookupKey(lookup),
                                        possibleType,
                                        unavailable,
                                        atAnchor: false,
                                        scan);
                                }
                            }
                        }
                    }

                    break;

                case InlineFragmentNode { TypeCondition: null } inlineFragment:
                    CollectExclusions(inlineFragment.SelectionSet, type, unavailable, atAnchor, scan);
                    break;

                case InlineFragmentNode inlineFragment
                    when _schema.Types.TryGetType(inlineFragment.TypeCondition.Name.Value, out var fragmentType)
                        && _schema.GetPossibleTypes(fragmentType).Contains(type):
                    CollectExclusions(inlineFragment.SelectionSet, type, unavailable, atAnchor, scan);
                    break;
            }
        }
    }

    /// <summary>
    /// Creates the reaches of <paramref name="type"/>, held through <paramref name="item"/>, without the
    /// data of each set of the type's exclusions, or returns <c>null</c> if the type has no exclusions.
    /// Each reach extends the reach of the visit of <paramref name="parent"/> without the same data, or, when
    /// <paramref name="parent"/> is <c>null</c>, is created for a node lookup.
    /// </summary>
    private ExclusionReaches? CreateExclusionReaches(
        MutableObjectTypeDefinition type,
        VisitTypeInfo info,
        SatisfiabilityPathItem item,
        WorkItem? parent,
        bool viewerFallback)
    {
        if (!_exclusions.TryGetValue(type, out var exclusions))
        {
            return null;
        }

        var parentKey = parent?.Key;
        var holder = _schemaSets.GetIndex(item.SchemaName);
        var anchor = RentReachAnchor();
        ExclusionReaches? reaches = null;
        Dictionary<int, bool>? parentResolutions = null;

        foreach (var exclusion in exclusions)
        {
            var buffer = _schemaSets.RentBuffer(SchemaSet.Empty);
            List<(int SchemaIndex, SelectionSetNode SelectionSet)>? provides = null;

            // The call holding the object was made without the unavailable data when it is a root call,
            // a node lookup, or the call holding the parent in the same schema made without it.
            var holderAvailable = !_schemaSets.Contains(exclusion.Schemas, holder);
            var parentReach =
                new ExclusionReach(exclusion.Schemas, SchemaSet.Empty, null, false, SchemaSet.Empty, default);

            if (parentKey is not null)
            {
                if (parentKey.SchemaName is not null)
                {
                    var parentEntry = parentKey.Exclusions?.Find(exclusion.Schemas);

                    Debug.Assert(
                        parentEntry is not null,
                        $"The parent of '{type.Name}' has no reach without the data of the given schemas.");

                    if (parentEntry is { } entry)
                    {
                        parentReach = entry;
                    }

                    holderAvailable &= parentKey.SchemaName == item.SchemaName && parentReach.HolderAvailable;
                }

                AddRetraversals(item, parentKey, parentReach.Reach, parentReach.ReachProvides, buffer, ref provides);
            }

            _schemaSets.IntersectWith(buffer, info.Schemas);
            _schemaSets.IntersectWith(buffer, exclusion.Relevant);

            anchor.Initialize(
                type,
                item,
                viewerFallback,
                exclusion,
                buffer,
                provides,
                reaches,
                holderAvailable,
                allowsReRootedCalls: true);

            AddLookupEntries(type, info, item, buffer, skippedIndex: -1, anchor);

            // The holding call counts only for a check that may ask for the holding schema.
            holderAvailable =
                _schemaSets.Contains(exclusion.Relevant, holder)
                && (holderAvailable || SchemaSetTable.Contains(buffer, holder));

            var reach = _schemaSets.Intern(buffer);
            var viewerOnly = parentKey is null
                ? SchemaSet.Empty
                : CreateViewerOnly(
                    type,
                    info,
                    item,
                    parentKey,
                    parentReach,
                    reach,
                    holderAvailable,
                    anchor);

            reaches = AddExclusionReach(
                reaches,
                new ExclusionReach(
                    exclusion.Schemas,
                    reach,
                    CreateReachProvides(provides, reach),
                    holderAvailable,
                    viewerOnly,
                    CreateArrivalCalls(
                        item,
                        parent,
                        parentReach,
                        exclusion,
                        reach,
                        holderAvailable,
                        ref parentResolutions)));
            _schemaSets.ReturnBuffer(buffer);
        }

        ReturnReachAnchor(anchor);
        return reaches;
    }

    /// <summary>
    /// Creates the calls of the access path that hold the object of <paramref name="item"/>, or its parent, without
    /// the data of <paramref name="exclusion"/>, from those of the visit of <paramref name="parent"/> in
    /// <paramref name="parentReach"/>, or for a node lookup when <paramref name="parent"/> is <c>null</c>.
    /// <paramref name="reach"/> and <paramref name="holderAvailable"/> are the reach of the object without that
    /// data and whether the holding call is made without it. <paramref name="parentResolutions"/> holds, by schema
    /// index, whether a schema resolves the field of <paramref name="item"/> at the visit of the parent.
    /// </summary>
    private ArrivalCalls CreateArrivalCalls(
        SatisfiabilityPathItem item,
        WorkItem? parent,
        ExclusionReach parentReach,
        Exclusion exclusion,
        SchemaSet reach,
        bool holderAvailable,
        ref Dictionary<int, bool>? parentResolutions)
    {
        var parentKey = parent?.Key;
        var parentCalls = parentReach.Arrival;

        if (parentKey is not null && (parentCalls.BelowReRootedViewer || IsViewerFallback(item, parentKey)))
        {
            return new ArrivalCalls(false, false, SchemaSet.Empty, SchemaSet.Empty, BelowReRootedViewer: true);
        }

        var holder = _schemaSets.GetIndex(item.SchemaName);
        var parentHolder = parentKey?.SchemaName;
        var unavailableSchemaName = exclusion.SchemaNames.Count == 1 ? exclusion.SchemaNames.First() : null;
        var holderIsUnavailable = _schemaSets.Contains(exclusion.Schemas, holder);
        var holderReadable = false;
        var holderPending = false;
        var parentReadable = SchemaSet.Empty;
        var routeReadable = SchemaSet.Empty;

        // A pending call of the parent no longer depends on the call that resolves the requiring field when
        // the unavailable schema does not serve the field or holds the object in a later call.
        var released =
            unavailableSchemaName is null
            || holderIsUnavailable
            || !item.Field.ExistsInSchema(unavailableSchemaName);
        var parentHolderReadable =
            parentHolder is not null
            && (parentReach.HolderAvailable
                || parentCalls.HolderReadable
                || (parentCalls.HolderPending && released));

        if (parentHolder is not null && parentKey!.Leaf?.ProvidedSelectionSet is null)
        {
            var parentServesField = ServesField(item.Field, parentHolder);
            var parentServesFieldPlainly =
                parentServesField && item.Field.GetFusionRequiresRequirements(parentHolder) is null;

            if (parentServesFieldPlainly)
            {
                // The call that holds the parent continues to the object when its schema serves the field without
                // requirements.
                var parentIndex = _schemaSets.GetIndex(parentHolder);

                if (parentHolderReadable
                    && parentHolder != item.SchemaName
                    && _schemaSets.Contains(exclusion.LocalReadRelevant, parentIndex)
                    && !_schemaSets.Contains(reach, parentIndex))
                {
                    parentReadable = _schemaSets.Create([parentIndex]);
                }
            }
            else if (unavailableSchemaName is not null
                && (parentServesField || !HolderProvidesField(parentKey, item.Field)))
            {
                // Otherwise a call of each schema that resolves the field from the parent can hold the object, and
                // that call does not depend on the call that resolves the requiring field of the unavailable schema.
                routeReadable = CreateRouteReadable(
                    item,
                    parent!.Value,
                    exclusion,
                    reach,
                    parentHolderReadable,
                    ref parentResolutions);
            }
        }

        // A holding call made with the unavailable data is read unless it depends on the call that resolves the
        // requiring field, which a single unavailable schema identifies as its last call on the path while the
        // schema serves the fields below it.
        if (!holderAvailable
            && !holderIsUnavailable
            && item.ProvidedSelectionSet is null
            && _schemaSets.Contains(exclusion.ReadRelevant, holder)
            && !_schemaSets.Contains(reach, holder))
        {
            if (parentHolder == item.SchemaName)
            {
                holderReadable = parentHolderReadable;
                holderPending = !parentHolderReadable && parentCalls.HolderPending;
            }
            else if (unavailableSchemaName is not null)
            {
                holderPending = !released && parentHolder == unavailableSchemaName;
                holderReadable = !holderPending;
            }
        }

        return new ArrivalCalls(
            holderReadable,
            holderPending,
            parentReadable,
            routeReadable,
            BelowReRootedViewer: false);
    }

    /// <summary>
    /// Creates the schemas, other than the holding schema of <paramref name="item"/> and among those that a check at
    /// the object without the data of <paramref name="exclusion"/> reads requirement data from and that are not
    /// in <paramref name="reach"/>, that resolve the field of <paramref name="item"/> at the visit of
    /// <paramref name="parent"/>, the schema holding the parent only if <paramref name="parentHolderReadable"/>.
    /// </summary>
    private SchemaSet CreateRouteReadable(
        SatisfiabilityPathItem item,
        in WorkItem parent,
        Exclusion exclusion,
        SchemaSet reach,
        bool parentHolderReadable,
        ref Dictionary<int, bool>? parentResolutions)
    {
        var holder = _schemaSets.GetIndex(item.SchemaName);
        var parentHolder = _schemaSets.GetIndex(parent.Key.SchemaName!);
        var members = _schemaSets.GetMembers(exclusion.LocalReadRelevant);
        List<int>? indexes = null;

        for (var word = 0; word < members.Length; word++)
        {
            for (var bits = members[word]; bits != 0; bits &= bits - 1)
            {
                var index = (word << 6) + BitOperations.TrailingZeroCount(bits);

                if (index != holder
                    && (index != parentHolder || parentHolderReadable)
                    && !_schemaSets.Contains(reach, index)
                    && ServesField(item.Field, _schemaSets.GetSchemaName(index))
                    && ResolvesFieldAtParent(parent, item.Field, index, ref parentResolutions))
                {
                    (indexes ??= []).Add(index);
                }
            }
        }

        return indexes is null ? SchemaSet.Empty : _schemaSets.Create(indexes);
    }

    /// <summary>
    /// Determines whether the schema with index <paramref name="schemaIndex"/> resolves <paramref name="field"/> as
    /// an option at the visit of <paramref name="parent"/>, a schema other than the holding schema with requirements
    /// only through one of its lookups for the object, and caches the answer in <paramref name="parentResolutions"/>.
    /// </summary>
    private bool ResolvesFieldAtParent(
        in WorkItem parent,
        MutableOutputFieldDefinition field,
        int schemaIndex,
        ref Dictionary<int, bool>? parentResolutions)
    {
        parentResolutions ??= [];

        if (!parentResolutions.TryGetValue(schemaIndex, out var resolves))
        {
            var schemaName = _schemaSets.GetSchemaName(schemaIndex);
            var type = parent.Key.ObjectType;
            var parentHolder = parent.Key.SchemaName!;
            var requirements = field.GetFusionRequiresRequirements(schemaName);

            resolves =
                (schemaName == parentHolder
                    || _facts.CanTransition(type, schemaName, parentHolder)
                    || _schemaSets.Contains(parent.Key.Reach, schemaIndex))
                && (requirements is null
                    || _facts.IsFieldResolvableOn(type, field, schemaName)
                    || (schemaName == parentHolder
                        ? ValidateRequirements(requirements, field, schemaName, parent, anchored: true, _decisionBudget)
                            .IsEmpty
                        : ResolvesThroughLookup(
                            requirements,
                            schemaName,
                            type,
                            parent.Path!.Item,
                            [],
                            new RequirementScope(
                                new VisitAnchor(this, parent.Key, parent.Path.Item),
                                ArrivalPath: null,
                                AllowsReRootedCalls: true,
                                BelowReRootedViewer: null,
                                ResolvesFieldAtParent: null),
                            _decisionBudget)));
            parentResolutions.Add(schemaIndex, resolves);
        }

        return resolves;
    }

    /// <summary>
    /// Determines whether <paramref name="schemaName"/> serves <paramref name="field"/> without it being partial.
    /// </summary>
    private static bool ServesField(MutableOutputFieldDefinition field, string schemaName)
        => field.ExistsInSchema(schemaName) && !field.IsPartial(schemaName);

    /// <summary>
    /// Determines whether the <c>@provides</c> selection set of the field holding the object of
    /// <paramref name="key"/>, in its holding schema, selects <paramref name="field"/>.
    /// </summary>
    private bool HolderProvidesField(VisitKey key, MutableOutputFieldDefinition field)
        => key.Leaf is { } leaf
            && GetFieldProvides(leaf.Field)?.Find(_schemaSets.GetIndex(key.SchemaName!)) is { } provides
            && SatisfiabilityPathItemExtensions.TryGetFieldSelectionSet(
                _schema,
                provides,
                leaf.Field.Type.AsTypeDefinition(),
                field,
                key.ObjectType,
                out _);

    /// <summary>
    /// Gets the reaches with the entries of <paramref name="reaches"/>, or with none if it is <c>null</c>,
    /// followed by <paramref name="entry"/>, which is the same instance for equal entries.
    /// </summary>
    private ExclusionReaches AddExclusionReach(ExclusionReaches? reaches, ExclusionReach entry)
    {
        if (!_exclusionReaches.TryGetValue((reaches, entry), out var extended))
        {
            extended = new ExclusionReaches(reaches, entry);
            _exclusionReaches.Add((reaches, entry), extended);
        }

        return extended;
    }

    /// <summary>
    /// Gets the exclusion of <paramref name="type"/> for the schemas of
    /// <paramref name="unavailableSchemaNames"/>, or <c>null</c> if the type has no such exclusion.
    /// </summary>
    private Exclusion? FindExclusion(
        MutableObjectTypeDefinition type,
        ImmutableHashSet<string> unavailableSchemaNames)
    {
        if (_exclusions.TryGetValue(type, out var exclusions))
        {
            foreach (var exclusion in exclusions)
            {
                if (exclusion.Matches(unavailableSchemaNames))
                {
                    return exclusion;
                }
            }
        }

        return null;
    }

    private SchemaSet AddToSchemaSet(SchemaSet set, string schemaName)
    {
        var buffer = _schemaSets.GetBuffer(set);
        SchemaSetTable.Add(buffer, _schemaSets.GetIndex(schemaName));
        return _schemaSets.Intern(buffer);
    }

    private ImmutableHashSet<string> GetSchemaNames(SchemaSet set)
    {
        var names = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        var members = _schemaSets.GetMembers(set);

        for (var word = 0; word < members.Length; word++)
        {
            for (var bits = members[word]; bits != 0; bits &= bits - 1)
            {
                names.Add(_schemaSets.GetSchemaName((word << 6) + BitOperations.TrailingZeroCount(bits)));
            }
        }

        return names.ToImmutable();
    }

    /// <summary>
    /// A set of unavailable schemas, as a schema set and by name, with the schemas that a check at a
    /// type without their data may ask to hold the type in, those it may read requirement data from at the
    /// type or below it, and those it may read requirement data from at the type itself.
    /// </summary>
    private sealed record Exclusion(
        SchemaSet Schemas,
        ImmutableHashSet<string> SchemaNames,
        SchemaSet Relevant,
        SchemaSet ReadRelevant,
        SchemaSet LocalReadRelevant)
    {
        /// <summary>
        /// Determines whether the unavailable schemas are exactly those of
        /// <paramref name="unavailableSchemaNames"/>.
        /// </summary>
        public bool Matches(ImmutableHashSet<string> unavailableSchemaNames)
        {
            if (ReferenceEquals(SchemaNames, unavailableSchemaNames))
            {
                return true;
            }

            if (SchemaNames.Count != unavailableSchemaNames.Count)
            {
                return false;
            }

            foreach (var schemaName in unavailableSchemaNames)
            {
                if (!SchemaNames.Contains(schemaName))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// The state of a scan of one requirement or lookup key for the sets of unavailable schemas it creates.
    /// </summary>
    private sealed class ExclusionScan
    {
        /// <summary>
        /// Gets the sets of unavailable schemas that the scan found.
        /// </summary>
        public HashSet<SchemaSet> Sets { get; } = [];

        /// <summary>
        /// Gets the schemas that serve the first level of the scanned selection sets at the object.
        /// </summary>
        public HashSet<int> AnchorSchemas { get; } = [];

        /// <summary>
        /// Gets the schemas that serve the first level of the scanned requirements at the object.
        /// </summary>
        public HashSet<int> ReadSchemas { get; } = [];

        /// <summary>
        /// Gets the requiring fields that the scan visited for each set of unavailable schemas.
        /// </summary>
        public HashSet<(MutableObjectTypeDefinition, MutableOutputFieldDefinition, string, SchemaSet)> VisitedFields
        {
            get;
        } = [];

        /// <summary>
        /// Gets the types whose lookup keys the scan visited for each set of unavailable schemas.
        /// </summary>
        public HashSet<(MutableObjectTypeDefinition, SchemaSet)> VisitedLookupTypes { get; } = [];

        public void Clear()
        {
            Sets.Clear();
            AnchorSchemas.Clear();
            ReadSchemas.Clear();
            VisitedFields.Clear();
            VisitedLookupTypes.Clear();
        }
    }
}
