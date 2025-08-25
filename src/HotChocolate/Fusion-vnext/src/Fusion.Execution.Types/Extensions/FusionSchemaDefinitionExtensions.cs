using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Provides extension methods for <see cref="ISchemaDefinition"/>.
/// </summary>
public static class FusionSchemaDefinitionExtensions
{
    private static readonly ThreadLocal<List<FusionUnionTypeDefinition>> s_threadLocalUnionTypes = new(() => []);

    internal static SchemaEnvironment? TryGetEnvironment(this ISchemaDefinition schema)
        => schema.Features.Get<SchemaEnvironment>();

    internal static ImmutableArray<Lookup> GetPossibleLookups(
        this FusionSchemaDefinition compositeSchema,
        ITypeDefinition type,
        string? schemaName = null)
    {
        var unionTypes = s_threadLocalUnionTypes.Value!;
        unionTypes.Clear();

        foreach (var unionType in compositeSchema.GetAllUnionTypes())
        {
            if (unionType.Types.ContainsName(type.Name))
            {
                unionTypes.Add(unionType);
            }
        }

        // TODO: Currently we just check that the type exists in the given source schema
        //       and that there are lookups for itself and / or the abstract types
        //       it's a part of. However, we don't check that the type is part of the
        //       abstract type in the given source schema.
        if (type is FusionComplexTypeDefinition complexType)
        {
            var lookups = ImmutableArray.CreateBuilder<Lookup>();

            foreach (var source in complexType.Sources)
            {
                CollectLookups(schemaName, lookups, source.Lookups);

                foreach (var interfaceType in complexType.Implements)
                {
                    if (interfaceType.Sources.TryGetMember(source.SchemaName, out var interfaceSource))
                    {
                        CollectLookups(schemaName, lookups, interfaceSource.Lookups);
                    }
                }

                foreach (var unionType in unionTypes)
                {
                    if (unionType.Sources.TryGetMember(source.SchemaName, out var unionSource))
                    {
                        CollectLookups(schemaName, lookups, unionSource.Lookups);
                    }
                }
            }

            return lookups.ToImmutable();
        }

        return [];

        static void CollectLookups(
            string? schemaName,
            ImmutableArray<Lookup>.Builder selectedLookups,
            ImmutableArray<Lookup> possibleLookups)
        {
            if (schemaName is not null)
            {
                foreach (var lookup in possibleLookups)
                {
                    if (lookup.SchemaName.Equals(schemaName, StringComparison.Ordinal))
                    {
                        selectedLookups.Add(lookup);
                    }
                }
            }
            else
            {
                selectedLookups.AddRange(possibleLookups);
            }
        }
    }
}
