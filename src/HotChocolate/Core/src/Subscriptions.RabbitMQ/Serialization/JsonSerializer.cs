using Newtonsoft.Json;

namespace HotChocolate.Subscriptions.RabbitMQ.Serialization;

public class JsonSerializer: ISerializer
{
    private readonly JsonSerializerSettings _settings = new()
    {
        TypeNameHandling = TypeNameHandling.All,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        Formatting = Formatting.None
    };

    public string Serialize<TValue>(TValue value)
        => JsonConvert.SerializeObject(value, _settings);

    public TValue Deserialize<TValue>(string value)
        => JsonConvert.DeserializeObject<TValue>(value, _settings)!;
}
