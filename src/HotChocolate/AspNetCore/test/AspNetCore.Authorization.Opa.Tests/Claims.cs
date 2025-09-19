using System.Text.Json.Serialization;

namespace HotChocolate.AspNetCore.Authorization;

public class Claims
{
    [JsonPropertyName("birthdate")]
    public string Birthdate { get; set; } = null!;

    [JsonPropertyName("iat")]
    public long Iat { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("sub")]
    public string Sub { get; set; } = null!;
}
