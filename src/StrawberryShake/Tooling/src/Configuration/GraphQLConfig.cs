using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace StrawberryShake.Tools.Configuration
{
    public class GraphQLConfig
    {
        public string Schema { get; set; } = FileNames.SchemaFile;

        public string Documents { get; set; } = "**/*.graphql";

        public string? Location { get; set; }

        public GraphQLConfigExtensions Extensions { get; } =
            new GraphQLConfigExtensions();

        public override string ToString()
        {
            if (Extensions.StrawberryShake.TransportProfiles.Count == 0)
            {
                Extensions.StrawberryShake.TransportProfiles.Add(
                    new StrawberryShakeTransportSettings
                    {
                        Default = TransportType.Http,
                        Subscription = TransportType.WebSocket
                    });
            }

            return JsonConvert.SerializeObject(this, CreateJsonSettings());
        }

        public static GraphQLConfig FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentException(
                    $"'{nameof(json)}' cannot be null or empty.",
                    nameof(json));
            }

            var config = JsonConvert.DeserializeObject<GraphQLConfig>(json, CreateJsonSettings());

            if (config.Extensions.StrawberryShake.TransportProfiles.Count == 0)
            {
                config.Extensions.StrawberryShake.TransportProfiles.Add(
                    new StrawberryShakeTransportSettings
                    {
                        Default = TransportType.Http,
                        Subscription = TransportType.WebSocket
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
}
