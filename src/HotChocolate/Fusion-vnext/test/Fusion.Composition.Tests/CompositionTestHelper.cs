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

        var result = sourceSchemaParser.Parse();

        return result.IsFailure
            ? throw new Exception($"Schema creation failed.\n- {string.Join("\n- ", log)}")
            : result.Value;
    }
}
