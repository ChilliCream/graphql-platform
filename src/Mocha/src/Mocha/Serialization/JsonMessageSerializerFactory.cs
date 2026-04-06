using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Mocha;

internal sealed class JsonMessageSerializerFactory(
    IEnumerable<IJsonTypeInfoResolver> typeInfos,
    IReadOnlyMessagingOptions options)
    : IMessageSerializerFactory
{
    private readonly ImmutableArray<IJsonTypeInfoResolver> _typeInfos = [.. typeInfos];

    public MessageContentType ContentType { get; } = MessageContentType.Json;

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Fallback path; AOT users provide JsonSerializerContext via MessagingModule attribute.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Fallback path; AOT users provide JsonSerializerContext via MessagingModule attribute.")]
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

        if (typeInfo is null)
        {
            if (options.RequireExplicitMessageTypes)
            {
                throw new InvalidOperationException(
                    $"No JsonTypeInfo found for type '{type.Name}'. "
                    + "Register it via [JsonSerializable] on your JsonSerializerContext. "
                    + "Set RequireExplicitMessageTypes = false to allow reflection-based serialization.");
            }

            typeInfo = JsonSerializerOptions.Default.GetTypeInfo(type);
        }

        return new JsonMessageSerializer(typeInfo);
    }
}
