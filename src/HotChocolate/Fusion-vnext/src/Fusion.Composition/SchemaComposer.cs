using System.Collections.Immutable;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.PreMergeValidation.Rules;
using HotChocolate.Fusion.Results;
using HotChocolate.Fusion.SourceSchemaValidation.Rules;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion;

public sealed class SchemaComposer
{
    public CompositionResult<SchemaDefinition> Compose(
        IEnumerable<SchemaDefinition> schemaDefinitions,
        ICompositionLog compositionLog)
    {
        ArgumentNullException.ThrowIfNull(schemaDefinitions);
        ArgumentNullException.ThrowIfNull(compositionLog);

        var schemas = schemaDefinitions.ToImmutableArray();
        var context = new CompositionContext(schemas, compositionLog);

        // Validate Source Schemas
        var validationResult =
            new SourceSchemaValidator(_sourceSchemaValidationRules).Validate(context);

        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        // Pre Merge Validation
        var preMergeValidationResult =
            new PreMergeValidator(_preMergeValidationRules).Validate(context);

        if (preMergeValidationResult.IsFailure)
        {
            return preMergeValidationResult;
        }

        // Merge Source Schemas
        var mergeResult = new SourceSchemaMerger(schemas).MergeSchemas();

        if (mergeResult.IsFailure)
        {
            return mergeResult;
        }

        // Post Merge Validation
        var postMergeValidationResult =
            new PostMergeValidator(_postMergeValidationRules).Validate(mergeResult.Value);

        if (postMergeValidationResult.IsFailure)
        {
            return postMergeValidationResult;
        }

        // Validate Satisfiability
        var satisfiabilityResult = new SatisfiabilityValidator().Validate(mergeResult.Value);

        if (satisfiabilityResult.IsFailure)
        {
            return satisfiabilityResult;
        }

        return mergeResult;
    }

    private static readonly List<object> _sourceSchemaValidationRules =
    [
        new DisallowedInaccessibleElementsRule(),
        new ExternalOnInterfaceRule(),
        new ExternalUnusedRule(),
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
        new ProvidesInvalidFieldsTypeRule(),
        new ProvidesInvalidSyntaxRule(),
        new ProvidesOnNonCompositeFieldRule(),
        new QueryRootTypeInaccessibleRule(),
        new RequireDirectiveInFieldsArgumentRule(),
        new RequireInvalidFieldsTypeRule(),
        new RequireInvalidSyntaxRule(),
        new RootMutationUsedRule(),
        new RootQueryUsedRule(),
        new RootSubscriptionUsedRule()
    ];

    private static readonly List<object> _preMergeValidationRules =
    [
        new EnumValuesMismatchRule(),
        new ExternalArgumentDefaultMismatchRule(),
        new ExternalMissingOnBaseRule(),
        new FieldArgumentTypesMergeableRule(),
        new InputFieldDefaultMismatchRule(),
        new InputFieldTypesMergeableRule(),
        new InputWithMissingRequiredFieldsRule(),
        new OutputFieldTypesMergeableRule()
    ];

    private static readonly List<object> _postMergeValidationRules = [];
}
