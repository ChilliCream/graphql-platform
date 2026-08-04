namespace Mocha.Configuration.Faults;

/// <summary>
/// Represents exception information in a fault.
/// </summary>
public sealed record FaultExceptionInfo(string ExceptionType, string StackTrace, string Message, string Source)
{
    /// <summary>
    /// Creates an exception info from an exception.
    /// </summary>
    public static FaultExceptionInfo From(Exception exception)
    {
        return new FaultExceptionInfo(
            exception.GetType().FullName ?? exception.GetType().Name,
            exception.StackTrace ?? string.Empty,
            exception.Message,
            exception.Source ?? string.Empty);
    }
}
