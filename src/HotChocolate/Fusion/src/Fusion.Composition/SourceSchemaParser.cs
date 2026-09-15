using HotChocolate.Fusion.ApolloFederation;
using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Logging;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;
using FusionLogEntryBuilder = HotChocolate.Fusion.Logging.LogEntryBuilder;
using FusionLogEntryCodes = HotChocolate.Fusion.Logging.LogEntryCodes;
using LogEntryHelper = HotChocolate.Fusion.Logging.LogEntryHelper;
using LogSeverity = HotChocolate.Fusion.Logging.LogSeverity;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;
using DocumentNode = HotChocolate.Language.DocumentNode;
using DirectiveDefinitionNode = HotChocolate.Language.DirectiveDefinitionNode;
using DirectiveNode = HotChocolate.Language.DirectiveNode;
using ISyntaxNode = HotChocolate.Language.ISyntaxNode;
using SyntaxException = HotChocolate.Language.SyntaxException;
using Utf8GraphQLParser = HotChocolate.Language.Utf8GraphQLParser;
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
    private readonly ScopedCompositionLog _log = new(log);

    public CompositionResult<MutableSchemaDefinition> Parse()
    {
        var schema = new MutableSchemaDefinition { Name = sourceSchemaText.Name };
        schema.AddBuiltInFusionTypes();
        schema.AddBuiltInFusionDirectives();

        // A source schema may apply @cost/@listSize without declaring its own definition.
        // Inject the canonical definition only when the combined source text uses the directive
        // but does not declare it (R-COMPOSITION-COMPAT).
        var requiresInjectedDefinitions = GetRequiredDefinitionInjections(sourceSchemaText);

        if (requiresInjectedDefinitions.Contains(DirectiveNames.Cost))
        {
            schema.DirectiveDefinitions.Add(CostMutableDirectiveDefinition.Create(schema));
        }

        if (requiresInjectedDefinitions.Contains(DirectiveNames.ListSize))
        {
            schema.DirectiveDefinitions.Add(ListSizeMutableDirectiveDefinition.Create(schema));
        }

        if (isApolloFederationV1)
        {
            FederationV1DirectiveDefinitions.Apply(schema);
        }

        if (!isApolloFederationV1 && IsFederationSourceText(sourceSchemaText))
        {
            // Apollo Federation's @requires directive has no Fusion-native definition (the Fusion
            // equivalent @require differs in name and argument shape). Register it before parsing
            // federation source schemas so an applied @requires binds to a real definition instead
            // of a missing one; preprocessing then rewrites it to @require and removes the
            // definition. A non-federation schema does not get the definition, so an applied
            // @requires is reported as an unknown directive, steering authors to @require.
            if (schema.Types.TryGetType<MutableScalarTypeDefinition>(
                WellKnownTypeNames.FieldSelectionSet, out var fieldSelectionSetType))
            {
                schema.DirectiveDefinitions.Add(
                    new RequiresMutableDirectiveDefinition(fieldSelectionSetType));
            }

            // Apollo Federation's @external may also be applied to object types, which the
            // Fusion @external definition does not allow. Replace it so federation applications
            // bind to the federation shape; RemoveFederationInfrastructure drops the definition
            // during preprocessing.
            schema.DirectiveDefinitions.Remove(WellKnownDirectiveNames.External);
            schema.DirectiveDefinitions.Add(
                new MutableDirectiveDefinition(FederationDirectiveNames.External)
                {
                    Locations = DirectiveLocation.FieldDefinition | DirectiveLocation.Object
                });

            // @link carries the federation vocabulary a subgraph imports and is applied to the
            // schema itself. RemoveFederationInfrastructure drops the definition and every
            // application during preprocessing.
            schema.DirectiveDefinitions.Add(LinkMutableDirectiveDefinition.Create(schema));
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
            _log.Write(LogEntryHelper.InvalidGraphQL(ex.Message, schema));
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
                _log.Write(LogEntryHelper.InvalidGraphQL(ex.Message, schema, inExtensions: true));
            }
        }

        if (isApolloFederationV1
            && FederationSchemaTransformer.IsFederationSchema(schema))
        {
            _log.Write(
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
            FederationV1SchemaAnalyzer.Validate(schema, _log);
        }

        // Schema validation.
        if (_options.EnableSchemaValidation && !_log.HasErrors)
        {
            var validationLog = new ValidationLog();
            s_schemaValidator.Validate(schema, validationLog);

            if (validationLog.HasErrors)
            {
                _log.WriteValidationLog(
                    validationLog, schema, invalidFieldDeprecationSeverity);
            }
        }

        return _log.HasErrors
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

    private static readonly string[] s_injectableDirectiveNames = [DirectiveNames.Cost, DirectiveNames.ListSize];

    /// <summary>
    /// Determines, in one pass over the source schema's combined text, which of the injectable
    /// directives (<c>@cost</c>, <c>@listSize</c>) must have a canonical definition injected: a
    /// directive the text applies somewhere but declares no definition for anywhere (main or
    /// extensions text). A declared definition (any shape) always wins and is never injected
    /// over; a directive neither used nor declared is left alone.
    /// </summary>
    private static HashSet<string> GetRequiredDefinitionInjections(SourceSchemaText sourceSchemaText)
    {
        var declared = new HashSet<string>(StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);

        CollectDirectiveNames(TryParseDocument(sourceSchemaText.SourceText), declared, used);

        if (sourceSchemaText.ExtensionsSourceText is { } extensionsText)
        {
            CollectDirectiveNames(TryParseDocument(extensionsText), declared, used);
        }

        used.ExceptWith(declared);
        used.IntersectWith(s_injectableDirectiveNames);
        return used;
    }

    private static DocumentNode? TryParseDocument(string sourceText)
    {
        try
        {
            return Utf8GraphQLParser.Parse(sourceText);
        }
        catch (SyntaxException)
        {
            // Malformed text is reported by the real parse further down; injection is skipped
            // and the underlying error surfaces normally.
            return null;
        }
    }

    private static void CollectDirectiveNames(DocumentNode? document, HashSet<string> declared, HashSet<string> used)
    {
        if (document is null)
        {
            return;
        }

        foreach (var definition in document.Definitions)
        {
            if (definition is DirectiveDefinitionNode directiveDefinition)
            {
                declared.Add(directiveDefinition.Name.Value);
            }
        }

        CollectDirectiveApplications(document, used);
    }

    private static void CollectDirectiveApplications(ISyntaxNode node, HashSet<string> used)
    {
        if (node is DirectiveNode directive)
        {
            used.Add(directive.Name.Value);
        }

        foreach (var child in node.GetNodes())
        {
            CollectDirectiveApplications(child, used);
        }
    }
}
