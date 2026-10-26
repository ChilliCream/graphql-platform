using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

internal sealed partial class SatisfiabilityValidator
{
    private readonly HashSet<MutableObjectTypeDefinition> _viewerFallbackTypes = [];
    private readonly MutableOutputFieldDefinition? _queryViewerField;
    private readonly int[] _queryViewerSchemas;
    private Dictionary<MutableObjectTypeDefinition, List<ParentField>>? _parentFields;

    /// <summary>
    /// Gets the <c>Query.viewer</c> field of <paramref name="schema"/> that viewer fields on mutation and
    /// subscription paths are re-rooted at, or <c>null</c> if it has none.
    /// </summary>
    private static MutableOutputFieldDefinition? GetQueryViewerField(MutableSchemaDefinition schema)
        => schema.QueryType is { } queryType
            && queryType.Fields.TryGetField(WellKnownFieldNames.Viewer, out var queryViewerField)
            && !HasRequiredArguments(queryViewerField)
                ? queryViewerField
                : null;

    /// <summary>
    /// Finds the object types that reach a viewer field which is missing in one of the schemas of
    /// <c>Query.viewer</c>.
    /// </summary>
    private void InitializeViewerFallback()
    {
        _viewerFallbackTypes.Clear();

        if (_queryViewerField is not { } queryViewerField)
        {
            return;
        }

        var parents = GetParentFields();
        var pending = new Queue<MutableObjectTypeDefinition>();

        foreach (var objectType in _schema.Types.OfType<MutableObjectTypeDefinition>())
        {
            if (ReferenceEquals(objectType, _schema.QueryType))
            {
                continue;
            }

            foreach (var field in objectType.Fields)
            {
                if (CanFallBackToQueryViewer(field, queryViewerField) && _viewerFallbackTypes.Add(objectType))
                {
                    pending.Enqueue(objectType);
                }
            }
        }

        while (pending.TryDequeue(out var type))
        {
            if (!parents.TryGetValue(type, out var typeParents))
            {
                continue;
            }

            foreach (var (parent, _) in typeParents)
            {
                if (_viewerFallbackTypes.Add(parent))
                {
                    pending.Enqueue(parent);
                }
            }
        }
    }

    /// <summary>
    /// Gets, for each object type, the fields that return it, directly or through an interface or
    /// union, with the object types declaring them.
    /// </summary>
    private Dictionary<MutableObjectTypeDefinition, List<ParentField>> GetParentFields()
    {
        if (_parentFields is not null)
        {
            return _parentFields;
        }

        var objectTypes = _schema.Types.OfType<MutableObjectTypeDefinition>().ToArray();
        var implementations = new Dictionary<string, List<MutableObjectTypeDefinition>>(StringComparer.Ordinal);
        var parents = new Dictionary<MutableObjectTypeDefinition, List<ParentField>>();

        foreach (var objectType in objectTypes)
        {
            foreach (var interfaceType in objectType.Implements)
            {
                AddEdge(implementations, interfaceType.Name, objectType);
            }

            foreach (var directive in objectType.Directives[WellKnownDirectiveNames.FusionImplements])
            {
                AddEdge(
                    implementations,
                    (string)directive.Arguments[WellKnownArgumentNames.Interface].Value!,
                    objectType);
            }
        }

        foreach (var objectType in objectTypes)
        {
            foreach (var field in objectType.Fields)
            {
                switch (field.Type.AsTypeDefinition())
                {
                    case MutableObjectTypeDefinition fieldType:
                        AddEdge(parents, fieldType, new ParentField(objectType, field));
                        break;

                    case MutableInterfaceTypeDefinition fieldType
                        when implementations.TryGetValue(fieldType.Name, out var possibleTypes):
                        foreach (var possibleType in possibleTypes)
                        {
                            AddEdge(parents, possibleType, new ParentField(objectType, field));
                        }

                        break;

                    case MutableUnionTypeDefinition fieldType:
                        foreach (var possibleType in fieldType.Types)
                        {
                            AddEdge(parents, possibleType, new ParentField(objectType, field));
                        }

                        foreach (var directive in fieldType.Directives[WellKnownDirectiveNames.FusionUnionMember])
                        {
                            var memberName = (string)directive.Arguments[WellKnownArgumentNames.Member].Value!;

                            if (_schema.Types.TryGetType<MutableObjectTypeDefinition>(memberName, out var member))
                            {
                                AddEdge(parents, member, new ParentField(objectType, field));
                            }
                        }

                        break;
                }
            }
        }

        _parentFields = parents;
        return parents;

        static void AddEdge<TKey, TValue>(Dictionary<TKey, List<TValue>> edges, TKey key, TValue value)
            where TKey : notnull
        {
            if (!edges.TryGetValue(key, out var values))
            {
                values = [];
                edges.Add(key, values);
            }

            values.Add(value);
        }
    }

    /// <summary>
    /// Determines whether <paramref name="field"/> is re-rooted at <paramref name="queryViewerField"/>
    /// in at least one schema.
    /// </summary>
    private static bool CanFallBackToQueryViewer(
        MutableOutputFieldDefinition field,
        MutableOutputFieldDefinition queryViewerField)
    {
        if (!field.IsViewerFieldFor(queryViewerField))
        {
            return false;
        }

        foreach (var schemaName in queryViewerField.GetSchemaNames())
        {
            if (!field.ExistsInSchema(schemaName))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A field of <paramref name="Parent"/> that returns an object type.
    /// </summary>
    private readonly record struct ParentField(MutableObjectTypeDefinition Parent, MutableOutputFieldDefinition Field);

    private static bool HasRequiredArguments(MutableOutputFieldDefinition field)
    {
        foreach (var argument in field.Arguments)
        {
            if (argument.Type.Kind is TypeKind.NonNull && argument.DefaultValue is null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the <c>Query.viewer</c> field when <paramref name="pathFromLeaf"/> starts at a mutation
    /// or subscription root; otherwise <c>null</c>.
    /// </summary>
    private MutableOutputFieldDefinition? GetQueryViewerFieldForPath(
        IReadOnlyList<SatisfiabilityPathItem> pathFromLeaf)
        => pathFromLeaf.Count > 0 && !ReferenceEquals(pathFromLeaf[^1].Type, _schema.QueryType)
            ? _queryViewerField
            : null;
}
