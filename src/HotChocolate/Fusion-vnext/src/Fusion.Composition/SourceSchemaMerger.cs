using HotChocolate.Fusion.PostMergeValidation;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.Results;
using HotChocolate.Skimmed;

namespace HotChocolate.Fusion;

internal sealed class SourceSchemaMerger
{
    public Result<SchemaDefinition> Merge(CompositionContext context)
    {
        // Pre Merge Validation
        var preMergeValidationResult = new PreMergeValidator().Validate(context);

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

    private Result<SchemaDefinition> MergeSchemaDefinitions(CompositionContext _)
    {
        // FIXME: Implement.
        return new SchemaDefinition();
    }
}
