using System.Collections.Immutable;
using HotChocolate.Fusion.Comparers;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.PostMergeValidationRules;
using HotChocolate.Fusion.PreMergeValidationRules;
using HotChocolate.Fusion.Results;
using HotChocolate.Fusion.SourceSchemaValidationRules;
using HotChocolate.Types.Mutable;
using LogSeverity = HotChocolate.Fusion.Logging.LogSeverity;

namespace HotChocolate.Fusion;

internal sealed class SchemaComposer
{
    private readonly IEnumerable<SourceSchemaText> _sourceSchemas;
    private readonly SchemaComposerOptions _schemaComposerOptions;
    private readonly ICompositionLog _log;

    public SchemaComposer(
        IEnumerable<SourceSchemaText> sourceSchemas,
        SchemaComposerOptions schemaComposerOptions,
        ICompositionLog log)
    {
        ArgumentNullException.ThrowIfNull(sourceSchemas);
        ArgumentNullException.ThrowIfNull(schemaComposerOptions);
        ArgumentNullException.ThrowIfNull(log);

        _sourceSchemas = sourceSchemas;
        _schemaComposerOptions = schemaComposerOptions;
        _log = log;
    }

    public CompositionResult<MutableSchemaDefinition> Compose()
    {
        if (!Enum.IsDefined(_schemaComposerOptions.Merger.EnumValuesMergeBehavior))
        {
            return InvalidEnumValuesMergeBehavior(
                $"The enum values merge behavior '{_schemaComposerOptions.Merger.EnumValuesMergeBehavior}' is invalid.");
        }

        if (!Enum.IsDefined(_schemaComposerOptions.Merger.NodeResolution))
        {
            return InvalidNodeResolution(
                $"The node resolution mode '{_schemaComposerOptions.Merger.NodeResolution}' is invalid.");
        }

        if (_schemaComposerOptions.Merger.NodeResolution is NodeResolution.SourceSchema
            && !_schemaComposerOptions.Merger.EnableGlobalObjectIdentification)
        {
            return InvalidNodeResolution(
                "Source-schema node resolution requires global object identification to be enabled.");
        }

        if (!Enum.IsDefined(
            _schemaComposerOptions.ApolloFederationCompatibility
                .ShareableFieldRuntimeTypeRouting))
        {
            return InvalidShareableFieldRuntimeTypeRouting(
                "The shareable field runtime type routing mode "
                + $"'{_schemaComposerOptions.ApolloFederationCompatibility.ShareableFieldRuntimeTypeRouting}' "
                + "is invalid.");
        }

        // Parse Source Schemas
        var parsingResult =
            _sourceSchemas.Select(schema =>
            {
                var options = _schemaComposerOptions.SourceSchemas.GetValueOrDefault(schema.Name);

                return new SourceSchemaParser(
                    schema,
                    _log,
                    options?.Parser,
                    options?.InvalidFieldDeprecationSeverity ?? LogSeverity.Warning,
                    options?.IsApolloFederationV1 ?? false).Parse();
            }).Combine();

        if (parsingResult.IsFailure)
        {
            return parsingResult.Errors;
        }

        var schemas =
            parsingResult.Value.ToImmutableSortedSet(new SchemaByNameComparer<MutableSchemaDefinition>());

        // Preprocess Source Schemas
        var preprocessingResult =
            schemas.Select(schema =>
            {
                var options = _schemaComposerOptions.SourceSchemas.GetValueOrDefault(schema.Name);

                return new SourceSchemaPreprocessor(
                    schema,
                    schemas,
                    _log,
                    options?.Version,
                    options?.Preprocessor,
                    options?.InvalidFieldDeprecationSeverity ?? LogSeverity.Warning,
                    options?.IsApolloFederationV1 ?? false)
                    .Preprocess();
            }).Combine();

        if (preprocessingResult.IsFailure)
        {
            return preprocessingResult.Errors;
        }

        var apolloFederationSchemaNames = schemas
            .Where(static schema =>
                schema.Features.Get<ApolloFederation.ConnectorKindMetadata>()?.Kind
                    == "ApolloFederation")
            .Select(static schema => StringUtilities.ToConstantCase(schema.Name))
            .ToHashSet(StringComparer.Ordinal);

        if (_schemaComposerOptions.ApolloFederationCompatibility
                .AllowNonResolvableInterfaceObjects)
        {
            foreach (var schema in schemas)
            {
                if (schema.Features.Get<ApolloFederation.ConnectorKindMetadata>()?.Kind
                    == "ApolloFederation")
                {
                    schema.Features.Set(
                        new ApolloFederation.ApolloFederationCompatibilityMetadata
                        {
                            AllowNonResolvableInterfaceObjects = true
                        });
                }
            }
        }

        // Enrich Source Schemas
        var enrichmentResult =
            schemas.Select(schema => new SourceSchemaEnricher(schema, schemas).Enrich()).Combine();

        if (enrichmentResult.IsFailure)
        {
            return enrichmentResult.Errors;
        }

        // Prune unreachable definitions from each source schema before validation, so types
        // stripped by @excludeByTag (or otherwise unreferenced) are not validated or merged.
        if (_schemaComposerOptions.Merger.RemoveUnreferencedDefinitions)
        {
            var preservedTypeNames = MutableSchemaDefinitionExtensions.GetPreservedTypeNames(schemas);

            foreach (var schema in schemas)
            {
                schema.RemoveUnreferencedDefinitions(preservedTypeNames, seedUnionsAsRoots: true);
            }
        }

        // Validate Source Schemas
        var validationResult =
            new SourceSchemaValidator(schemas, SourceSchemaRules, _log).Validate();

        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        // Pre Merge Validation
        var preMergeValidationResult =
            new PreMergeValidator(schemas, CreatePreMergeRules(_schemaComposerOptions), _log)
                .Validate();

        if (preMergeValidationResult.IsFailure)
        {
            return preMergeValidationResult;
        }

        // Merge Source Schemas
        var sourceSchemaMergerOptions = _schemaComposerOptions.Merger;
        var (_, isMergeFailure, mergedSchema, mergeErrors) =
            new SourceSchemaMerger(
                schemas,
                sourceSchemaMergerOptions,
                _schemaComposerOptions.ApolloFederationCompatibility
                    .ShareableFieldRuntimeTypeRouting).Merge();

        if (isMergeFailure)
        {
            return mergeErrors;
        }

        // Post Merge Validation
        var postMergeValidationResult =
            new PostMergeValidator(mergedSchema, PostMergeRules, schemas, _log).Validate();

        if (postMergeValidationResult.IsFailure)
        {
            return postMergeValidationResult;
        }

        // Validate Satisfiability
        var satisfiabilityResult =
            new SatisfiabilityValidator(
                mergedSchema,
                _log,
                _schemaComposerOptions.Satisfiability,
                _schemaComposerOptions.Merger.NodeResolution,
                _schemaComposerOptions.ApolloFederationCompatibility,
                apolloFederationSchemaNames).Validate();

        if (satisfiabilityResult.IsFailure)
        {
            return satisfiabilityResult;
        }

        return mergedSchema;
    }

