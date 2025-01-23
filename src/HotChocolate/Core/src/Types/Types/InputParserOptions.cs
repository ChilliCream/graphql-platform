#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Represents the input parser options.
/// </summary>
public sealed class InputParserOptions
{
    /// <summary>
    /// Specifies if additional input object fields should be ignored during parsing.
    ///
    /// The default is <c>false</c>.
    /// </summary>
    public bool IgnoreAdditionalInputFields { get; set; }

    /// <summary>
    /// Specifies if missing input object fields should be ignored and populated with default values.
    /// The default is <c>false</c>.
    /// </summary>
    public bool IgnoreMissingInputFields { get; set; }
}
