namespace HotChocolate;

/// <summary>
/// This interface represents a way to access optionals easier
/// without the need to know the actual value type.
/// </summary>
public interface IOptional
{
    /// <summary>
    /// The name value.
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// <c>true</c> if the optional has a value.
    /// </summary>
    bool HasValue { get; }
}
