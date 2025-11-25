using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

internal static class CompositionTestHelper
{
    internal static ImmutableSortedSet<MutableSchemaDefinition> CreateSchemaDefinitions(
        string[] sdl)
    {
        var log = new CompositionLog();
        var sourceSchemaParser =
            new SourceSchemaParser(
                sdl.Select((s, i) => new SourceSchemaText(((char)('A' + i)).ToString(), s)),
                log,
                new SourceSchemaParserOptions { EnableSchemaValidation = false });

        var (_, isFailure, schemas, _) = sourceSchemaParser.Parse();

        if (isFailure)
        {
            throw new Exception($"Schema creation failed.\n- {string.Join("\n- ", log)}");
        }

        foreach (var schema in schemas)
        {
            new SourceSchemaPreprocessor(schema).Process();
            new SourceSchemaEnricher(schema, schemas).Enrich();
        }

        return schemas;
    }
}
