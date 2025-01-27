using System.Collections.Immutable;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Skimmed;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Fusion;

public abstract class CompositionTestBase
{
    internal static CompositionContext CreateCompositionContext(string[] sdl)
    {
        return new CompositionContext(CreateSchemaDefinitions(sdl), new CompositionLog());
    }

    internal static ImmutableArray<SchemaDefinition> CreateSchemaDefinitions(string[] sdl)
    {
        return
        [
            .. sdl.Select((s, i) =>
            {
                var schemaDefinition = SchemaParser.Parse(s);
                schemaDefinition.Name = ((char)('A' + i)).ToString();

                return schemaDefinition;
            })
        ];
    }
}
