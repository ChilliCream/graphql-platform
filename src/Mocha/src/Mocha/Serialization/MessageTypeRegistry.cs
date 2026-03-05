using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Mocha;

/// <summary>
/// Thread-safe implementation of <see cref="IMessageTypeRegistry"/> that stores and resolves message type metadata by CLR type and identity string.
/// </summary>
/// <param name="serializerRegistry">The serializer registry used to resolve serializers for message types.</param>
public sealed class MessageTypeRegistry(IMessageSerializerRegistry serializerRegistry) : IMessageTypeRegistry
{
    public IMessageSerializerRegistry Serializers => serializerRegistry;

    private readonly object _lock = new();
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
