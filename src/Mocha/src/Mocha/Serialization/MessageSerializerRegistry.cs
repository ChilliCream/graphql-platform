using System.Collections.Frozen;

namespace Mocha;

internal class MessageSerializerRegistry : IMessageSerializerRegistry
{
    private readonly FrozenDictionary<MessageContentType, IMessageSerializerFactory> _factories;

    public MessageSerializerRegistry(IEnumerable<IMessageSerializerFactory> factories)
    {
        _factories = factories.ToFrozenDictionary(p => p.ContentType, p => p);
    }

    public IMessageSerializer? GetSerializer(MessageContentType contentType, Type type)
    {
        if (_factories.TryGetValue(contentType, out var factory))
        {
            return factory.GetSerializer(type);
        }

        return null;
    }
}
