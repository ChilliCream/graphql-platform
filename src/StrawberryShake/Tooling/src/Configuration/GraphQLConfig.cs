using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using StrawberryShake.Tools.Configuration.Properties;

namespace StrawberryShake.Tools.Configuration;

public class GraphQLConfig
{
    public string Schema { get; set; } = FileNames.SchemaFile;

    [JsonConverter(typeof(StringOrStringArrayConverter))]
    public string[] Documents { get; set; } = ["**/*.graphql"];

    public string? Location { get; set; }

    public GraphQLConfigExtensions Extensions { get; } = new();

    public override string ToString()
    {
        if (Extensions.StrawberryShake.TransportProfiles.Count == 0)
        {
            Extensions.StrawberryShake.TransportProfiles.Add(
                new StrawberryShakeSettingsTransportProfile
                {
                    Default = TransportType.Http,
                    Subscription = TransportType.WebSocket,
                });
        }

        return JsonConvert.SerializeObject(this, CreateJsonSettings());
    }

    public static GraphQLConfig FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            throw new ArgumentException(
                string.Format(
                    ToolsConfigResources.GraphQLConfig_FromJson_JsonCannotBeNull,
                    nameof(json)),
                nameof(json));
        }

        var config = JsonConvert.DeserializeObject<GraphQLConfig>(json, CreateJsonSettings());

        if(config is null)
        {
            throw new InvalidOperationException("The Strawberry Shake configuration is null.");
        }

        if (config.Extensions.StrawberryShake.TransportProfiles.Count == 0)
        {
            config.Extensions.StrawberryShake.TransportProfiles.Add(
                new StrawberryShakeSettingsTransportProfile
                {
                    Default = TransportType.Http,
                    Subscription = TransportType.WebSocket,
                });
        }

        return config;
    }

    private static JsonSerializerSettings CreateJsonSettings()
    {
        var jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
        };

        jsonSettings.Converters.Add(new StringEnumConverter());

        return jsonSettings;
    }
}
