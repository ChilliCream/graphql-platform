using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace StrawberryShake.Tools.Configuration
{
    public class GraphQLConfig
    {
        public string Schema { get; set; } = "schema.graphql";

        public string Documents { get; set; } = "**/*.graphql";

        public GraphQLConfigExtensions Extensions { get; set; } =
            new GraphQLConfigExtensions();

        public override string ToString()
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
            };

            settings.Converters.Add(new StringEnumConverter());

            return JsonConvert.SerializeObject(this, settings);
        }
    }
}
