namespace HotChocolate.Execution;

/// <summary>
/// Represents a time provider abstraction.
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// Gets the current time in UTC.
    /// </summary>
    /// <returns>
    /// Returns the current time in UTC.
    /// </returns>
    DateTimeOffset UtcNow { get; }
}
