using System.Net;

namespace HotChocolate.AspNetCore.Tests.Utilities;

public sealed class ClientQueryResult
{
    public string? ContentType { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public Dictionary<string, object?>? Data { get; set; }
    public List<Dictionary<string, object?>>? Errors { get; set; }
    public Dictionary<string, object?>? Extensions { get; set; }
}
