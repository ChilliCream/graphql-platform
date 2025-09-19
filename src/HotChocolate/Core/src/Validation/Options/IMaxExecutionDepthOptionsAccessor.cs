namespace HotChocolate.Validation.Options;

/// <summary>
/// Represents the options for the max execution depth analyzer.
/// </summary>
public interface IMaxExecutionDepthOptionsAccessor
{
    /// <summary>
    /// Gets the maximum allowed depth of a query. The default value is
    /// <see langword="null"/>. The minimum allowed value is <c>1</c>.
    /// </summary>
    int? MaxAllowedExecutionDepth { get; }

    /// <summary>
    /// Specifies that the max execution depth analysis
    /// shall skip introspection fields.
    /// </summary>
    bool SkipIntrospectionFields { get; }
}
