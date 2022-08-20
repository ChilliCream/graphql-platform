using System;

namespace HotChocolate.Subscriptions.RabbitMQ.Serialization;

public static class ISerializerExtensions
{
    public static TValue DeserializeOrString<TValue>(this ISerializer serializer, string value)
    {
        if (typeof(TValue) == typeof(string))
            return (TValue)(object)value;

        return serializer.Deserialize<TValue>(value);
    }

    public static string SerializeOrString<TValue>(this ISerializer serializer, TValue value)
    {
        if (typeof(TValue) == typeof(string))
            return (string)(object)value!;

        return serializer.Serialize(value);
    }
}
