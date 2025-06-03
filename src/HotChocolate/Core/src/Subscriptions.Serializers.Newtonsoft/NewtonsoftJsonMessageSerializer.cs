using HotChocolate.Subscriptions.Properties;
using Newtonsoft.Json;

namespace HotChocolate.Subscriptions;

internal sealed class NewtonsoftJsonMessageSerializer : IMessageSerializer
{
    private const string Completed = "{\"Kind\":1}";
    private readonly JsonSerializerSettings _settings = new()
    {
        TypeNameHandling = TypeNameHandling.All,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        Formatting = Formatting.None,
    };

    public string CompleteMessage => Completed;

    public string Serialize<TMessage>(TMessage message)
    {
        return JsonConvert.SerializeObject(new MessageEnvelope<TMessage>(message), _settings);
    }

    public MessageEnvelope<TMessage> Deserialize<TMessage>(string serializedMessage)
    {
        var result = JsonConvert.DeserializeObject<MessageEnvelope<TMessage>>(
            serializedMessage,
            _settings);

        if (result.Kind is MessageKind.Default && result.Body is null)
        {
            throw new InvalidOperationException(
                Resources.JsonMessageSerializer_Deserialize_MessageIsNull);
        }

        return result;
    }
}
