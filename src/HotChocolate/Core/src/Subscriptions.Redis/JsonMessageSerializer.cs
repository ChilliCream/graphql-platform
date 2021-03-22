using Newtonsoft.Json;

namespace HotChocolate.Subscriptions.Redis
{
    internal class JsonMessageSerializer : IMessageSerializer
    {
        private readonly JsonSerializerSettings _settings = new()
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            Formatting = Formatting.None
        };

        public string Serialize<TMessage>(TMessage message) =>
            JsonConvert.SerializeObject(message, _settings);

        public TMessage Deserialize<TMessage>(string serializedMessage) =>
            JsonConvert.DeserializeObject<TMessage>(serializedMessage, _settings);
    }
}
