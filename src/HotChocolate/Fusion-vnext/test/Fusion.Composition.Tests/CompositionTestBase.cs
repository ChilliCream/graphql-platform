using System.Collections.Immutable;
using HotChocolate.Fusion.Comparers;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

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
