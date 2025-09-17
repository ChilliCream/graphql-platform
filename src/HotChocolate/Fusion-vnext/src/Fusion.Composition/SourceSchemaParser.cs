using System.Collections.Immutable;
using HotChocolate.Fusion.Comparers;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Results;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion;

internal sealed class SourceSchemaParser(IEnumerable<SourceSchemaText> sourceSchemas, ICompositionLog log)
{
    public CompositionResult<ImmutableSortedSet<MutableSchemaDefinition>> Parse()
    {
        var sortedSetBuilder = ImmutableSortedSet.CreateBuilder(
            new SchemaByNameComparer<MutableSchemaDefinition>());

        foreach (var sourceSchema in sourceSchemas)
        {
            try
            {
                var schema = new MutableSchemaDefinition { Name = sourceSchema.Name };
                schema.AddBuiltInFusionTypes();
                schema.AddBuiltInFusionDirectives();
                SchemaParser.Parse(
                    schema,
                    sourceSchema.SourceText,
                    new SchemaParserOptions
                    {
                        IgnoreExistingTypes = true,
                        IgnoreExistingDirectives = true
                    });

                sortedSetBuilder.Add(schema);
            }
            catch (Exception ex)
            {
                log.Write(LogEntryHelper.InvalidGraphQL(ex.Message));
                break;
            }
        }

        return log.HasErrors
            ? ErrorHelper.SourceSchemaParsingFailed()
            : sortedSetBuilder.ToImmutable();
    }
}
