namespace HotChocolate.Validation.Options;

/// <summary>
/// The validation rules options.
/// </summary>
public sealed class ValidationRulesOptions
{
    /// <summary>
    /// Gets the document rules of the validation.
    /// </summary>
    public IList<IDocumentValidatorRule> Rules { get; } =
        new List<IDocumentValidatorRule>();

    /// <summary>
    /// Gets the document rules that run async logic after the initial validators have run..
    /// </summary>
    public IList<IValidationResultAggregator> ResultAggregators { get; } =
        new List<IValidationResultAggregator>();
}
