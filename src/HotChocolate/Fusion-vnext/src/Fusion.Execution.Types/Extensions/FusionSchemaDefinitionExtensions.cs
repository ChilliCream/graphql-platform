using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Provides extension methods for <see cref="ISchemaDefinition"/>.
/// </summary>
public static class FusionSchemaDefinitionExtensions
{
    internal static SchemaEnvironment? TryGetEnvironment(this ISchemaDefinition schema)
        => schema.Features.Get<SchemaEnvironment>();

    public static IEnumerable<Lookup> GetPossibleLookups(
        this FusionSchemaDefinition compositeSchema,
        ITypeDefinition type)
    {
        var unionTypes = compositeSchema.Types
            .OfType<FusionUnionTypeDefinition>()
            .Where(u => u.Types.Contains(type))
            .ToArray();

        // TODO: Currently we just check that the type exists in the given source schema
        //       and that there are lookups for itself and / or the abstract types
        //       it's a part of. However, we don't check that the type is part of the
        //       abstract type in the given source schema.
        if (type is FusionComplexTypeDefinition complexType)
        {
            var lookups = new List<Lookup>();

            foreach (var source in complexType.Sources)
            {
                lookups.AddRange(source.Lookups);

                foreach (var interfaceType in complexType.Implements)
                {
                    if (interfaceType.Sources.TryGetMember(source.SchemaName, out var interfaceSource))
                    {
                        lookups.AddRange(interfaceSource.Lookups);
                    }
                }

                foreach (var unionType in unionTypes)
                {
                    if (unionType.Sources.TryGetMember(source.SchemaName, out var unionSource))
                    {
                        lookups.AddRange(unionSource.Lookups);
                    }
                }
            }

            return lookups;
        }

        return [];
    }

    public static IEnumerable<Lookup> GetPossibleLookups(
        this FusionSchemaDefinition compositeSchema,
        ITypeDefinition type,
        string schemaName)
    {
        return compositeSchema.GetPossibleLookups(type)
            .Where(l => l.SchemaName == schemaName)
            .ToArray();
    }
}
