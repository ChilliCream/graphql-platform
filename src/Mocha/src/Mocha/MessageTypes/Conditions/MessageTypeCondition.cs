using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Matches when the received message type is the route's message type or encloses it through its type
/// hierarchy, so handlers registered for base contracts can receive derived messages. When the route
/// is optional and the received message has no resolved message type, the condition still matches so
/// the route can select on other terms.
/// </summary>
internal sealed class MessageTypeCondition : RouteCondition
{
    private readonly Type _eventType;
    private readonly bool _optional;
    private MessageType? _messageType;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageTypeCondition"/> class for the given CLR
    /// message type. The message type is resolved against the registry in <see cref="Initialize"/>.
    /// </summary>
    /// <param name="eventType">The CLR type of the message the route handles.</param>
    /// <param name="optional">
    /// When <c>true</c>, the condition matches a received message that has no resolved message type.
    /// </param>
    public MessageTypeCondition(Type eventType, bool optional = false)
    {
        _eventType = eventType;
        _optional = optional;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageTypeCondition"/> class from an already
    /// resolved message type.
    /// </summary>
    /// <param name="messageType">The resolved message type the route handles.</param>
    /// <param name="optional">
    /// When <c>true</c>, the condition matches a received message that has no resolved message type.
    /// </param>
    public MessageTypeCondition(MessageType messageType, bool optional = false)
    {
        _eventType = messageType.RuntimeType;
        _messageType = messageType;
        _optional = optional;
    }

    public MessageType? MessageType => _messageType;

    /// <inheritdoc />
    public override void Initialize(IMessagingConfigurationContext context)
        => _messageType ??= context.Messages.GetOrAdd(context, _eventType);

    /// <inheritdoc />
    public override bool Matches(IReceiveContext context)
    {
        if (context.MessageType is not { } mt)
        {
            return _optional;
        }

        return _messageType is { } messageType
            && (mt == messageType || mt.EnclosedMessageTypes.Contains(messageType));
    }

    /// <inheritdoc />
    public override RouteConditionDescription Describe()
        => new("MessageType", _messageType?.Identity, []);
}
