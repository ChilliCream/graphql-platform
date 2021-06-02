using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using HotChocolate.Analyzers.Configuration.Properties;

namespace HotChocolate.Analyzers.Configuration
{
    public class GraphQLConfig
    {
        public string Schema { get; set; } = FileNames.SchemaFile;

        public string Documents { get; set; } = "**/*.graphql";

        public string? Location { get; set; }

        public GraphQLConfigExtensions Extensions { get; } = new();

        public override string ToString()
        {
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

            return JsonConvert.DeserializeObject<GraphQLConfig>(json, CreateJsonSettings())!;
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
