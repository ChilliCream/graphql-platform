using System.Diagnostics.Tracing;

namespace HotChocolate.Execution.Serialization;

[EventSource(Name = "HotChocolate-Execution-Serialization-JsonLinesResultFormatter")]
public sealed class JsonLinesResultFormatterEventSource : EventSource
{
    public static readonly JsonLinesResultFormatterEventSource Log = new();

    private JsonLinesResultFormatterEventSource() { }

    [Event(
        eventId: 1,
        Level = EventLevel.Informational,
        Message = "Started formatting operation result.")]
    public OperationScope? FormatOperationResultStart()
    {
        if(IsEnabled())
        {
            var correlationId = Guid.NewGuid();
            WriteEvent(1, correlationId);
            return new OperationScope(correlationId, this);
        }

        return null;
    }

    [Event(
        eventId: 2,
        Level = EventLevel.Informational,
        Message = "Finished formatting operation result.")]
    private void FormatOperationResultStop(Guid correlationId)
    {
        WriteEvent(2, correlationId);
    }

    [Event(
        eventId: 3,
        Level = EventLevel.Error,
        Message = "An error occurred during formatting: {0}.")]
    private void FormatOperationResultError(string message, string stackTrace, Guid correlationId)
    {
        WriteEvent(3, message, stackTrace, correlationId);
    }

    [NonEvent]
    private void FormatOperationResultError(Exception ex, Guid correlationId)
    {
        ArgumentNullException.ThrowIfNull(ex);

        FormatOperationResultError(ex.Message, ex.StackTrace ?? string.Empty, correlationId);
    }

    public sealed class OperationScope(
        Guid correlationId,
        JsonLinesResultFormatterEventSource eventSource)
        : IDisposable
    {
        public void AddError(Exception ex)
        {
            eventSource.FormatOperationResultError(ex, correlationId);
        }

        public void Dispose()
        {
            eventSource.FormatOperationResultStop(correlationId);
        }
    }
}
