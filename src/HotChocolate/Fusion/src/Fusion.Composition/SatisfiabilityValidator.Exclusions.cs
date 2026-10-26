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

    /// <summary>
    /// Finds, for each object type, the sets of schemas whose data a requirement check at the type or
    /// below it may treat as unavailable, and for each set the schemas that such a check may ask to
    /// hold the type in.
    /// </summary>
    private void InitializeExclusions()
    {
        _exclusions.Clear();

        var relevant = new Dictionary<MutableObjectTypeDefinition, Dictionary<SchemaSet, HashSet<int>>>();
        var pending = new Queue<MutableObjectTypeDefinition>();
        var sets = new HashSet<SchemaSet>();
        var visited = new HashSet<(MutableObjectTypeDefinition, MutableOutputFieldDefinition, string, SchemaSet)>();
        var anchorSchemas = new HashSet<int>();

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

                    sets.Clear();
                    visited.Clear();
                    anchorSchemas.Clear();
                    CollectFirstLevelSchemas(requirements, type, anchorSchemas);
                    CollectExclusions(requirements, type, SchemaSet.Empty, true, sets, visited, anchorSchemas);

                    // A requiring field whose schema cannot always re-enter the type through a lookup is
                    // resolved without the data of its schema.
                    if (!_schema.IsRootOperationType(type)
                        && (isProvided || !HasSelfServedLookup(type, schemaName)))
                    {
                        var unavailable = AddToSchemaSet(SchemaSet.Empty, schemaName);
                        sets.Add(unavailable);
                        CollectExclusions(requirements, type, unavailable, true, sets, visited, anchorSchemas);
                    }

                    AddRelevantSchemas(relevant, type, sets, anchorSchemas, pending);
                }
            }

            // A lookup key that selects a requiring field makes the schema of that field unavailable.
            foreach (var lookup in _lookupCache.GetPossibleFusionLookupDirectives(type))
            {
                var key = _lookupCache.GetLookupKey(lookup);

                sets.Clear();
                visited.Clear();
                anchorSchemas.Clear();
                CollectFirstLevelSchemas(key, type, anchorSchemas);
                CollectExclusions(key, type, SchemaSet.Empty, true, sets, visited, anchorSchemas);
                AddRelevantSchemas(relevant, type, sets, anchorSchemas, pending);
            }
        }

        // A parent carries every set of its children, with the schemas that serve the field returning
        // the child.
        var parentFields = GetParentFields();

        while (pending.TryDequeue(out var type))
        {
            var typeRelevant = relevant[type];

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

        foreach (var (type, typeRelevant) in relevant)
        {
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
                            _schemaSets.Create([.. entry.Value])))
                ]);
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
    /// Adds to <paramref name="sets"/> the unavailable schemas of the requiring fields that
    /// <paramref name="selectionSet"/> selects on <paramref name="type"/> while the schemas of
    /// <paramref name="unavailable"/> are unavailable. When the selection set is validated at the
    /// object (<paramref name="atAnchor"/>), the schemas that serve the first level of a nested
    /// requirement are added to <paramref name="anchorSchemas"/>.
    /// </summary>
    private void CollectExclusions(
        SelectionSetNode selectionSet,
        MutableObjectTypeDefinition type,
        SchemaSet unavailable,
        bool atAnchor,
        HashSet<SchemaSet> sets,
        HashSet<(MutableObjectTypeDefinition, MutableOutputFieldDefinition, string, SchemaSet)> visited,
        HashSet<int> anchorSchemas)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode fieldNode when type.Fields.TryGetField(fieldNode.Name.Value, out var field):
                    foreach (var schemaName in field.GetSchemaNames())
                    {
                        if (field.GetFusionRequiresRequirements(schemaName) is not { } requirements
                            || !visited.Add((type, field, schemaName, unavailable)))
                        {
                            continue;
                        }

                        if (atAnchor)
                        {
                            CollectFirstLevelSchemas(requirements, type, anchorSchemas);
                        }

                        var extended = AddToSchemaSet(unavailable, schemaName);
                        sets.Add(extended);
                        CollectExclusions(requirements, type, unavailable, atAnchor, sets, visited, anchorSchemas);
                        CollectExclusions(requirements, type, extended, atAnchor, sets, visited, anchorSchemas);
                    }

                    if (fieldNode.SelectionSet is { } fieldSelectionSet
                        && field.Type.AsTypeDefinition() is MutableComplexTypeDefinition or MutableUnionTypeDefinition)
                    {
                        foreach (var possibleType in _schema.GetPossibleTypes(field.Type.AsTypeDefinition()))
                        {
                            CollectExclusions(
                                fieldSelectionSet,
                                possibleType,
                                unavailable,
                                atAnchor: false,
                                sets,
                                visited,
                                anchorSchemas);
                        }
                    }

                    break;

                case InlineFragmentNode { TypeCondition: null } inlineFragment:
                    CollectExclusions(inlineFragment.SelectionSet, type, unavailable, atAnchor, sets, visited, anchorSchemas);
                    break;

                case InlineFragmentNode inlineFragment
                    when _schema.Types.TryGetType(inlineFragment.TypeCondition.Name.Value, out var fragmentType)
                        && _schema.GetPossibleTypes(fragmentType).Contains(type):
                    CollectExclusions(inlineFragment.SelectionSet, type, unavailable, atAnchor, sets, visited, anchorSchemas);
                    break;
            }
        }
    }

    /// <summary>
    /// Creates the reaches of <paramref name="type"/>, held through <paramref name="item"/>, without the
    /// data of each set of the type's exclusions, or returns <c>null</c> if the type has no exclusions.
    /// Each reach extends the reach of <paramref name="parentKey"/> without the same data, or, when
    /// <paramref name="parentKey"/> is <c>null</c>, is created for a node lookup.
    /// </summary>
    private ExclusionReaches? CreateExclusionReaches(
        MutableObjectTypeDefinition type,
        VisitTypeInfo info,
        SatisfiabilityPathItem item,
        VisitKey? parentKey,
        bool viewerFallback)
    {
        if (!_exclusions.TryGetValue(type, out var exclusions))
        {
            return null;
        }

        var holder = _schemaSets.GetIndex(item.SchemaName);
        var entries = new ExclusionReach[exclusions.Length];

        for (var i = 0; i < exclusions.Length; i++)
        {
            var exclusion = exclusions[i];
            var buffer = _schemaSets.CreateBuffer(SchemaSet.Empty);
            List<(int SchemaIndex, SelectionSetNode SelectionSet)>? provides = null;

            // The call holding the object was made without the unavailable data when it is a root call,
            // a node lookup, or the call holding the parent in the same schema made without it.
            var holderAvailable = !_schemaSets.Contains(exclusion.Schemas, holder);
            var parentReach = new ExclusionReach(exclusion.Schemas, SchemaSet.Empty, null, false, SchemaSet.Empty);

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

            var anchor = new ReachAnchor(
                this,
                type,
                item,
                viewerFallback,
                exclusion,
                buffer,
                provides,
                i == 0 ? null : new ExclusionReaches(entries[..i]))
            {
                HolderAvailable = holderAvailable
            };

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
                    SchemaSet.Empty,
                    reach,
                    holderAvailable,
                    anchor);

            entries[i] = new ExclusionReach(
                exclusion.Schemas,
                reach,
                CreateReachProvides(provides, reach),
                holderAvailable,
                viewerOnly);
        }

        return new ExclusionReaches(entries);
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
        var buffer = _schemaSets.CreateBuffer(set);
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
    /// type without their data may ask to hold the type in.
    /// </summary>
    private sealed record Exclusion(SchemaSet Schemas, ImmutableHashSet<string> SchemaNames, SchemaSet Relevant)
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
}
