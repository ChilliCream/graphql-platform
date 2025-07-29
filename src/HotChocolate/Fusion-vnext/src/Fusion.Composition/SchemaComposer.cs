using System.Collections.Immutable;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.PostMergeValidationRules;
using HotChocolate.Fusion.PreMergeValidationRules;
using HotChocolate.Fusion.Results;
using HotChocolate.Fusion.SourceSchemaValidationRules;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

public sealed class SchemaComposer(IEnumerable<string> sourceSchemas, SchemaComposerOptions schemaComposerOptions, ICompositionLog log)
{
    private readonly IEnumerable<string> _sourceSchemas = sourceSchemas
        ?? throw new ArgumentNullException(nameof(sourceSchemas));

    private readonly SchemaComposerOptions _schemaComposerOptions = schemaComposerOptions
        ?? throw new ArgumentNullException(nameof(schemaComposerOptions));

    private readonly ICompositionLog _log = log
        ?? throw new ArgumentNullException(nameof(log));

    public CompositionResult<MutableSchemaDefinition> Compose()
    {
        // Parse Source Schemas
        var (_, isParseFailure, schemas, parseErrors) =
            new SourceSchemaParser(_sourceSchemas, _log).Parse();

        if (isParseFailure)
        {
            return parseErrors;
        }

        // Validate Source Schemas
        var validationResult =
            new SourceSchemaValidator(schemas, s_sourceSchemaRules, _log).Validate();

        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        // Pre Merge Validation
        var preMergeValidationResult =
            new PreMergeValidator(schemas, s_preMergeRules, _log).Validate();

        if (preMergeValidationResult.IsFailure)
        {
            return preMergeValidationResult;
        }

        // Merge Source Schemas
        var sourceSchemaMergerOptions = new SourceSchemaMergerOptions
        {
            EnableGlobalObjectIdentification = _schemaComposerOptions.EnableGlobalObjectIdentification
        };
        var (_, isMergeFailure, mergedSchema, mergeErrors) =
            new SourceSchemaMerger(schemas, sourceSchemaMergerOptions).Merge();

        if (isMergeFailure)
        {
            return mergeErrors;
        }

        // Post Merge Validation
        var postMergeValidationResult =
            new PostMergeValidator(mergedSchema, s_postMergeRules, schemas, _log).Validate();

        if (postMergeValidationResult.IsFailure)
        {
            return postMergeValidationResult;
        }

        // Validate Satisfiability
        var satisfiabilityResult = new SatisfiabilityValidator(mergedSchema, _log).Validate();

        if (satisfiabilityResult.IsFailure)
        {
            return satisfiabilityResult;
        }

        return mergedSchema;
    }

    private static readonly ImmutableArray<object> s_sourceSchemaRules =
    [
        new DisallowedInaccessibleElementsRule(),
        new ExternalOnInterfaceRule(),
        new ExternalUnusedRule(),
        new InvalidShareableUsageRule(),
        new IsInvalidFieldTypeRule(),
        new IsInvalidSyntaxRule(),
        new IsInvalidUsageRule(),
        new KeyDirectiveInFieldsArgumentRule(),
        new KeyFieldsHasArgumentsRule(),
        new KeyFieldsSelectInvalidTypeRule(),
        new KeyInvalidFieldsRule(),
        new KeyInvalidFieldsTypeRule(),
        new KeyInvalidSyntaxRule(),
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
        new TypeDefinitionInvalidRule()
    ];

    private static readonly ImmutableArray<object> s_preMergeRules =
    [
        new EnumValuesMismatchRule(),
        new ExternalArgumentDefaultMismatchRule(),
        new ExternalMissingOnBaseRule(),
        new FieldArgumentTypesMergeableRule(),
        new FieldWithMissingRequiredArgumentRule(),
        new InputFieldDefaultMismatchRule(),
        new InputFieldTypesMergeableRule(),
        new InputWithMissingRequiredFieldsRule(),
        new OutputFieldTypesMergeableRule(),
        new TypeKindMismatchRule()
    ];

    private static readonly ImmutableArray<object> s_postMergeRules =
    [
        new EmptyMergedEnumTypeRule(),
        new EmptyMergedInputObjectTypeRule(),
        new EmptyMergedInterfaceTypeRule(),
        new EmptyMergedObjectTypeRule(),
        new EmptyMergedUnionTypeRule(),
        new EnumTypeDefaultValueInaccessibleRule(),
        new ImplementedByInaccessibleRule(),
        new InterfaceFieldNoImplementationRule(),
        new IsInvalidFieldRule(),
        new NonNullInputFieldIsInaccessibleRule(),
        new NoQueriesRule(),
        new RequireInvalidFieldsRule()
    ];
}
