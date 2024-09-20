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
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

    /// <inheritdoc />
    public string CompleteMessage => _completed;

    /// <inheritdoc />
    public string Serialize<TMessage>(TMessage message)
    {
        return JsonSerializer.Serialize(new MessageEnvelope<TMessage>(message), _options);
    }

    /// <inheritdoc />
    public MessageEnvelope<TMessage> Deserialize<TMessage>(string serializedMessage)
    {
        var result = JsonSerializer.Deserialize<InternalMessageEnvelope<TMessage>>(
            serializedMessage,
            _options);

        if (result.Kind is MessageKind.Default && result.Body is null)
        {
            throw new InvalidOperationException(JsonMessageSerializer_Deserialize_MessageIsNull);
        }

        return new MessageEnvelope<TMessage>(result.Body, result.Kind);
    }

    private struct InternalMessageEnvelope<TBody>
    {
        /// <summary>
        /// Gets the message body.
        /// </summary>
        public TBody? Body { get; set; }

        /// <summary>
        /// Gets the message kind.
        /// </summary>
        public MessageKind Kind { get; set; }
    }
}