    private CompositionError InvalidEnumValuesMergeBehavior(string message)
    {
        _log.Write(
            LogEntryBuilder.New()
                .SetMessage(message)
                .SetCode(LogEntryCodes.InvalidEnumValuesMergeBehavior)
                .SetSeverity(LogSeverity.Error)
                .Build());

        return new CompositionError(message);
    }

    private CompositionError InvalidNodeResolution(string message)
    {
        _log.Write(
            LogEntryBuilder.New()
                .SetMessage(message)
                .SetCode(LogEntryCodes.InvalidNodeResolution)
                .SetSeverity(LogSeverity.Error)
                .Build());

        return new CompositionError(message);
    }

    private CompositionError InvalidShareableFieldRuntimeTypeRouting(string message)
    {
        _log.Write(
            LogEntryBuilder.New()
                .SetMessage(message)
                .SetCode(LogEntryCodes.InvalidShareableFieldRuntimeTypeRouting)
                .SetSeverity(LogSeverity.Error)
                .Build());

        return new CompositionError(message);
    }

    /// <summary>
    /// Gets the rules that run against each source schema, in the order they are applied.
    /// </summary>
    internal static ImmutableArray<object> SourceSchemaRules { get; } =
    [
        new DisallowedInaccessibleElementsRule(),
        new ExternalOnInterfaceRule(),
        new ExternalOverrideCollisionRule(),
        new ExternalProvidesCollisionRule(),
        new ExternalRequireCollisionRule(),
        new ExternalUnusedRule(),
        new EventCursorMarkerRule(),
        new InterfaceObjectKeyMissingRule(),
        new InvalidShareableUsageRule(),
        new IsInvalidFieldTypeRule(),
        new IsInvalidSyntaxRule(),
        new IsInvalidUsageRule(),
        new KeyDirectiveInFieldsArgumentRule(),
        new KeyFieldsSelectInvalidTypeRule(),
        new KeyInvalidArgumentsRule(),
        new KeyInvalidFieldsTypeRule(),
        new KeyInvalidSyntaxRule(),
        new LookupMustHaveArgumentsRule(),
        new LookupReturnsListRule(),
        new LookupReturnsNonNullableTypeRule(),
        new OverrideFromSelfRule(),
        new OverrideOnInterfaceRule(),
        new ProvidesDirectiveInFieldsArgumentRule(),
        new ProvidesFieldsHasArgumentsRule(),
        new ProvidesFieldsMissingExternalRule(),
        new ProvidesInvalidFieldsRule(),
        new ProvidesInvalidFieldsTypeRule(),
        new ProvidesInvalidSyntaxRule(),
        new ProvidesOnNonCompositeFieldRule(),
        new QueryRootTypeInaccessibleRule(),
        new RequireInvalidFieldTypeRule(),
        new RequireInvalidSyntaxRule(),
        new RootMutationUsedRule(),
        new RootQueryUsedRule(),
        new RootSubscriptionUsedRule(),
        new EventStreamMessageInvalidFieldsRule(),
        new EventStreamTopicsEmptyRule()
    ];

