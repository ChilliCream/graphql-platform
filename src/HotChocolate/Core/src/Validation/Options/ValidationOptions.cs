using System.Collections.Generic;

namespace HotChocolate.Validation.Options;

/// <summary>
/// The validation options.
/// </summary>
public class ValidationOptions
    : IMaxExecutionDepthOptionsAccessor
    , IErrorOptionsAccessor
{
    private int? _maxAllowedExecutionDepth;
    private int _maxErrors = 5;

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

    /// <summary>
    /// Gets the maximum allowed depth of a query. The default value is
    /// <see langword="null"/>. The minimum allowed value is <c>1</c>.
    /// </summary>
    public int? MaxAllowedExecutionDepth
    {
        get => _maxAllowedExecutionDepth;
        set
        {
            _maxAllowedExecutionDepth = value < 1 ? 1 : value;
        }
    }

    /// <summary>
    /// Specifies that the max execution depth analysis
    /// shall skip introspection fields.
    /// </summary>
    public bool SkipIntrospectionFields { get; set; }

    /// <summary>
    /// Specifies how many errors are allowed before the validation is aborted.
    /// </summary>
    public int MaxAllowedErrors
    {
        get => _maxErrors;
        set
        {
            // if the value is lover than 1 we will set it to the default.
            if (value < 1)
            {
                value = 5;
            }
            _maxErrors = value;
        }
    }
}
