using Microsoft.Extensions.Logging;
using Mocha.Middlewares;

namespace Mocha;

internal static class MessageLogging
{
    /// <summary>
    /// Begins a logging scope with the identifiers of a received message, or returns <c>null</c>
    /// when <paramref name="level"/> is not enabled.
    /// </summary>
    /// <param name="logger">The logger to begin the scope on.</param>
    /// <param name="level">The most severe level logged inside the scope.</param>
    /// <param name="context">The context of the received message.</param>
    public static IDisposable? BeginMessageScope(this ILogger logger, LogLevel level, IMessageContext context)
    {
        if (!logger.IsEnabled(level))
        {
            return null;
        }

        return logger.BeginScope(new ReceiveLogScope(context));
    }

    /// <summary>
    /// Begins a logging scope with the identifiers of a dispatched message, or returns <c>null</c>
    /// when <paramref name="level"/> is not enabled.
    /// </summary>
    /// <param name="logger">The logger to begin the scope on.</param>
    /// <param name="level">The most severe level logged inside the scope.</param>
    /// <param name="context">The context of the dispatched message.</param>
    public static IDisposable? BeginMessageScope(this ILogger logger, LogLevel level, IDispatchContext context)
    {
        if (!logger.IsEnabled(level))
        {
            return null;
        }

        return logger.BeginScope(new DispatchLogScope(context));
    }

    private sealed class ReceiveLogScope : LogScope
    {
        private readonly Guid? _instanceId;
        private readonly string? _transport;
        private readonly string? _endpoint;
        private readonly Uri? _sourceAddress;
        private readonly string? _messageId;
        private readonly string? _messageType;
        private readonly string? _correlationId;
        private readonly string? _conversationId;
        private readonly string? _causationId;
        private readonly int? _deliveryCount;

        public ReceiveLogScope(IMessageContext context)
        {
            _instanceId = (context as IExecutionContext)?.Runtime?.Host.InstanceId;
            _transport = context.Transport?.Name;
            _endpoint = context.Endpoint?.Name;
            _sourceAddress = context.Endpoint?.Source?.Address;
            _messageId = context.MessageId;
            _messageType = context.MessageType?.Identity ?? context.Envelope?.MessageType;
            _correlationId = context.CorrelationId;
            _conversationId = context.ConversationId;
            _causationId = context.CausationId;
            _deliveryCount = context.DeliveryCount;
        }

        public override int Count => 10;

        public override KeyValuePair<string, object?> this[int index]
            => index switch
            {
                0 => new("InstanceId", _instanceId),
                1 => new("Transport", _transport),
                2 => new("Endpoint", _endpoint),
                3 => new("SourceAddress", _sourceAddress),
                4 => new("MessageId", _messageId),
                5 => new("MessageType", _messageType),
                6 => new("CorrelationId", _correlationId),
                7 => new("ConversationId", _conversationId),
                8 => new("CausationId", _causationId),
                9 => new("DeliveryCount", _deliveryCount),
                _ => throw ThrowHelper.LogScopeIndexOutOfRange(index)
            };
    }

    private sealed class DispatchLogScope : LogScope
    {
        private readonly Guid? _instanceId;
        private readonly string? _transport;
        private readonly string? _endpoint;
        private readonly Uri? _destinationAddress;
        private readonly string? _messageId;
        private readonly string? _messageType;
        private readonly string? _correlationId;
        private readonly string? _conversationId;
        private readonly string? _causationId;

        public DispatchLogScope(IDispatchContext context)
        {
            _instanceId = context.Runtime?.Host.InstanceId;
            _transport = context.Transport?.Name;
            _endpoint = context.Endpoint?.Name;
            _destinationAddress = context.DestinationAddress;
            _messageId = context.Envelope?.MessageId ?? context.MessageId;
            _messageType = context.Envelope?.MessageType ?? context.MessageType?.Identity;
            _correlationId = context.Envelope?.CorrelationId ?? context.CorrelationId;
            _conversationId = context.Envelope?.ConversationId ?? context.ConversationId;
            _causationId = context.Envelope?.CausationId ?? context.CausationId;
        }

        public override int Count => 9;

        public override KeyValuePair<string, object?> this[int index]
            => index switch
            {
                0 => new("InstanceId", _instanceId),
                1 => new("Transport", _transport),
                2 => new("Endpoint", _endpoint),
                3 => new("DestinationAddress", _destinationAddress),
                4 => new("MessageId", _messageId),
                5 => new("MessageType", _messageType),
                6 => new("CorrelationId", _correlationId),
                7 => new("ConversationId", _conversationId),
                8 => new("CausationId", _causationId),
                _ => throw ThrowHelper.LogScopeIndexOutOfRange(index)
            };
    }
}
