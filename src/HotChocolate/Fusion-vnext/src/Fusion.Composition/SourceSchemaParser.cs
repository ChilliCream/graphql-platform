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
    SourceSchemaText sourceSchemaText,
    ICompositionLog log,
    SourceSchemaParserOptions? options = null)
{
    private static readonly SchemaValidator s_schemaValidator = new();
    private readonly SourceSchemaParserOptions _options = options ?? new SourceSchemaParserOptions();

    public CompositionResult<MutableSchemaDefinition> Parse()
    {
        var schema = new MutableSchemaDefinition { Name = sourceSchemaText.Name };
        schema.AddBuiltInFusionTypes();
        schema.AddBuiltInFusionDirectives();

        try
        {
            SchemaParser.Parse(
                schema,
                sourceSchemaText.SourceText,
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
                }
            }
        }
        catch (Exception ex)
        {
            log.Write(LogEntryHelper.InvalidGraphQL(ex.Message, schema));
        }

        return log.HasErrors
            ? ErrorHelper.SourceSchemaParsingFailed()
            : schema;
    }
}
