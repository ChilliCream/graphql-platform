using System.Text.Json.Serialization;

#nullable disable

namespace HotChocolate.Utilities.Introspection;

internal sealed class IntrospectionData
{
    [JsonPropertyName("__schema")]
    public Schema Schema { get; set; }
}
