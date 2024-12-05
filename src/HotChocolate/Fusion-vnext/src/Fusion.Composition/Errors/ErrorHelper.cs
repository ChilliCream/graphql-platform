using HotChocolate.Fusion.PreMergeValidation.Contracts;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Errors;

internal static class ErrorHelper
{
    public static CompositionError PreMergeValidationRuleFailed(IPreMergeValidationRule rule)
        => new(string.Format(ErrorHelper_PreMergeValidationRuleFailed, rule.GetType().Name));
}
