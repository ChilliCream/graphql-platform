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
    private readonly SchemaSetTable _schemaSets;
    private readonly Dictionary<MutableOutputFieldDefinition, SchemaSet> _fieldSchemas = [];
    private readonly Dictionary<MutableOutputFieldDefinition, SchemaSelectionSets?> _fieldProvides = [];
    private readonly Dictionary<MutableObjectTypeDefinition, VisitTypeInfo> _visitTypeInfos = [];
    private readonly Dictionary<(MutableObjectTypeDefinition, string), SchemaSet> _transitionFacts = [];
    private readonly Dictionary<(MutableObjectTypeDefinition, string, bool), NodeReaches> _nodeReaches = [];
    private readonly Dictionary<LookupEntryKey, bool> _lookupEntries = [];
    private readonly List<int> _schemaIndexes = [];
    private readonly List<int> _compositeFieldSchemaIndexes = [];
    private readonly HashSet<string> _lookupSchemaNames = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the names of the source schemas that the fusion directives on the types of
    /// <paramref name="schema"/> and on their fields refer to, in ordinal order.
    /// </summary>
    private static SortedSet<string> GetSourceSchemaNames(MutableSchemaDefinition schema)
    {
        var schemaNames = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var type in schema.Types)
        {
            AddSchemaNames(type.Directives.AsEnumerable(), schemaNames);

            if (type is MutableComplexTypeDefinition complexType)
            {
                foreach (var field in complexType.Fields)
                {
                    AddSchemaNames(field.Directives.AsEnumerable(), schemaNames);
                }
            }
        }

        return schemaNames;

        static void AddSchemaNames(IEnumerable<IDirective> directives, SortedSet<string> schemaNames)
        {
            foreach (var directive in directives)
            {
                if (directive.Name.StartsWith("fusion__", StringComparison.Ordinal)
                    && directive.Arguments.TryGetValue(WellKnownArgumentNames.Schema, out var schemaName)
                    && schemaName.Value is string name)
                {
                    schemaNames.Add(name);
                }
            }
        }
    }

    /// <summary>
    /// Creates the key of the visit of <paramref name="type"/> held through <paramref name="item"/>,
    /// which the visit of <paramref name="parent"/> produced.
    /// </summary>
    private VisitKey CreateVisitKey(
        MutableObjectTypeDefinition type,
        SatisfiabilityPathItem item,
        in WorkItem parent)
    {
        var info = GetVisitTypeInfo(type);
        var parentKey = parent.Key;
        var transitionFacts = item.ProvidedSelectionSet is null
            ? GetTransitionFacts(type, info, item.SchemaName)
            : SchemaSet.Empty;
        var viewerFallback = parentKey.ViewerFallback && _viewerFallbackTypes.Contains(type);

        // The reaches without the data of some schemas are created first, and the lookup keys of the
        // reach read them.
        var exclusions = CreateExclusionReaches(type, info, item, parentKey, viewerFallback);
        var buffer = _schemaSets.CreateBuffer(SchemaSet.Empty);
        List<(int SchemaIndex, SelectionSetNode SelectionSet)>? provides = null;

        AddRetraversals(
            item,
            parentKey,
            parentKey.Reach,
            parentKey.SchemaName is not null ? parentKey.ReachProvides : null,
            buffer,
            ref provides);

        var anchor = new ReachAnchor(this, type, item, viewerFallback, exclusion: null, buffer, provides, exclusions);
        var reach = CompleteReach(type, info, item, transitionFacts, buffer, anchor);
        var reachProvides = CreateReachProvides(provides, reach);
        VisitLeaf? leaf = null;

        if (item.ProvidedSelectionSet is not null || info.ReadsLeaf)
        {
            leaf = new VisitLeaf(
                item.Field,
                item.Type,
                item.ProvidedSelectionSet is { } providedSelectionSet
                    ? new ProvidedSelectionSetKey(providedSelectionSet)
                    : null,
                item.ProvidedByEventStream);
        }

        return new VisitKey(
            type,
            item.SchemaName,
            reach,
            reachProvides,
            exclusions,
            leaf,
            viewerFallback,
            item.ProvidedSelectionSet is not null && CanReenterHolder(type, item, parentKey, anchor));
    }

    /// <summary>
    /// Adds to <paramref name="buffer"/> the schemas that hold the object of <paramref name="item"/>
    /// when they re-traverse its field from the parent of <paramref name="parentKey"/>, which they
    /// hold when <paramref name="parentReach"/> contains them, and adds the selection sets they
    /// provide for the object to <paramref name="provides"/>. <paramref name="parentProvides"/> holds
    /// the selection sets they provide for the parent.
    /// </summary>
    private void AddRetraversals(
        SatisfiabilityPathItem item,
        VisitKey parentKey,
        SchemaSet parentReach,
        SchemaSelectionSets? parentProvides,
        ulong[] buffer,
        ref List<(int SchemaIndex, SelectionSetNode SelectionSet)>? provides)
    {
        var fieldSchemas = GetFieldSchemas(item.Field);
        var fieldProvides = GetFieldProvides(item.Field);
        var isViewerFallback = IsViewerFallback(item, parentKey);
        var viewerProvides = isViewerFallback ? GetFieldProvides(_queryViewerField!) : null;

        _schemaSets.UnionWith(buffer, fieldSchemas);

        // Below a root type, keeps the schemas of the item's field that the parent reaches.
        if (parentKey.SchemaName is not null)
        {
            _schemaSets.IntersectWith(buffer, parentReach);
        }

        // A schema of the parent's reach also serves a partial field of the item when the selection
        // set it provides for the parent selects the field.
        if (parentProvides is not null)
        {
            for (var i = 0; i < parentProvides.Count; i++)
            {
                var index = parentProvides.GetSchemaIndex(i);

                if (_schemaSets.Contains(parentReach, index)
                    && SourceSchemaTransitionHelper.ServesOnRetraversal(
                        _schema,
                        item.Field,
                        item.Type,
                        _schemaSets.GetSchemaName(index),
                        parentProvides.GetSelectionSet(i),
                        fieldProvides?.Find(index),
                        out var providedSelectionSet))
                {
                    SchemaSetTable.Add(buffer, index);
                    AddProvides(ref provides, index, providedSelectionSet);
                }
            }
        }

        if (fieldProvides is not null)
        {
            for (var i = 0; i < fieldProvides.Count; i++)
            {
                var index = fieldProvides.GetSchemaIndex(i);

                if (SchemaSetTable.Contains(buffer, index) && parentProvides?.Find(index) is null)
                {
                    AddProvides(ref provides, index, fieldProvides.GetSelectionSet(i));
                }
            }
        }

        // A viewer field on a mutation or subscription path is re-rooted at Query.viewer in the
        // schemas that do not define it.
        if (isViewerFallback)
        {
            foreach (var index in _queryViewerSchemas)
            {
                if (!item.Field.ExistsInSchema(_schemaSets.GetSchemaName(index)))
                {
                    SchemaSetTable.Add(buffer, index);
                    AddProvides(ref provides, index, viewerProvides?.Find(index));
                }
            }
        }
    }

    /// <summary>
    /// Determines whether the field of <paramref name="item"/> is a viewer field that is re-rooted at
    /// <c>Query.viewer</c> below the parent of <paramref name="parentKey"/>.
    /// </summary>
    private bool IsViewerFallback(SatisfiabilityPathItem item, VisitKey parentKey)
        => parentKey.ViewerFallback
            && _queryViewerField is not null
            && item.Field.IsViewerFieldFor(_queryViewerField);

    /// <summary>
    /// Adds <paramref name="selectionSet"/> to the selection set provided in the schema with index
    /// <paramref name="index"/>, unless it is <c>null</c>.
    /// </summary>
    private static void AddProvides(
        ref List<(int SchemaIndex, SelectionSetNode SelectionSet)>? provides,
        int index,
        SelectionSetNode? selectionSet)
    {
        if (selectionSet is null)
        {
            return;
        }

        provides ??= [];

        for (var i = 0; i < provides.Count; i++)
        {
            if (provides[i].SchemaIndex == index)
            {
                provides[i] = (
                    index,
                    SourceSchemaTransitionHelper.CombineSelectionSets(provides[i].SelectionSet, selectionSet)!);
                return;
            }
        }

        provides.Add((index, selectionSet));
    }

    /// <summary>
    /// Creates the provided selection sets of the schemas in <paramref name="reach"/>, or returns
    /// <c>null</c> if none of them provides a selection set.
    /// </summary>
    private SchemaSelectionSets? CreateReachProvides(
        List<(int SchemaIndex, SelectionSetNode SelectionSet)>? provides,
        SchemaSet reach)
    {
        if (provides is null)
        {
            return null;
        }

        List<(int SchemaIndex, SelectionSetNode SelectionSet)>? entries = null;

        foreach (var entry in provides)
        {
            if (_schemaSets.Contains(reach, entry.SchemaIndex))
            {
                (entries ??= []).Add(entry);
            }
        }

        return entries is null ? null : new SchemaSelectionSets(entries);
    }

    /// <summary>
    /// Creates the key of the visit of <paramref name="type"/> held through the node lookup item
    /// <paramref name="lookupItem"/>, which the visit of <paramref name="parent"/> produced.
    /// </summary>
    private VisitKey CreateNodeVisitKey(
        MutableObjectTypeDefinition type,
        SatisfiabilityPathItem lookupItem,
        in WorkItem parent)
    {
        var info = GetVisitTypeInfo(type);
        var viewerFallback = parent.Key.ViewerFallback && _viewerFallbackTypes.Contains(type);

        // The reaches of a node item depend only on the type, the lookup schema and the viewer fallback.
        if (!_nodeReaches.TryGetValue((type, lookupItem.SchemaName, viewerFallback), out var reaches))
        {
            var exclusions = CreateExclusionReaches(type, info, lookupItem, parentKey: null, viewerFallback);
            var buffer = _schemaSets.CreateBuffer(SchemaSet.Empty);
            var anchor = new ReachAnchor(
                this,
                type,
                lookupItem,
                viewerFallback,
                exclusion: null,
                buffer,
                provides: null,
                exclusions);
            var transitionFacts = GetTransitionFacts(type, info, lookupItem.SchemaName);
            reaches = new NodeReaches(
                CompleteReach(type, info, lookupItem, transitionFacts, buffer, anchor),
                exclusions);
            _nodeReaches.Add((type, lookupItem.SchemaName, viewerFallback), reaches);
        }

        var leaf = info.ReadsLeaf
            ? new VisitLeaf(lookupItem.Field, lookupItem.Type, null, false)
            : null;

        return new VisitKey(
            type,
            lookupItem.SchemaName,
            reaches.Reach,
            null,
            reaches.Exclusions,
            leaf,
            viewerFallback,
            HolderReentry: false);
    }

    /// <summary>
    /// Creates the key of the visit of the root type <paramref name="rootType"/>.
    /// </summary>
    private VisitKey CreateRootVisitKey(MutableObjectTypeDefinition rootType)
        => new(
            rootType,
            null,
            SchemaSet.Empty,
            null,
            null,
            null,
            !ReferenceEquals(rootType, _schema.QueryType) && _viewerFallbackTypes.Contains(rootType),
            HolderReentry: false);

    /// <summary>
    /// Restricts <paramref name="buffer"/> to the schemas that the visit of <paramref name="type"/>,
    /// held through <paramref name="item"/>, and its children read, adds
    /// <paramref name="transitionFacts"/> and the other schemas the type can be entered in through a
    /// lookup whose key <paramref name="anchor"/> resolves, and returns the resulting set.
    /// </summary>
    private SchemaSet CompleteReach(
        MutableObjectTypeDefinition type,
        VisitTypeInfo info,
        SatisfiabilityPathItem item,
        SchemaSet transitionFacts,
        ulong[] buffer,
        ReachAnchor anchor)
    {
        var holder = _schemaSets.GetIndex(item.SchemaName);
        _schemaSets.IntersectWith(buffer, info.Schemas);

        // Keeps the holding schema only when a field returning a composite type exists in it.
        var readsHolder = _schemaSets.Contains(info.CompositeFieldSchemas, holder);

        if (!readsHolder)
        {
            SchemaSetTable.Remove(buffer, holder);
        }

        _schemaSets.UnionWith(buffer, transitionFacts);
        AddLookupEntries(type, info, item, buffer, readsHolder ? -1 : holder, anchor);

        return _schemaSets.Intern(buffer);
    }

    /// <summary>
    /// Adds to <paramref name="buffer"/> the schemas, other than the one with index
    /// <paramref name="skippedIndex"/>, that <paramref name="type"/> can be entered in through a
    /// lookup whose key <paramref name="anchor"/> resolves from <paramref name="item"/>, also when the
    /// key reads a schema that another of these lookups adds.
    /// </summary>
    private void AddLookupEntries(
        MutableObjectTypeDefinition type,
        VisitTypeInfo info,
        SatisfiabilityPathItem item,
        ulong[] buffer,
        int skippedIndex,
        ReachAnchor anchor)
    {
        bool added;

        do
        {
            added = false;

            foreach (var index in info.LookupSchemas)
            {
                if (index == skippedIndex
                    || SchemaSetTable.Contains(buffer, index)
                    || anchor.IsExcluded(index))
                {
                    continue;
                }

                if (CanEnterWithLookup(type, _schemaSets.GetSchemaName(index), item, anchor))
                {
                    SchemaSetTable.Add(buffer, index);
                    added = true;
                }
            }
        }
        while (added);
    }

    /// <summary>
    /// Creates the schemas, among the holding schema and those of <paramref name="reach"/>, that hold the
    /// object of <paramref name="item"/> without the data of the exclusion of <paramref name="anchor"/>
    /// only through calls that depend on a call re-rooted at <c>Query.viewer</c>, given the reach of the
    /// parent of <paramref name="parentKey"/> without the same data.
    /// </summary>
    private SchemaSet CreateViewerOnly(
        MutableObjectTypeDefinition type,
        VisitTypeInfo info,
        SatisfiabilityPathItem item,
        VisitKey parentKey,
        ExclusionReach parentReach,
        SchemaSet reach,
        bool holderAvailable,
        ReachAnchor anchor)
    {
        if (parentReach.ViewerOnly == SchemaSet.Empty && !IsViewerFallback(item, parentKey))
        {
            return SchemaSet.Empty;
        }

        var holder = _schemaSets.GetIndex(item.SchemaName);
        var fieldSchemas = GetFieldSchemas(item.Field);
        var candidates = _schemaSets.CreateBuffer(reach);
        var independent = _schemaSets.CreateBuffer(SchemaSet.Empty);

        if (holderAvailable)
        {
            SchemaSetTable.Add(candidates, holder);
        }

        // A schema that holds the parent without depending on a re-rooted call, and serves the item's
        // field, re-traverses it without depending on one. A schema that only the re-rooted viewer field
        // adds depends on its call.
        for (var word = 0; word < candidates.Length; word++)
        {
            for (var bits = candidates[word]; bits != 0; bits &= bits - 1)
            {
                var index = (word << 6) + BitOperations.TrailingZeroCount(bits);

                if (parentKey.SchemaName is null
                    ? _schemaSets.Contains(fieldSchemas, index)
                    : (_schemaSets.Contains(parentReach.Reach, index)
                            || (parentKey.SchemaName == _schemaSets.GetSchemaName(index)
                                && parentReach.HolderAvailable))
                        && !_schemaSets.Contains(parentReach.ViewerOnly, index)
                        && (_schemaSets.Contains(fieldSchemas, index)
                            || (parentReach.ReachProvides?.Find(index) is { } selectionSet
                                && SourceSchemaTransitionHelper.ServesOnRetraversal(
                                    _schema,
                                    item.Field,
                                    item.Type,
                                    _schemaSets.GetSchemaName(index),
                                    selectionSet,
                                    fieldProvides: null,
                                    out _))))
                {
                    SchemaSetTable.Add(independent, index);
                }
            }
        }

        // A lookup enters the type without depending on a re-rooted call when its key resolves from the
        // schemas that hold the object without depending on one.
        var independentAnchor = anchor.WithoutReRootedCalls(
            independent,
            SchemaSetTable.Contains(independent, holder));
        bool added;

        do
        {
            added = false;

            foreach (var index in info.LookupSchemas)
            {
                if (SchemaSetTable.Contains(candidates, index)
                    && !SchemaSetTable.Contains(independent, index)
                    && CanEnterWithLookup(type, _schemaSets.GetSchemaName(index), item, independentAnchor))
                {
                    SchemaSetTable.Add(independent, index);
                    added = true;
                }
            }
        }
        while (added);

        for (var word = 0; word < candidates.Length; word++)
        {
            candidates[word] &= ~independent[word];
        }

        return _schemaSets.Intern(candidates);
    }

    /// <summary>
    /// Determines whether a new call can enter the holding schema at <paramref name="type"/>, held
    /// through <paramref name="item"/>, whose selection set the holding call provides: through a lookup of
    /// the type, or by entering the holding schema at the parent of <paramref name="parentKey"/> and
    /// re-traversing the item's field.
    /// </summary>
    private bool CanReenterHolder(
        MutableObjectTypeDefinition type,
        SatisfiabilityPathItem item,
        VisitKey parentKey,
        ReachAnchor anchor)
    {
        var holder = item.SchemaName;

        if (HasSatisfiableLookup(type, holder, item, excludeSchemaName: null, anchor))
        {
            return true;
        }

        if (parentKey.SchemaName is null
            || !item.Field.ExistsInSchema(holder)
            || item.Field.IsPartial(holder))
        {
            return false;
        }

        // A parent held through a provided selection set is entered again in the same way; any other
        // parent is held by a call in each schema of its reach.
        return parentKey.Leaf?.ProvidedSelectionSet is not null
            ? parentKey.SchemaName == holder && parentKey.HolderReentry
            : parentKey.SchemaName == holder || _schemaSets.Contains(parentKey.Reach, holder);
    }

    private VisitTypeInfo GetVisitTypeInfo(MutableObjectTypeDefinition type)
    {
        if (_visitTypeInfos.TryGetValue(type, out var info))
        {
            return info;
        }

        _schemaIndexes.Clear();
        _compositeFieldSchemaIndexes.Clear();
        var readsLeaf = false;

        foreach (var field in type.Fields)
        {
            var isCompositeField =
                field.Type.AsTypeDefinition() is MutableComplexTypeDefinition or MutableUnionTypeDefinition;
            var directives = field.Directives;

            for (var i = 0; i < directives.Count; i++)
            {
                var directive = directives[i];
                var isFieldDirective = directive.Name == WellKnownDirectiveNames.FusionField;

                if (!isFieldDirective && directive.Name != WellKnownDirectiveNames.FusionEventStream)
                {
                    continue;
                }

                var schemaName = (string)directive.Arguments[WellKnownArgumentNames.Schema].Value!;
                var index = _schemaSets.GetIndex(schemaName);
                _schemaIndexes.Add(index);
                readsLeaf = readsLeaf || ReadsLeaf(type, field, schemaName);

                if (isFieldDirective && isCompositeField)
                {
                    _compositeFieldSchemaIndexes.Add(index);
                }
            }
        }

        var schemas = _schemaSets.Create(_schemaIndexes);
        var compositeFieldSchemas = _schemaSets.Create(_compositeFieldSchemaIndexes);
        var members = _schemaSets.GetMembers(schemas);
        _schemaIndexes.Clear();

        // Only the schemas named by the type's lookups can have a lookup for the type.
        _lookupSchemaNames.Clear();

        foreach (var lookup in _lookupCache.GetPossibleFusionLookupDirectives(type))
        {
            _lookupSchemaNames.Add((string)lookup.Arguments[WellKnownArgumentNames.Schema].Value!);
        }

        for (var word = 0; word < members.Length && _lookupSchemaNames.Count > 0; word++)
        {
            for (var bits = members[word]; bits != 0; bits &= bits - 1)
            {
                var index = (word << 6) + BitOperations.TrailingZeroCount(bits);
                var schemaName = _schemaSets.GetSchemaName(index);

                if (_lookupSchemaNames.Contains(schemaName)
                    && _lookupCache.GetPossibleFusionLookupDirectives(type, schemaName).Count > 0)
                {
                    _schemaIndexes.Add(index);
                }
            }
        }

        info = new VisitTypeInfo(schemas, compositeFieldSchemas, [.. _schemaIndexes], readsLeaf);
        _visitTypeInfos.Add(type, info);
        return info;
    }

    /// <summary>
    /// Gets the schemas, other than <paramref name="schemaName"/>, that the facts confirm
    /// <paramref name="type"/> held in <paramref name="schemaName"/> can transition to.
    /// </summary>
    private SchemaSet GetTransitionFacts(
        MutableObjectTypeDefinition type,
        VisitTypeInfo info,
        string schemaName)
    {
        if (!_transitionFacts.TryGetValue((type, schemaName), out var transitionFacts))
        {
            _schemaIndexes.Clear();

            foreach (var index in info.LookupSchemas)
            {
                var targetSchemaName = _schemaSets.GetSchemaName(index);

                if (targetSchemaName != schemaName && _facts.CanTransition(type, targetSchemaName, schemaName))
                {
                    _schemaIndexes.Add(index);
                }
            }

            transitionFacts = _schemaSets.Create(_schemaIndexes);
            _transitionFacts.Add((type, schemaName), transitionFacts);
        }

        return transitionFacts;
    }

    private bool ReadsLeaf(
        MutableObjectTypeDefinition type,
        MutableOutputFieldDefinition field,
        string schemaName)
        => !_facts.IsFieldResolvableOn(type, field, schemaName)
            && field.GetFusionRequiresRequirements(schemaName) is not null;

    private SchemaSet GetFieldSchemas(MutableOutputFieldDefinition field)
    {
        if (!_fieldSchemas.TryGetValue(field, out var schemas))
        {
            _schemaIndexes.Clear();
            var directives = field.Directives;

            for (var i = 0; i < directives.Count; i++)
            {
                if (directives[i].Name == WellKnownDirectiveNames.FusionField)
                {
                    var schemaName = (string)directives[i].Arguments[WellKnownArgumentNames.Schema].Value!;

                    // A partial (@external) field is not served by its schema.
                    if (!field.IsPartial(schemaName))
                    {
                        _schemaIndexes.Add(_schemaSets.GetIndex(schemaName));
                    }
                }
            }

            schemas = _schemaSets.Create(_schemaIndexes);
            _fieldSchemas.Add(field, schemas);
        }

        return schemas;
    }

    /// <summary>
    /// Gets the @provides selection sets of <paramref name="field"/> by schema, or <c>null</c> if it
    /// has none.
    /// </summary>
    private SchemaSelectionSets? GetFieldProvides(MutableOutputFieldDefinition field)
    {
        if (!_fieldProvides.TryGetValue(field, out var provides))
        {
            List<(int SchemaIndex, SelectionSetNode SelectionSet)>? entries = null;

            foreach (var schemaName in field.GetSchemaNames())
            {
                if (SourceSchemaTransitionHelper.GetProvidedSelectionSet(field, schemaName) is { } selectionSet)
                {
                    (entries ??= []).Add((_schemaSets.GetIndex(schemaName), selectionSet));
                }
            }

            provides = entries is null ? null : new SchemaSelectionSets(entries);
            _fieldProvides.Add(field, provides);
        }

        return provides;
    }

    /// <summary>
    /// Determines whether <paramref name="type"/> has a lookup in <paramref name="schemaName"/> whose key
    /// <paramref name="anchor"/> resolves from <paramref name="item"/> without data from that schema.
    /// </summary>
    private bool CanEnterWithLookup(
        MutableObjectTypeDefinition type,
        string schemaName,
        SatisfiabilityPathItem item,
        ReachAnchor anchor)
    {
        if (item.ProvidedSelectionSet is not null || anchor.HasProvides)
        {
            return HasSatisfiableLookup(type, schemaName, item, excludeSchemaName: schemaName, anchor);
        }

        var cacheKey = anchor.CreateLookupEntryKey(schemaName);

        if (!_lookupEntries.TryGetValue(cacheKey, out var canEnter))
        {
            canEnter = HasSatisfiableLookup(type, schemaName, item, excludeSchemaName: schemaName, anchor);
            _lookupEntries.Add(cacheKey, canEnter);
        }

        return canEnter;
    }

    /// <summary>
    /// Determines whether <paramref name="type"/> has a lookup in <paramref name="schemaName"/> whose key
    /// <paramref name="anchor"/> resolves from <paramref name="item"/>, without data from
    /// <paramref name="excludeSchemaName"/> unless it is <c>null</c>.
    /// </summary>
    private bool HasSatisfiableLookup(
        MutableObjectTypeDefinition type,
        string schemaName,
        SatisfiabilityPathItem item,
        string? excludeSchemaName,
        ReachAnchor anchor)
    {
        foreach (var lookup in _lookupCache.GetPossibleFusionLookupDirectives(type, schemaName))
        {
            var key = _lookupCache.GetLookupKey(lookup);

            // The facts are computed with the data of every schema available, from the holding call.
            if ((item.ProvidedSelectionSet is null
                    && anchor.UnavailableSchemaNames.IsEmpty
                    && anchor.IsHolderAvailable(anchor.UnavailableSchemaNames)
                    && CanResolveLeafSelections(key, type, item.SchemaName, excludeSchemaName))
                || _requirementsValidator.Validate(
                    key,
                    type,
                    item,
                    ancestors: [],
                    new RequirementScope(anchor, ArrivalPath: null, anchor.AllowsReRootedCalls),
                    excludeSchemaName,
                    _decisionBudget,
                    unavailableSchemaNames: anchor.UnavailableSchemaNames).IsEmpty)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether the facts confirm that every selection of <paramref name="selectionSet"/>
    /// is a leaf field of <paramref name="type"/> resolvable while the type is held in
    /// <paramref name="fromSchemaName"/>, without data from <paramref name="excludeSchemaName"/>
    /// unless it is <c>null</c>.
    /// </summary>
    private bool CanResolveLeafSelections(
        SelectionSetNode selectionSet,
        MutableObjectTypeDefinition type,
        string fromSchemaName,
        string? excludeSchemaName)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is not FieldNode { SelectionSet: null } fieldNode
                || !type.Fields.TryGetField(fieldNode.Name.Value, out var field)
                || !CanResolveField(field))
            {
                return false;
            }
        }

        return true;

        bool CanResolveField(MutableOutputFieldDefinition field)
        {
            var directives = field.Directives;

            for (var i = 0; i < directives.Count; i++)
            {
                if (directives[i].Name != WellKnownDirectiveNames.FusionField)
                {
                    continue;
                }

                var schemaName = (string)directives[i].Arguments[WellKnownArgumentNames.Schema].Value!;

                if (schemaName != excludeSchemaName
                    && _facts.IsFieldResolvableOn(type, field, schemaName)
                    && (schemaName == fromSchemaName || _facts.CanTransition(type, schemaName, fromSchemaName)))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Gets the reach of <paramref name="key"/> without the data of <paramref name="unavailableSchemaNames"/>,
    /// or <c>null</c> if the key has none, which fails the check that asks for it.
    /// </summary>
    private ExclusionReach? FindExclusionReach(
        VisitKey key,
        ExclusionReaches? exclusions,
        ImmutableHashSet<string> unavailableSchemaNames)
    {
        var exclusion = FindExclusion(key.ObjectType, unavailableSchemaNames);
        var entry = exclusion is null ? null : exclusions?.Find(exclusion.Schemas);

        Debug.Assert(
            entry is not null,
            $"The visit of '{key.ObjectType.Name}' has no reach without "
            + $"'{string.Join(",", unavailableSchemaNames.Order(StringComparer.Ordinal))}'.");

        return entry;
    }

    /// <summary>
    /// Identifies whether a type can be entered in a schema through a lookup from a path item, given
    /// everything the key walk reads from the anchor of the reach being created.
    /// </summary>
    private readonly record struct LookupEntryKey(
        MutableObjectTypeDefinition Type,
        string SchemaName,
        SatisfiabilityPathItem Item,
        SchemaSet Reach,
        SchemaSet Unavailable,
        bool HolderAvailable,
        bool ViewerFallback,
        bool AllowsReRootedCalls,
        ExclusionReaches? Exclusions);

    /// <summary>
    /// The anchor of a key walk while the reach of a visit key is created. It answers for the reach
    /// being created from the schemas added so far, and for the reaches without the data of more schemas
    /// from those created before.
    /// </summary>
    private sealed class ReachAnchor(
        SatisfiabilityValidator validator,
        MutableObjectTypeDefinition type,
        SatisfiabilityPathItem item,
        bool viewerFallback,
        Exclusion? exclusion,
        ulong[] buffer,
        List<(int SchemaIndex, SelectionSetNode SelectionSet)>? provides,
        ExclusionReaches? exclusions) : PathAnchor
    {
        public override SatisfiabilityPathItem Item => item;

        public override bool ReroutesViewerFields => viewerFallback && AllowsReRootedCalls;

        /// <summary>
        /// Gets whether the call holding the object was made without the unavailable data, before the
        /// reach adds the holding schema.
        /// </summary>
        public bool HolderAvailable { get; init; } = exclusion is null;

        /// <summary>
        /// Gets whether the walk may take data that depends on calls re-rooted at <c>Query.viewer</c>, which
        /// the reach being created holds otherwise only without such calls.
        /// </summary>
        public bool AllowsReRootedCalls { get; private init; } = true;

        public bool HasProvides => provides is not null;

        public ImmutableHashSet<string> UnavailableSchemaNames => exclusion?.SchemaNames ?? [];

        /// <summary>
        /// Determines whether the reach being created leaves out the schema with the given index.
        /// </summary>
        public bool IsExcluded(int index)
            => exclusion is not null
                && (!validator._schemaSets.Contains(exclusion.Relevant, index)
                    || validator._schemaSets.Contains(exclusion.Schemas, index));

        /// <summary>
        /// Creates an anchor for the same reach that holds the object in the schemas of
        /// <paramref name="independentBuffer"/>, and in the holding schema when
        /// <paramref name="holderAvailable"/> is set, and takes no data that depends on calls re-rooted
        /// at <c>Query.viewer</c>.
        /// </summary>
        public ReachAnchor WithoutReRootedCalls(ulong[] independentBuffer, bool holderAvailable)
            => new(validator, type, item, viewerFallback, exclusion, independentBuffer, provides, exclusions)
            {
                HolderAvailable = holderAvailable,
                AllowsReRootedCalls = false
            };

        public LookupEntryKey CreateLookupEntryKey(string schemaName)
            => new(
                type,
                schemaName,
                item,
                validator._schemaSets.Intern(buffer),
                exclusion?.Schemas ?? SchemaSet.Empty,
                IsCurrentHolderAvailable(),
                viewerFallback,
                AllowsReRootedCalls,
                exclusions);

        public override bool CanHold(string schemaName, ImmutableHashSet<string> unavailableSchemaNames)
        {
            var schemaSets = validator._schemaSets;

            if (IsCurrent(unavailableSchemaNames))
            {
                return (exclusion is null && HolderAvailable && schemaName == item.SchemaName)
                    || SchemaSetTable.Contains(buffer, schemaSets.GetIndex(schemaName));
            }

            return GetExclusionReach(unavailableSchemaNames) is { } entry
                && schemaSets.Contains(entry.Reach, schemaName)
                && (AllowsReRootedCalls || !schemaSets.Contains(entry.ViewerOnly, schemaName));
        }

        public override SelectionSetNode? GetProvidedSelectionSet(
            string schemaName,
            ImmutableHashSet<string> unavailableSchemaNames)
        {
            var index = validator._schemaSets.GetIndex(schemaName);

            if (!IsCurrent(unavailableSchemaNames))
            {
                return GetExclusionReach(unavailableSchemaNames)?.ReachProvides?.Find(index);
            }

            if (provides is not null)
            {
                foreach (var entry in provides)
                {
                    if (entry.SchemaIndex == index)
                    {
                        return entry.SelectionSet;
                    }
                }
            }

            return null;
        }

        public override bool IsHolderAvailable(ImmutableHashSet<string> unavailableSchemaNames)
        {
            if (IsCurrent(unavailableSchemaNames))
            {
                return IsCurrentHolderAvailable();
            }

            return GetExclusionReach(unavailableSchemaNames) is { } entry
                && entry.HolderAvailable
                && (AllowsReRootedCalls || !validator._schemaSets.Contains(entry.ViewerOnly, item.SchemaName));
        }

        // Answers for the reaches created before. An anchor that takes no data that depends on re-rooted
        // calls holds the object in the reach being created only in schemas without such calls.
        public override bool IsViewerOnly(string schemaName, ImmutableHashSet<string> unavailableSchemaNames)
            => !IsCurrent(unavailableSchemaNames)
                && GetExclusionReach(unavailableSchemaNames) is { } entry
                && validator._schemaSets.Contains(entry.ViewerOnly, schemaName);

        private bool IsCurrentHolderAvailable()
            => HolderAvailable
                || SchemaSetTable.Contains(buffer, validator._schemaSets.GetIndex(item.SchemaName));

        private bool IsCurrent(ImmutableHashSet<string> unavailableSchemaNames)
            => exclusion is null
                ? unavailableSchemaNames.IsEmpty
                : exclusion.Matches(unavailableSchemaNames);

        private ExclusionReach? GetExclusionReach(ImmutableHashSet<string> unavailableSchemaNames)
        {
            var other = validator.FindExclusion(type, unavailableSchemaNames);
            var entry = other is null ? null : exclusions?.Find(other.Schemas);

            Debug.Assert(
                entry is not null,
                $"The reach of '{type.Name}' was created before the reach without "
                + $"'{string.Join(",", unavailableSchemaNames.Order(StringComparer.Ordinal))}'.");

            return entry;
        }
    }

    /// <summary>
    /// The anchor of a key walk at the object of a visit, which answers from the visit key.
    /// </summary>
    private sealed class VisitAnchor(SatisfiabilityValidator validator, VisitKey key, SatisfiabilityPathItem item)
        : PathAnchor
    {
        public override SatisfiabilityPathItem Item => item;

        public override bool ReroutesViewerFields => key.ViewerFallback;

        public override bool CanHold(string schemaName, ImmutableHashSet<string> unavailableSchemaNames)
        {
            if (unavailableSchemaNames.IsEmpty)
            {
                return schemaName == key.SchemaName || validator._schemaSets.Contains(key.Reach, schemaName);
            }

            return validator.FindExclusionReach(key, key.Exclusions, unavailableSchemaNames) is { } entry
                && validator._schemaSets.Contains(entry.Reach, schemaName);
        }

        public override SelectionSetNode? GetProvidedSelectionSet(
            string schemaName,
            ImmutableHashSet<string> unavailableSchemaNames)
        {
            var index = validator._schemaSets.GetIndex(schemaName);

            return unavailableSchemaNames.IsEmpty
                ? key.ReachProvides?.Find(index)
                : validator.FindExclusionReach(key, key.Exclusions, unavailableSchemaNames)?.ReachProvides?.Find(index);
        }

        public override bool IsHolderAvailable(ImmutableHashSet<string> unavailableSchemaNames)
            => unavailableSchemaNames.IsEmpty
                || validator.FindExclusionReach(key, key.Exclusions, unavailableSchemaNames)?.HolderAvailable == true;

        // Answers for a reach without the data of some schemas, the only reach that a walk taking no data
        // that depends on re-rooted calls asks for.
        public override bool IsViewerOnly(string schemaName, ImmutableHashSet<string> unavailableSchemaNames)
            => validator.FindExclusionReach(key, key.Exclusions, unavailableSchemaNames) is { } entry
                && validator._schemaSets.Contains(entry.ViewerOnly, schemaName);
    }

    /// <summary>
    /// The reach of a node lookup entry and its reaches without the data of some schemas.
    /// </summary>
    private readonly record struct NodeReaches(SchemaSet Reach, ExclusionReaches? Exclusions);

    /// <summary>
    /// The schemas of an object type's fields, the schemas of its fields that return a composite
    /// type, the schemas with a lookup for the type, and whether a visit validates a requirement
    /// from the holding path item.
    /// </summary>
    private sealed record VisitTypeInfo(
        SchemaSet Schemas,
        SchemaSet CompositeFieldSchemas,
        int[] LookupSchemas,
        bool ReadsLeaf);
}
