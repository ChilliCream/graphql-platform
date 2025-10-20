using System.Collections.Immutable;
using HotChocolate.Fusion.Comparers;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Logging;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion;

internal sealed class SourceSchemaParser(
    IEnumerable<SourceSchemaText> sourceSchemas,
    ICompositionLog log,
    SourceSchemaParserOptions? options = null)
{
    private static readonly SchemaValidator s_schemaValidator = new();
    private readonly SourceSchemaParserOptions _options = options ?? new SourceSchemaParserOptions();

    public CompositionResult<ImmutableSortedSet<MutableSchemaDefinition>> Parse()
    {
        var sortedSetBuilder = ImmutableSortedSet.CreateBuilder(
            new SchemaByNameComparer<MutableSchemaDefinition>());

        foreach (var sourceSchema in sourceSchemas)
        {
            var schema = new MutableSchemaDefinition { Name = sourceSchema.Name };
            schema.AddBuiltInFusionTypes();
            schema.AddBuiltInFusionDirectives();

            try
            {
                SchemaParser.Parse(
                    schema,
                    sourceSchema.SourceText,
                    new SchemaParserOptions
                    {
                        IgnoreExistingTypes = true,
                        IgnoreExistingDirectives = true
                    });

                // Schema validation.
                if (_options.EnableSchemaValidation)
                {
                    var validationLog = new ValidationLog();
                    s_schemaValidator.Validate(schema, validationLog);

                    if (validationLog.HasErrors)
                    {
                        log.WriteValidationLog(validationLog, schema);
                        continue;
                    }
                }

                sortedSetBuilder.Add(schema);
            }
            catch (Exception ex)
            {
                log.Write(LogEntryHelper.InvalidGraphQL(ex.Message, schema));
            }
        }

        return log.HasErrors
            ? ErrorHelper.SourceSchemaParsingFailed()
            : sortedSetBuilder.ToImmutable();
    }
}
