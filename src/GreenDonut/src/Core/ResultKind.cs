namespace GreenDonut;

/// <summary>
/// Defines the type of result.
/// </summary>
public enum ResultKind
{
    /// <summary>
    /// The result is undefined and is neither <see cref="Value"/> or <see cref="Error"/>.
    /// </summary>
    Undefined,

    /// <summary>
    /// The result is a value.
    /// </summary>
    Value,

    /// <summary>
    /// The result is an error.
    /// </summary>
    Error,
}
