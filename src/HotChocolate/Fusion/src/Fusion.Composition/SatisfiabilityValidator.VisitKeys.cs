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
    private readonly SchemaSetTable _schemaSets = new();
    private readonly Dictionary<MutableOutputFieldDefinition, SchemaSet> _fieldSchemas = [];
    private readonly Dictionary<MutableObjectTypeDefinition, VisitTypeInfo> _visitTypeInfos = [];
    private readonly Dictionary<(MutableObjectTypeDefinition, string), SchemaSet> _transitionFacts = [];
    private readonly Dictionary<(MutableObjectTypeDefinition, string), SchemaSet> _nodeReaches = [];
    private readonly Dictionary<(MutableObjectTypeDefinition, string, SatisfiabilityPathItem), bool>
        _lookupEntries = [];
    private readonly List<int> _schemaIndexes = [];
    private readonly List<int> _compositeFieldSchemaIndexes = [];

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
        var fieldSchemas = GetFieldSchemas(item.Field);
        var transitionFacts = item.ProvidedSelectionSet is null
            ? GetTransitionFacts(type, info, item.SchemaName)
            : SchemaSet.Empty;
        var holder = _schemaSets.GetIndex(item.SchemaName);
        var buffer = _schemaSets.GetBuffer(fieldSchemas);

        // Below a root type, keeps the schemas of the item's field that the parent reaches.
        if (parent.Path is not null)
        {
            _schemaSets.IntersectWith(buffer, parent.Key.Reach);
        }

        // A viewer field on a mutation or subscription path is re-rooted at Query.viewer in the
        // schemas it is missing in.
        if (parent.Key.ViewerFallback
            && _queryViewerField is not null
            && item.Field.IsViewerFieldFor(_queryViewerField))
        {
            foreach (var index in _queryViewerSchemas)
            {
                if (!_schemaSets.Contains(fieldSchemas, index))
                {
                    SchemaSetTable.Add(buffer, index);
                }
            }
        }

        var reach = CompleteReach(type, info, item, holder, transitionFacts, buffer);
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
            leaf,
            parent.Key.ViewerFallback && _viewerFallbackTypes.Contains(type));
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

        // The lookup field of a node item exists in no source schema, so the reach depends only on
        // the type and the lookup schema.
        if (!_nodeReaches.TryGetValue((type, lookupItem.SchemaName), out var reach))
        {
            var transitionFacts = GetTransitionFacts(type, info, lookupItem.SchemaName);
            var holder = _schemaSets.GetIndex(lookupItem.SchemaName);
            var buffer = _schemaSets.GetBuffer(SchemaSet.Empty);
            reach = CompleteReach(type, info, lookupItem, holder, transitionFacts, buffer);
            _nodeReaches.Add((type, lookupItem.SchemaName), reach);
        }

        var leaf = info.ReadsLeaf
            ? new VisitLeaf(lookupItem.Field, lookupItem.Type, null, false)
            : null;

        return new VisitKey(
            type,
            lookupItem.SchemaName,
            reach,
            leaf,
            parent.Key.ViewerFallback && _viewerFallbackTypes.Contains(type));
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
            !ReferenceEquals(rootType, _schema.QueryType) && _viewerFallbackTypes.Contains(rootType));

    /// <summary>
    /// Restricts <paramref name="buffer"/> to the schemas that the visit of <paramref name="type"/>,
    /// held in the schema with index <paramref name="holder"/>, and its children read, adds
    /// <paramref name="transitionFacts"/> and the other schemas the type can be entered in through a
    /// lookup from <paramref name="item"/>, and returns the resulting set.
    /// </summary>
    private SchemaSet CompleteReach(
        MutableObjectTypeDefinition type,
        VisitTypeInfo info,
        SatisfiabilityPathItem item,
        int holder,
        SchemaSet transitionFacts,
        ulong[] buffer)
    {
        _schemaSets.IntersectWith(buffer, info.Schemas);

        // Keeps the holding schema only when a field returning a composite type exists in it.
        var readsHolder = _schemaSets.Contains(info.CompositeFieldSchemas, holder);

        if (!readsHolder)
        {
            SchemaSetTable.Remove(buffer, holder);
        }

        _schemaSets.UnionWith(buffer, transitionFacts);

        foreach (var index in info.LookupSchemas)
        {
            if ((readsHolder || index != holder)
                && !SchemaSetTable.Contains(buffer, index)
                && CanEnterWithLookup(type, _schemaSets.GetSchemaName(index), item))
            {
                SchemaSetTable.Add(buffer, index);
            }
        }

        return _schemaSets.Intern(buffer);
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

        for (var word = 0; word < members.Length; word++)
        {
            for (var bits = members[word]; bits != 0; bits &= bits - 1)
            {
                var index = (word << 6) + BitOperations.TrailingZeroCount(bits);

                if (_lookupCache.GetPossibleFusionLookupDirectives(type, _schemaSets.GetSchemaName(index)).Count > 0)
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
                    _schemaIndexes.Add(_schemaSets.GetIndex(schemaName));
                }
            }

            schemas = _schemaSets.Create(_schemaIndexes);
            _fieldSchemas.Add(field, schemas);
        }

        return schemas;
    }

    /// <summary>
    /// Determines whether <paramref name="type"/> has a lookup in <paramref name="schemaName"/>
    /// whose key is satisfiable from <paramref name="item"/> without data from that schema.
    /// </summary>
    private bool CanEnterWithLookup(
        MutableObjectTypeDefinition type,
        string schemaName,
        SatisfiabilityPathItem item)
    {
        if (item.ProvidedSelectionSet is not null)
        {
            return HasSatisfiableLookup(type, schemaName, item, excludeSchemaName: schemaName);
        }

        var cacheKey = (type, schemaName, item);

        if (!_lookupEntries.TryGetValue(cacheKey, out var canEnter))
        {
            canEnter = HasSatisfiableLookup(type, schemaName, item, excludeSchemaName: schemaName);
            _lookupEntries.Add(cacheKey, canEnter);
        }

        return canEnter;
    }

    /// <summary>
    /// Determines whether <paramref name="type"/> has a lookup in <paramref name="schemaName"/>
    /// whose key is satisfiable from <paramref name="item"/>, without data from
    /// <paramref name="excludeSchemaName"/> unless it is <c>null</c>.
    /// </summary>
    private bool HasSatisfiableLookup(
        MutableObjectTypeDefinition type,
        string schemaName,
        SatisfiabilityPathItem item,
        string? excludeSchemaName)
    {
        foreach (var lookup in _lookupCache.GetPossibleFusionLookupDirectives(type, schemaName))
        {
            var key = _lookupCache.GetLookupKey(lookup);

            if ((item.ProvidedSelectionSet is null
                    && CanResolveLeafSelections(key, type, item.SchemaName, excludeSchemaName))
                || _requirementsValidator.Validate(key, type, item, excludeSchemaName).IsEmpty)
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
