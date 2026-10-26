using System.Collections.Immutable;
using HotChocolate.Fusion.Comparers;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

internal static class CompositionTestHelper
{
    internal static (CompositionResult<MutableSchemaDefinition> Result, CompositionLog Log) ComposeSourceSchemas(
        SourceSchemaText[] sourceSchemas)
        => ComposeSourceSchemas(ShareableFieldRuntimeTypeRouting.SourceLocal, sourceSchemas);

    internal static (CompositionResult<MutableSchemaDefinition> Result, CompositionLog Log) ComposeSourceSchemas(
        ShareableFieldRuntimeTypeRouting shareableFieldRuntimeTypeRouting,
        SourceSchemaText[] sourceSchemas)
    {
        var log = new CompositionLog();
        var schemaComposer = new SchemaComposer(
            sourceSchemas,
            new SchemaComposerOptions
            {
                ApolloFederationCompatibility =
                {
                    ShareableFieldRuntimeTypeRouting = shareableFieldRuntimeTypeRouting
                },
                Merger = { AddFusionDefinitions = false },
                Satisfiability = { IncludeSatisfiabilityPaths = true }
            },
            log);

        return (schemaComposer.Compose(), log);
    }

    internal static ImmutableSortedSet<MutableSchemaDefinition> CreateSchemaDefinitions(
        string[] sdl)
    {
        var log = new CompositionLog();

        var parsingResult =
            sdl.Select((s, i) =>
            {
                var sourceSchemaText = new SourceSchemaText(((char)('A' + i)).ToString(), s);
                var options = new SourceSchemaParserOptions { EnableSchemaValidation = false };

                return new SourceSchemaParser(sourceSchemaText, log, options).Parse();
            }).Combine();

        if (parsingResult.IsFailure)
        {
            throw new Exception($"Schema creation failed.\n- {string.Join("\n- ", log)}");
        }

        var schemas =
            parsingResult.Value.ToImmutableSortedSet(new SchemaByNameComparer<MutableSchemaDefinition>());

        foreach (var schema in schemas)
        {
            new SourceSchemaPreprocessor(schema, schemas, log).Preprocess();
            new SourceSchemaEnricher(schema, schemas).Enrich();
        }

        return schemas;
    }
}
