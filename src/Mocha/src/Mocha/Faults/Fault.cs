using Mocha.Events;

namespace Mocha.Configuration.Faults;

/// <summary>
/// Represents the details of a fault
/// </summary>
public record FaultInfo(Guid Id, DateTimeOffset Timestamp, string ErrorCode, FaultExceptionInfo[] Exceptions)
{
    /// <summary>
    /// Creates a fault from an exception and message context.
    /// </summary>
    public static FaultInfo From(Guid id, DateTimeOffset timestamp, Exception exception)
    {
        return new FaultInfo(id, timestamp, ErrorCodes.Exception, [FaultExceptionInfo.From(exception)]);
    }
}
