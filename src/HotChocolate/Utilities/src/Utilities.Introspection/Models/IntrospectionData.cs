using System.Text.Json.Serialization;

#pragma warning disable CA1812
#nullable disable

namespace HotChocolate.Utilities.Introspection;

internal sealed class IntrospectionData
{
    [JsonPropertyName("__schema")]
    public Schema Schema { get; set; }
}
#pragma warning restore CA1812
