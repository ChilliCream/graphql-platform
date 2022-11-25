using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using static HotChocolate.Subscriptions.Properties.Resources;

namespace HotChocolate.Subscriptions;

public sealed class DefaultJsonMessageSerializer : IMessageSerializer
{
    private const string _completed = "{\"kind\":\"Completed\"}";

    private readonly JsonSerializerOptions _options =
        new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

    public string CompleteMessage => _completed;

    public string Serialize<TMessage>(TMessage message)
        => JsonSerializer.Serialize(message, _options);

    public TMessage Deserialize<TMessage>(string serializedMessage)
    {
        var result = JsonSerializer.Deserialize<TMessage>(serializedMessage, _options);

        if (result is null)
        {
            throw new InvalidOperationException(JsonMessageSerializer_Deserialize_MessageIsNull);
        }

        return result;
    }
}
