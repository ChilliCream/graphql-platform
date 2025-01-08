using HotChocolate.Fusion.PostMergeValidation;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;
using HotChocolate.Fusion.Results;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion;

internal sealed class SourceSchemaMerger
{
    public CompositionResult<SchemaDefinition> Merge(CompositionContext context)
    {
        // Pre Merge Validation
        var preMergeValidationResult =
            new PreMergeValidator(_preMergeValidationRules).Validate(context);

        if (preMergeValidationResult.IsFailure)
        {
            return preMergeValidationResult;
        }

        // Merge
        var mergeResult = MergeSchemaDefinitions(context);

        if (mergeResult.IsFailure)
        {
            return mergeResult;
        }

        // Post Merge Validation
        var postMergeValidationResult = new PostMergeValidator().Validate(mergeResult.Value);

        if (postMergeValidationResult.IsFailure)
        {
            return postMergeValidationResult;
        }

        return mergeResult;
    }

    private CompositionResult<SchemaDefinition> MergeSchemaDefinitions(CompositionContext _)
    {
        // FIXME: Implement.
        return new SchemaDefinition();
    }

    private static readonly List<object> _preMergeValidationRules =
    [
        new DisallowedInaccessibleElementsRule(),
        new EnumValuesMismatchRule(),
        new ExternalArgumentDefaultMismatchRule(),
        new ExternalMissingOnBaseRule(),
        new ExternalOnInterfaceRule(),
        new ExternalUnusedRule(),
        new InputFieldDefaultMismatchRule(),
        new InputFieldTypesMergeableRule(),
        new InputWithMissingRequiredFieldsRule(),
        new KeyDirectiveInFieldsArgumentRule(),
        new KeyFieldsHasArgumentsRule(),
        new KeyFieldsSelectInvalidTypeRule(),
        new KeyInvalidFieldsRule(),
        new KeyInvalidFieldsTypeRule(),
        new KeyInvalidSyntaxRule(),
        new LookupMustNotReturnListRule(),
        new LookupShouldHaveNullableReturnTypeRule(),
        new OutputFieldTypesMergeableRule(),
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
}
