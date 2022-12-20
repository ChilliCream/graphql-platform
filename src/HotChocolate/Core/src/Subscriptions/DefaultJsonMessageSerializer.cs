using System.Text.Json;
using System.Text.Json.Serialization;
using static HotChocolate.Subscriptions.Properties.Resources;

namespace HotChocolate.Subscriptions;

/// <summary>
/// The default serializer implementation for subscription providers.
/// The serialization uses System.Text.Json.
/// </summary>
public sealed class DefaultJsonMessageSerializer : IMessageSerializer
{
    private const string _completed = "{\"kind\":1}";

    private readonly JsonSerializerOptions _options =
        new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

    /// <inheritdoc />
    public string CompleteMessage => _completed;

    /// <inheritdoc />
    public string Serialize<TMessage>(TMessage message)
        => JsonSerializer.Serialize(message, _options);
    
    /// <inheritdoc />
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
