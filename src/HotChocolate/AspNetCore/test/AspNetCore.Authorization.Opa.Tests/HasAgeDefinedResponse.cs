using System.Text.Json.Serialization;

namespace HotChocolate.AspNetCore.Authorization;

public class HasAgeDefinedResponse
{
    [JsonPropertyName("allow")]
    public bool Allow { get; set; } = default!;

    [JsonPropertyName("claims")]
    public Claims Claims { get; set; } = default!;
}
