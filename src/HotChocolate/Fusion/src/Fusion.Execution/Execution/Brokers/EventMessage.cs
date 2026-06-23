using System.Buffers;

namespace HotChocolate.Fusion.Execution.Brokers;

/// <summary>
/// Represents a broker-agnostic event payload.
/// </summary>
public sealed class EventMessage : IDisposable
{
    private readonly int _bodyStart;
    private readonly int _bodyLength;
    private readonly int _cursorStart;
    private readonly int _cursorLength;
    private IMemoryOwner<byte>? _message;

    public EventMessage(
        IMemoryOwner<byte> message,
        Range body,
        Range cursor)
    {
        ArgumentNullException.ThrowIfNull(message);

        var length = message.Memory.Length;
        (_bodyStart, _bodyLength) = body.GetOffsetAndLength(length);
        (_cursorStart, _cursorLength) = cursor.GetOffsetAndLength(length);

        _message = message;
    }

    /// <summary>
    /// Gets the raw JSON event body.
    /// </summary>
    public ReadOnlySpan<byte> Body
    {
        get
        {
            var message = _message;
            ObjectDisposedException.ThrowIf(message is null, this);
            return message!.Memory.Span.Slice(_bodyStart, _bodyLength);
        }
    }

    /// <summary>
    /// Gets the transport cursor associated with this event.
    /// </summary>
    public ReadOnlySpan<byte> Cursor
    {
        get
        {
            var message = _message;
            ObjectDisposedException.ThrowIf(message is null, this);
            return message!.Memory.Span.Slice(_cursorStart, _cursorLength);
        }
    }

    public void Dispose()
    {
        _message?.Dispose();
        _message = null;
    }
}
