#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Represents the input parser options.
/// </summary>
public sealed class InputParserOptions
{
    /// <summary>
    /// <para>
    /// Specifies if additional input object fields should be ignored during parsing.
    /// </para>
    /// <para>
    /// The default is <c>false</c>.
    /// </para>
    /// </summary>
    public bool IgnoreAdditionalInputFields { get; set; }
}
