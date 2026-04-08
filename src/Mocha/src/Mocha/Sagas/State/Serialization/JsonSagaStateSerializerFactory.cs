using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Mocha.Sagas;

internal sealed class JsonSagaStateSerializerFactory(IEnumerable<IJsonTypeInfoResolver> typeInfos)
    : ISagaStateSerializerFactory
{
    private readonly ImmutableArray<IJsonTypeInfoResolver> _typeInfos = [.. typeInfos];

    public ISagaStateSerializer GetSerializer(Type type)
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

        return new JsonSagaStateSerializer(typeInfo);
    }
}
