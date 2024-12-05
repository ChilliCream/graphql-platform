using HotChocolate.Fusion.Results;

namespace HotChocolate.Fusion.PreMergeValidation.Contracts;

internal interface IPreMergeValidationRule
{
    Result Run(PreMergeValidationContext context);
}
