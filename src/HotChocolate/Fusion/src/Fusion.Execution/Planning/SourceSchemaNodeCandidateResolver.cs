using System.Collections.Immutable;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

internal sealed class SourceSchemaNodeCandidateResolver(FusionSchemaDefinition schema)
{
    public ImmutableDictionary<string, ImmutableHashSet<string>> Resolve(
        FieldNode rootFieldNode,
        FusionComplexTypeDefinition abstractType)
    {
        var candidatesByType = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var (schemaName, sourceTypeName) in GetCandidates(rootFieldNode, abstractType))
        {
            foreach (var typeName in GetSourceLocalRuntimeTypes(
                abstractType,
                schemaName,
                sourceTypeName))
            {
                if (!candidatesByType.TryGetValue(typeName, out var candidates))
                {
                    candidates = new HashSet<string>(StringComparer.Ordinal);
                    candidatesByType.Add(typeName, candidates);
                }

                candidates.Add(schemaName);
            }
        }

        var result = ImmutableDictionary.CreateBuilder<string, ImmutableHashSet<string>>(
            StringComparer.Ordinal);

        foreach (var (typeName, candidateNames) in candidatesByType)
        {
            var candidates = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
            candidates.UnionWith(candidateNames);
            result.Add(typeName, candidates.ToImmutable());
        }

        return result.ToImmutable();
    }

    private SortedDictionary<string, string?> GetCandidates(
        FieldNode rootFieldNode,
        FusionComplexTypeDefinition abstractType)
    {
        var rootField = schema.QueryType.Fields.GetField(
            rootFieldNode.Name.Value,
            allowInaccessibleFields: true);
        var candidates = new SortedDictionary<string, string?>(StringComparer.Ordinal);

        foreach (var rootFieldSource in rootField.Sources)
        {
            if (!rootFieldSource.IsExternal)
            {
                candidates.TryAdd(
                    rootFieldSource.SchemaName,
                    rootFieldSource.SourceTypeName);
            }
        }

        foreach (var possibleType in schema.GetPossibleTypes(
            abstractType,
            includeInaccessible: true))
        {
            foreach (var lookup in schema.GetPossibleLookupsOrdered(possibleType))
            {
                if (!lookup.IsInternal
                    && lookup.FieldName.Equals(rootField.Name, StringComparison.Ordinal)
                    && lookup.FieldType == abstractType
                    && lookup.Path.IsDefaultOrEmpty)
                {
                    candidates.TryAdd(lookup.SchemaName, abstractType.Name);
                }
            }
        }

        return candidates;
    }

    private HashSet<string> GetSourceLocalRuntimeTypes(
        FusionComplexTypeDefinition abstractType,
        string schemaName,
        string? sourceTypeName)
    {
        var applicableTypes = new HashSet<string>(StringComparer.Ordinal);

        if (sourceTypeName is not null
            && schema.Types.TryGetType(
                sourceTypeName,
                allowInaccessibleFields: true,
                out var sourceType))
        {
            if (sourceType is FusionObjectTypeDefinition objectType)
            {
                applicableTypes.Add(objectType.Name);
                return applicableTypes;
            }

            if (sourceType is FusionComplexTypeDefinition sourceAbstractType)
            {
                abstractType = sourceAbstractType;
            }
        }

        foreach (var possibleType in schema.GetPossibleTypes(
            abstractType,
            includeInaccessible: true))
        {
            if (IsSourceLocalPossibleType(abstractType, possibleType, schemaName))
            {
                applicableTypes.Add(possibleType.Name);
            }
        }

        return applicableTypes;
    }

    private static bool IsSourceLocalPossibleType(
        FusionComplexTypeDefinition abstractType,
        FusionObjectTypeDefinition possibleType,
        string schemaName)
    {
        if (!possibleType.Sources.TryGetMember(schemaName, out var possibleTypeSource))
        {
            return false;
        }

        return abstractType is FusionInterfaceTypeDefinition
            && possibleTypeSource.Implements.Contains(abstractType.Name);
    }
}
