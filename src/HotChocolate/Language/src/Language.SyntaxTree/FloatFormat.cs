namespace HotChocolate.Language;

/// <summary>
/// Specifies the format of a parsed float literal.
/// </summary>
public enum FloatFormat
{
    /// <summary>
    /// The value string had a fixed point eg. 1.555
    /// </summary>
    FixedPoint = 0,

    /// <summary>
    /// The value has the e notation eg. 6.022e23
    /// </summary>
    Exponential = 1
}
