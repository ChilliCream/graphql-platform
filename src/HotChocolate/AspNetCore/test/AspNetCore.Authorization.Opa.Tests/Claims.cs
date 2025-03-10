using System.Text.Json.Serialization;

namespace HotChocolate.AspNetCore.Authorization;

public class Claims
{
    [JsonPropertyName("birthdate")]
    public string Birthdate { get; set; } = default!;

    [JsonPropertyName("iat")]
    public long Iat { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("sub")]
    public string Sub { get; set; } = default!;
}
