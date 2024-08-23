using System.Diagnostics.Tracing;

namespace HotChocolate.Execution.Serialization;

[EventSource(Name = "HotChocolate-Execution-Serialization-EventStreamResultFormatter")]
public sealed class EventStreamResultFormatterEventSource : EventSource
{
    public static readonly EventStreamResultFormatterEventSource Log = new();

    private EventStreamResultFormatterEventSource() { }

    [Event(
        eventId: 1,
        Level = EventLevel.Informational,
        Message = "Started formatting operation result.")]
    public void FormatOperationResultStart()
    {
        WriteEvent(1);
    }

    [Event(
        eventId: 2,
        Level = EventLevel.Informational,
        Message = "Finished formatting operation result.")]
    public void FormatOperationResultStop()
    {
        WriteEvent(2);
    }

    [Event(
        eventId: 3,
        Level = EventLevel.Error,
        Message = "An error occurred during formatting: {0}")]
    public void FormatOperationResultError(string message, string stackTrace)
    {
        WriteEvent(3, message, stackTrace);
    }

    [NonEvent]
    public void FormatOperationResultError(Exception ex)
    {
        if (ex == null)
        {
            throw new ArgumentNullException(nameof(ex));
        }

        FormatOperationResultError(ex.Message, ex.StackTrace ?? string.Empty);
    }
}
