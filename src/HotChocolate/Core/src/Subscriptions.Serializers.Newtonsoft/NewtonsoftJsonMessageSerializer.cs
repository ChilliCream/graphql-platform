using HotChocolate.Subscriptions.Properties;
using Newtonsoft.Json;

namespace HotChocolate.Subscriptions;

internal sealed class NewtonsoftJsonMessageSerializer : IMessageSerializer
{
    private const string _completed = "{\"Kind\":1}";
    private readonly JsonSerializerSettings _settings = new()
    {
        TypeNameHandling = TypeNameHandling.All,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        Formatting = Formatting.None
    };

    public string CompleteMessage => _completed;

    public string Serialize<TMessage>(TMessage message)
        => JsonConvert.SerializeObject(message, _settings);

    public TMessage Deserialize<TMessage>(string serializedMessage)
    {
        var result = JsonConvert.DeserializeObject<TMessage>(serializedMessage, _settings);

        if (result is null)
        {
            throw new InvalidOperationException(
                Resources.JsonMessageSerializer_Deserialize_MessageIsNull);
        }

        return result;
    }
}
