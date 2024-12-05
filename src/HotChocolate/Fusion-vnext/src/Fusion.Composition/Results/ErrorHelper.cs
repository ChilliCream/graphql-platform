using HotChocolate.Fusion.PreMergeValidation.Contracts;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Results;

internal static class ErrorHelper
{
    public static Error PreMergeValidationRuleFailed(IPreMergeValidationRule rule)
        => new(string.Format(ErrorHelper_PreMergeValidationRuleFailed, rule.GetType().Name));
}
