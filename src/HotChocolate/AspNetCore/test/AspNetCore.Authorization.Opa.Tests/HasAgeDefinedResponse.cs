using System.Text.Json.Serialization;

namespace HotChocolate.AspNetCore.Authorization;

public class HasAgeDefinedResponse
{
    [JsonPropertyName("allow")]
    public bool Allow { get; set; }

    [JsonPropertyName("claims")]
    public Claims Claims { get; set; } = null!;
}
