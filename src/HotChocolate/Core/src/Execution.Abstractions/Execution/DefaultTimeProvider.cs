namespace HotChocolate.Execution;

/// <summary>
/// Represents a default time provider.
/// </summary>
public sealed class DefaultTimeProvider : ITimeProvider
{
    /// <summary>
    /// Gets the current time in UTC.
    /// </summary>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
