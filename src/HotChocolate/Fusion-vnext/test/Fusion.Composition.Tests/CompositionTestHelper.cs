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

        var schemas = sourceSchemaParser.Parse().Value;

        foreach (var schema in schemas)
        {
            new SourceSchemaPreprocessor(schema).Process();
            new SourceSchemaEnricher(schema, schemas).Enrich();
        }

        return schemas;
    }
}
