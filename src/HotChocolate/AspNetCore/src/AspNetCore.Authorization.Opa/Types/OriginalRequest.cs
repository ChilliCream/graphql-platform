using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Authorization;

public sealed class OriginalRequest
{
    public IDictionary<string, string>? Headers { get; set; }
    public string Host { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public IQueryCollection? Query { get; set; }
    public string Scheme { get; set; } = string.Empty;
}
