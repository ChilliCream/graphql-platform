using System.Collections.Immutable;
using HotChocolate.Fusion.PreMergeValidation.Contracts;
using HotChocolate.Fusion.PreMergeValidation.Rules;
using HotChocolate.Fusion.Results;

namespace HotChocolate.Fusion.PreMergeValidation;

internal sealed class PreMergeValidator
{
    private readonly ImmutableArray<IPreMergeValidationRule> _validationRules =
    [
        new DisallowedInaccessibleElementsRule(),
        new OutputFieldTypesMergeableRule()
    ];

    public CompositionResult Validate(CompositionContext compositionContext)
    {
        var preMergeValidationContext = new PreMergeValidationContext(compositionContext);
        preMergeValidationContext.Initialize();

        var errors = new List<Error>();

        foreach (var validationRule in _validationRules)
        {
            var result = validationRule.Run(preMergeValidationContext);

            if (result.IsFailure)
            {
                errors.AddRange(result.Errors);
            }
        }

        return errors;
    }
}
