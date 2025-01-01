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
        new ExternalArgumentDefaultMismatchRule(),
        new ExternalMissingOnBaseRule(),
        new ExternalOnInterfaceRule(),
        new ExternalUnusedRule(),
        new KeyDirectiveInFieldsArgumentRule(),
        new KeyFieldsHasArgumentsRule(),
        new KeyFieldsSelectInvalidTypeRule(),
        new KeyInvalidFieldsRule(),
        new KeyInvalidSyntaxRule(),
        new OutputFieldTypesMergeableRule(),
        new ProvidesDirectiveInFieldsArgumentRule(),
        new RootMutationUsedRule(),
        new RootQueryUsedRule(),
        new RootSubscriptionUsedRule()
    ];
}
