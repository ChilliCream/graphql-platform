using System.Collections.Immutable;

namespace Mocha;

/// <summary>
/// Thread-safe implementation of <see cref="IMessageTypeRegistry"/> that stores and resolves message type metadata by CLR type and identity string.
/// </summary>
/// <param name="serializerRegistry">The serializer registry used to resolve serializers for message types.</param>
/// <param name="options">The messaging options controlling strict registration mode.</param>
public sealed class MessageTypeRegistry(
    IMessageSerializerRegistry serializerRegistry,
    IReadOnlyMessagingOptions options) : IMessageTypeRegistry
{
    public IMessageSerializerRegistry Serializers => serializerRegistry;

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly HashSet<MessageType> _messageTypes = [];
    private readonly Dictionary<Type, MessageType> _messageTypesByType = [];
    private readonly Dictionary<string, MessageType> _messageTypesByIdentity = [];

    public IReadOnlySet<MessageType> MessageTypes => _messageTypes;

    public MessageType? GetMessageType(Type type)
    {
        return _messageTypesByType.GetValueOrDefault(type);
    }

    public MessageType? GetMessageType(string identity)
    {
        return _messageTypesByIdentity.GetValueOrDefault(identity);
    }

    public bool IsRegistered(Type type)
    {
        return _messageTypesByType.ContainsKey(type);
    }

    public void AddMessageType(MessageType messageType)
    {
        lock (_lock)
        {
            if (_messageTypes.Add(messageType))
            {
                _messageTypesByType.Add(messageType.RuntimeType, messageType);
                _messageTypesByIdentity.Add(messageType.Identity, messageType);
            }
        }
    }

    public MessageType GetOrAdd(IMessagingConfigurationContext context, Type type)
    {
        var messageType = GetMessageType(type);
        if (messageType is not null)
        {
            return messageType;
        }

        if (options.IsAotCompatible)
        {
            throw new InvalidOperationException(
                $"Message type '{type.FullName}' was not registered at startup. "
                    + "Register it via the source generator or AddMessageConfiguration(). "
                    + "Set IsAotCompatible = false to allow runtime type registration.");
        }

        lock (_lock)
        {
            messageType = GetMessageType(type);
            if (messageType is not null)
            {
                return messageType;
            }

            messageType = new MessageType();
            var configuration = new MessageTypeConfiguration { RuntimeType = type };
            messageType.Initialize(context, configuration);
            AddMessageType(messageType);
            messageType.Complete(context);

            return messageType;
        }
    }
}
