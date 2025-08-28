using HotChocolate.Resolvers;

namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// Represents a result formatter configuration.
/// </summary>
public sealed class ResultFormatterConfiguration : IRepeatableConfiguration
{
    /// <summary>
    /// Initializes a new instance of <see cref="ResultFormatterConfiguration"/>.
    /// </summary>
    /// <param name="formatter">
    /// The delegate representing the result converter.
    /// </param>
    /// <param name="isRepeatable">
    /// Defines if the middleware or result converters is repeatable and
    /// the same middleware is allowed to be occur multiple times.
    /// </param>
    /// <param name="key">
    /// The key is optional and is used to identify a middleware.
    /// </param>
    public ResultFormatterConfiguration(
        ResultFormatterDelegate formatter,
        bool isRepeatable = true,
        string? key = null)
    {
        Formatter = formatter;
        IsRepeatable = isRepeatable;
        Key = key;
    }

    /// <summary>
    /// Gets the delegate representing the result converter.
    /// </summary>
    public ResultFormatterDelegate Formatter { get; }

    /// <summary>
    /// Defines if the middleware or result converters is repeatable and
    /// the same middleware is allowed to occur multiple times.
    /// </summary>
    public bool IsRepeatable { get; }

    /// <summary>
    /// The key is optional and is used to identify middleware.
    /// </summary>
    public string? Key { get; }
}
