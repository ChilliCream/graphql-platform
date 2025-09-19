using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

internal static class CompositionTestHelper
{
    internal static ImmutableSortedSet<MutableSchemaDefinition> CreateSchemaDefinitions(
        string[] sdl)
    {
        var sourceSchemaParser =
            new SourceSchemaParser(
                sdl.Select((s, i) => new SourceSchemaText(((char)('A' + i)).ToString(), s)),
                new CompositionLog());

        return sourceSchemaParser.Parse().Value;
    }
}
