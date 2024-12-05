using HotChocolate.Fusion.Results;

namespace HotChocolate.Fusion.PreMergeValidation.Contracts;

internal interface IPreMergeValidationRule
{
    CompositionResult Run(PreMergeValidationContext context);
}
