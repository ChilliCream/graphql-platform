using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Composition;

public abstract class CompositionTestBase
{
    internal static CompositionContext CreateCompositionContext(string[] sdl)
    {
        return new CompositionContext(
            [
                .. sdl.Select((s, i) =>
                {
                    var schemaDefinition = SchemaParser.Parse(s);
                    schemaDefinition.Name = ((char)('A' + i)).ToString();

                    return schemaDefinition;
                })
            ],
            new CompositionLog());
    }
}
