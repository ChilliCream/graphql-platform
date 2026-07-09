using HotChocolate.Fusion.Extensions;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using BooleanValueNode = HotChocolate.Language.BooleanValueNode;
using ListValueNode = HotChocolate.Language.ListValueNode;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// Caches possible <c>@fusion__lookup</c> directives for a merged schema.
/// </summary>
internal sealed class FusionLookupDirectiveCache
{
    private readonly MutableSchemaDefinition _schema;
    private readonly Dictionary<MutableComplexTypeDefinition, List<MutableUnionTypeDefinition>> _unionsByMember = [];
    private readonly Dictionary<(MutableComplexTypeDefinition Type, string? SchemaName), List<IDirective>> _lookups = [];
    private readonly Dictionary<(MutableComplexTypeDefinition Type, string? SchemaName), List<IDirective>> _lookupsById = [];

    /// <summary>
    /// Initializes a new lookup directive cache for <paramref name="schema"/>.
    /// </summary>
    /// <param name="schema">The merged schema whose lookup directives are cached.</param>
    public FusionLookupDirectiveCache(MutableSchemaDefinition schema)
    {
        _schema = schema;

        foreach (var unionType in schema.Types.OfType<MutableUnionTypeDefinition>())
        {
            foreach (var memberType in unionType.Types)
            {
                if (!_unionsByMember.TryGetValue(memberType, out var unionTypes))
                {
                    unionTypes = [];
                    _unionsByMember.Add(memberType, unionTypes);
                }

                unionTypes.Add(unionType);
            }
        }
    }

    /// <summary>
    /// Gets possible <c>@fusion__lookup</c> directives for <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The type whose possible lookup directives are requested.</param>
    /// <param name="schemaName">The optional source schema name used to filter lookups.</param>
    /// <returns>The possible lookup directives in declaration order.</returns>
    public List<IDirective> GetPossibleFusionLookupDirectives(
        MutableComplexTypeDefinition type,
        string? schemaName = null)
    {
        if (!string.IsNullOrEmpty(schemaName)
            && !type.ExistsInSchema(schemaName)
            && !MutableSchemaDefinitionExtensions.ReachesInterfaceObjectStandIn(_schema, type, schemaName))
        {
            return [];
        }

        var cacheKey = (type, schemaName);

        if (!_lookups.TryGetValue(cacheKey, out var lookups))
        {
            lookups = MutableSchemaDefinitionExtensions.GetPossibleFusionLookupDirectivesCore(
                _schema,
                type,
                schemaName,
                GetUnionsContainingType(type));

            _lookups.Add(cacheKey, lookups);
        }

        return lookups;
    }

    /// <summary>
    /// Gets possible non-internal <c>@fusion__lookup</c> directives that map by <c>id</c>.
    /// </summary>
    /// <param name="type">The type whose possible lookup directives are requested.</param>
    /// <param name="schemaName">The optional source schema name used to filter lookups.</param>
    /// <returns>The possible lookup directives by <c>id</c> in declaration order.</returns>
    public List<IDirective> GetPossibleFusionLookupDirectivesById(
        MutableComplexTypeDefinition type,
        string? schemaName = null)
    {
        var cacheKey = (type, schemaName);

        if (!_lookupsById.TryGetValue(cacheKey, out var lookupsById))
        {
            var lookups = GetPossibleFusionLookupDirectives(type, schemaName);
            lookupsById = [];

            foreach (var lookup in lookups)
            {
                if (lookup.Arguments[WellKnownArgumentNames.Map] is ListValueNode { Items.Count: 1 } mapArg
                    && mapArg.Items[0].Value?.Equals(WellKnownArgumentNames.Id) == true
                    && lookup.Arguments[WellKnownArgumentNames.Internal] is not BooleanValueNode { Value: true })
                {
                    lookupsById.Add(lookup);
                }
            }

            _lookupsById.Add(cacheKey, lookupsById);
        }

        return lookupsById;
    }

    private IReadOnlyList<MutableUnionTypeDefinition> GetUnionsContainingType(
        MutableComplexTypeDefinition type)
    {
        if (type.Kind == TypeKind.Object && _unionsByMember.TryGetValue(type, out var unionTypes))
        {
            return unionTypes;
        }

        return [];
    }
}
