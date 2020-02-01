using Newtonsoft.Json;

namespace HotChocolate.Stitching.Introspection.Models
{
    internal class IntrospectionData
    {
        [JsonProperty("__schema")]
        public Schema Schema { get; set; }
    }
}
