using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Mocha;

internal sealed class JsonMessageSerializerFactory(IEnumerable<IJsonTypeInfoResolver> typeInfos)
    : IMessageSerializerFactory
{
    private readonly ImmutableArray<IJsonTypeInfoResolver> _typeInfos = [.. typeInfos];

    public MessageContentType ContentType { get; } = MessageContentType.Json;

    public IMessageSerializer GetSerializer(Type type)
    {
        JsonTypeInfo? typeInfo = null;

        foreach (var typeInfoResolver in _typeInfos)
        {
            typeInfo = typeInfoResolver.GetTypeInfo(type, JsonSerializerOptions.Default);
            if (typeInfo is not null)
            {
                break;
            }
        }

        typeInfo ??= JsonSerializerOptions.Default.GetTypeInfo(type);

        return new JsonMessageSerializer(typeInfo);
    }
}
