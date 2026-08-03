using HotChocolate.Fusion.ApolloFederation;
using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Logging;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;
using FusionLogEntryBuilder = HotChocolate.Fusion.Logging.LogEntryBuilder;
using FusionLogEntryCodes = HotChocolate.Fusion.Logging.LogEntryCodes;
using LogEntryHelper = HotChocolate.Fusion.Logging.LogEntryHelper;
using LogSeverity = HotChocolate.Fusion.Logging.LogSeverity;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion;

internal sealed class SourceSchemaParser(
    SourceSchemaText sourceSchemaText,
    ICompositionLog log,
    SourceSchemaParserOptions? options = null,
    LogSeverity invalidFieldDeprecationSeverity = LogSeverity.Warning,
    bool isApolloFederationV1 = false)
{
    private static readonly SchemaValidator s_schemaValidator = new();
    private readonly SourceSchemaParserOptions _options = options ?? new SourceSchemaParserOptions();

    public CompositionResult<MutableSchemaDefinition> Parse()
    {
        var schema = new MutableSchemaDefinition { Name = sourceSchemaText.Name };
        schema.AddBuiltInFusionTypes();
        schema.AddBuiltInFusionDirectives();

        if (isApolloFederationV1)
        {
            FederationV1DirectiveDefinitions.Apply(schema);
        }

        // Apollo Federation's @requires directive has no Fusion-native definition (the Fusion
        // equivalent @require differs in name and argument shape). Register it before parsing
        // federation source schemas so an applied @requires binds to a real definition instead
        // of a missing one; preprocessing then rewrites it to @require and removes the
        // definition. A non-federation schema does not get the definition, so an applied
        // @requires is reported as an unknown directive, steering authors to @require.
        if (!isApolloFederationV1
            && IsFederationSourceText(sourceSchemaText)
            && schema.Types.TryGetType<MutableScalarTypeDefinition>(
                WellKnownTypeNames.FieldSelectionSet, out var fieldSelectionSetType))
        {
            schema.DirectiveDefinitions.Add(
                new RequiresMutableDirectiveDefinition(fieldSelectionSetType));
        }

        // Parse source schema.
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
        }
        catch (Exception ex)
        {
            log.Write(LogEntryHelper.InvalidGraphQL(ex.Message, schema));
        }

        // Parse optional source schema extensions.
        if (sourceSchemaText.ExtensionsSourceText is not null)
        {
            try
            {
                SchemaParser.Parse(
                    schema,
                    sourceSchemaText.ExtensionsSourceText,
                    new SchemaParserOptions
                    {
                        IgnoreExistingTypes = true,
                        IgnoreExistingDirectives = true
                    });
            }
            catch (Exception ex)
            {
                log.Write(LogEntryHelper.InvalidGraphQL(ex.Message, schema, inExtensions: true));
            }
        }

        if (isApolloFederationV1
            && FederationSchemaTransformer.IsFederationSchema(schema))
        {
            log.Write(
                FusionLogEntryBuilder.New()
                    .SetMessage(
                        SourceSchemaParser_ConflictingApolloFederationVersion,
                        schema.Name)
                    .SetCode(FusionLogEntryCodes.ConflictingApolloFederationVersion)
                    .SetSeverity(LogSeverity.Error)
                    .SetSchema(schema)
                    .Build());
        }

        if (isApolloFederationV1)
        {
            FederationV1SchemaAnalyzer.Validate(schema, log);
        }

        // Schema validation.
        if (_options.EnableSchemaValidation && !log.HasErrors)
        {
            var validationLog = new ValidationLog();
            s_schemaValidator.Validate(schema, validationLog);

            if (validationLog.HasErrors)
            {
                log.WriteValidationLog(
                    validationLog, schema, invalidFieldDeprecationSeverity);
            }
        }

        return log.HasErrors
            ? ErrorHelper.SourceSchemaParsingFailed()
            : schema;
    }

    private static bool IsFederationSourceText(SourceSchemaText sourceSchemaText)
        => sourceSchemaText.SourceText.Contains(
               FederationSchemaAnalyzer.FederationUrlPrefix,
               StringComparison.Ordinal)
           || (sourceSchemaText.ExtensionsSourceText?.Contains(
               FederationSchemaAnalyzer.FederationUrlPrefix,
               StringComparison.Ordinal) ?? false);
}
