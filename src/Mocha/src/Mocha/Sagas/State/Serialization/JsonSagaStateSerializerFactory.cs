using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Mocha.Sagas;

internal sealed class JsonSagaStateSerializerFactory(
    IEnumerable<IJsonTypeInfoResolver> typeInfos,
    IReadOnlyMessagingOptions options)
    : ISagaStateSerializerFactory
{
    private readonly ImmutableArray<IJsonTypeInfoResolver> _typeInfos = [.. typeInfos];

#pragma warning disable IL2026, IL3050 // Reflection fallback for JSON serialization — AOT users should provide JsonSerializerContext
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

        if (typeInfo is null)
        {
            if (options.RequireExplicitMessageTypes)
            {
                throw new InvalidOperationException(
                    $"No JsonTypeInfo found for saga state type '{type.Name}'. "
                    + "Register it via [JsonSerializable] on your JsonSerializerContext. "
                    + "Set RequireExplicitMessageTypes = false to allow reflection-based serialization.");
            }

            typeInfo = JsonSerializerOptions.Default.GetTypeInfo(type);
        }

        return new JsonSagaStateSerializer(typeInfo);
    }
#pragma warning restore IL2026, IL3050
}
