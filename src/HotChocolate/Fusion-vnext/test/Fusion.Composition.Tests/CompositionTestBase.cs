using System.Collections.Immutable;
using HotChocolate.Fusion.Comparers;
using HotChocolate.Skimmed;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Fusion;

public abstract class CompositionTestBase
{
    internal static ImmutableSortedSet<SchemaDefinition> CreateSchemaDefinitions(string[] sdl)
    {
        var schemaDefinitions =
            sdl.Select((s, i) =>
            {
                var schemaDefinition = SchemaParser.Parse(s);
                schemaDefinition.Name = ((char)('A' + i)).ToString();

                return schemaDefinition;
            });

        return schemaDefinitions.ToImmutableSortedSet(new SchemaByNameComparer());
    }
}