    /// <summary>
    /// Creates the rules that run across the source schemas before the merge, in the order they
    /// are applied, configured from the given options.
    /// </summary>
    internal static ImmutableArray<object> CreatePreMergeRules(SchemaComposerOptions options) =>
    [
        new EnumValuesMismatchRule(options.Merger.EnumValuesMergeBehavior),
        new ExternalArgumentDefaultMismatchRule(),
        new ExternalArgumentMissingRule(),
        new ExternalArgumentTypeMismatchRule(),
        new ExternalMissingOnBaseRule(),
        new ExternalTypeMismatchRule(),
        new FieldArgumentTypesMergeableRule(),
        new FieldWithMissingRequiredArgumentRule(),
        new InputFieldDefaultMismatchRule(),
        new InputFieldTypesMergeableRule(),
        new InputWithMissingRequiredFieldsRule(),
        new InputWithMissingOneOfRule(),
        new InterfaceObjectKeyMismatchRule(),
        new InterfaceObjectNoInterfaceRule(),
        new InvalidFieldSharingRule(),
        new MultipleEventStreamSourcesRule(),
        new OptInFeatureStabilityMismatchRule(),
        new OutputFieldTypesMergeableRule(),
        new SpecifiedByUrlMismatchRule(),
        new TypeKindMismatchRule()
    ];

    /// <summary>
    /// Gets the rules that run against the merged schema, in the order they are applied.
    /// </summary>
    internal static ImmutableArray<object> PostMergeRules { get; } =
    [
        new EmptyMergedEnumTypeRule(),
        new EmptyMergedInputObjectTypeRule(),
        new EmptyMergedInterfaceTypeRule(),
        new EmptyMergedObjectTypeRule(),
        new EmptyMergedUnionTypeRule(),
        new EnumTypeDefaultValueInaccessibleRule(),
        new EventStreamMessageAbstractTypeRequiresTypeNameRule(),
        new ImplementedByInaccessibleRule(),
        new ImplementWithoutDefaultRule(),
        new InterfaceFieldNoImplementationRule(),
        new InterfaceObjectFieldRequiresImplementRule(),
        new InvalidProjectedFieldSharingRule(),
        new IsInvalidFieldsRule(),
        new KeyInvalidFieldsRule(),
        new NonNullInputFieldIsInaccessibleRule(),
        new NoQueriesRule(),
        new ReferenceToDeprecatedTypeRule(),
        new ReferenceToInaccessibleTypeRule(),
        new ReferenceToInternalTypeRule(),
        new RequireInvalidFieldsRule()
    ];
}
