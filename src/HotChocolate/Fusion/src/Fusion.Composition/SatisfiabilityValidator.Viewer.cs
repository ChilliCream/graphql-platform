using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

internal sealed partial class SatisfiabilityValidator
{
    private readonly HashSet<MutableObjectTypeDefinition> _viewerFallbackTypes = [];
    private MutableOutputFieldDefinition? _queryViewerField;
    private int[] _queryViewerSchemas = [];

    /// <summary>
    /// Finds the <c>Query.viewer</c> field that viewer fields on mutation and subscription paths
    /// are re-rooted at, and the object types that reach a viewer field missing in one of its schemas.
    /// </summary>
    private void InitializeViewerFallback()
    {
        _viewerFallbackTypes.Clear();
        _queryViewerField = null;
        _queryViewerSchemas = [];

        if (_schema.QueryType is not { } queryType
            || !queryType.Fields.TryGetField(WellKnownFieldNames.Viewer, out var queryViewerField)
            || HasRequiredArguments(queryViewerField))
        {
            return;
        }

        _queryViewerField = queryViewerField;
        _queryViewerSchemas = [.. queryViewerField.GetSchemaNames().Select(_schemaSets.GetIndex)];

        var objectTypes = _schema.Types.OfType<MutableObjectTypeDefinition>().ToArray();
        var implementations = new Dictionary<string, List<MutableObjectTypeDefinition>>(StringComparer.Ordinal);
        var parents = new Dictionary<MutableObjectTypeDefinition, List<MutableObjectTypeDefinition>>();
        var pending = new Queue<MutableObjectTypeDefinition>();

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
                if (!ReferenceEquals(objectType, queryType)
                    && CanFallBackToQueryViewer(field, queryViewerField)
                    && _viewerFallbackTypes.Add(objectType))
                {
                    pending.Enqueue(objectType);
                }

                switch (field.Type.AsTypeDefinition())
                {
                    case MutableObjectTypeDefinition fieldType:
                        AddEdge(parents, fieldType, objectType);
                        break;

                    case MutableInterfaceTypeDefinition fieldType
                        when implementations.TryGetValue(fieldType.Name, out var possibleTypes):
                        foreach (var possibleType in possibleTypes)
                        {
                            AddEdge(parents, possibleType, objectType);
                        }

                        break;

                    case MutableUnionTypeDefinition fieldType:
                        foreach (var possibleType in fieldType.Types)
                        {
                            AddEdge(parents, possibleType, objectType);
                        }

                        foreach (var directive in fieldType.Directives[WellKnownDirectiveNames.FusionUnionMember])
                        {
                            var memberName = (string)directive.Arguments[WellKnownArgumentNames.Member].Value!;

                            if (_schema.Types.TryGetType<MutableObjectTypeDefinition>(memberName, out var member))
                            {
                                AddEdge(parents, member, objectType);
                            }
                        }

                        break;
                }
            }
        }

        while (pending.TryDequeue(out var type))
        {
            if (!parents.TryGetValue(type, out var typeParents))
            {
                continue;
            }

            foreach (var parent in typeParents)
            {
                if (_viewerFallbackTypes.Add(parent))
                {
                    pending.Enqueue(parent);
                }
            }
        }

        static void AddEdge<TKey>(
            Dictionary<TKey, List<MutableObjectTypeDefinition>> edges,
            TKey key,
            MutableObjectTypeDefinition type)
            where TKey : notnull
        {
            if (!edges.TryGetValue(key, out var types))
            {
                types = [];
                edges.Add(key, types);
            }

            types.Add(type);
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
    private MutableOutputFieldDefinition? GetQueryViewerField(
        IReadOnlyList<SatisfiabilityPathItem> pathFromLeaf)
        => pathFromLeaf.Count > 0 && !ReferenceEquals(pathFromLeaf[^1].Type, _schema.QueryType)
            ? _queryViewerField
            : null;
}
