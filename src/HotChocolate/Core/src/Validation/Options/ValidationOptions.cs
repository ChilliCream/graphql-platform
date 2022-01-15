using System.Collections.Generic;

namespace HotChocolate.Validation.Options;

/// <summary>
/// The validation options.
/// </summary>
public class ValidationOptions : IMaxExecutionDepthOptionsAccessor
{
    private int? _maxAllowedExecutionDepth;

    /// <summary>
    /// Gets the document rules of the validation.
    /// </summary>
    public IList<IDocumentValidatorRule> Rules { get; } =
        new List<IDocumentValidatorRule>();

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
}
