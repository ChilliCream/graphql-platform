using System.Text.Json;

namespace HotChocolate.AspNetCore.Authorization;

public sealed class OpaOptions
{
    public Uri BaseAddress { get; set; } = new Uri("http://127.0.0.1:8181");
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromMilliseconds(250);
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions();
}
