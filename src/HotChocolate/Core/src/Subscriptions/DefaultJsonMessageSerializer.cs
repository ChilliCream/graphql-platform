using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Text.Json;
using static HotChocolate.Subscriptions.Properties.Resources;

namespace HotChocolate.Subscriptions;

/// <summary>
/// The default serializer implementation for subscription providers.
/// The serialization uses System.Text.Json.
/// </summary>
public sealed class DefaultJsonMessageSerializer : IMessageSerializer
{
    private const string Completed = "{\"kind\":1}";

    private static readonly JsonSerializerOptions s_options =
        JsonSerializerOptionDefaults.GraphQL;

    /// <inheritdoc />
    public string CompleteMessage => Completed;

    /// <inheritdoc />
    [RequiresUnreferencedCode("JSON message serialization might require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON message serialization might require types that cannot be statically analyzed and might need runtime code generation.")]
    public string Serialize<TMessage>(TMessage message)
    {
        return JsonSerializer.Serialize(new MessageEnvelope<TMessage>(message), s_options);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("JSON message deserialization might require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON message deserialization might require types that cannot be statically analyzed and might need runtime code generation.")]
    public MessageEnvelope<TMessage> Deserialize<TMessage>(string serializedMessage)
    {
        var result = JsonSerializer.Deserialize<InternalMessageEnvelope<TMessage>>(
            serializedMessage,
            s_options);

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
