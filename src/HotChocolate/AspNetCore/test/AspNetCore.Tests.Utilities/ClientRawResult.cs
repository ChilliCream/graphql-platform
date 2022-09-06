using System.Net;

namespace HotChocolate.AspNetCore.Tests.Utilities;

public class ClientRawResult
{
    public string ContentType { get; set; } = default!;
    public HttpStatusCode StatusCode { get; set; }
    public string? Content { get; set; }
}
