using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// A Nitro API stand-in that runs on a real Kestrel server on a loopback port and records every
/// request it receives.
/// </summary>
internal sealed class FakeNitroServer : IAsyncDisposable
{
    private readonly List<RecordedRequest> _requests = [];
    private readonly Lock _sync = new();
    private readonly WebApplication _app;

    private FakeNitroServer(WebApplication app)
    {
        _app = app;
    }

    /// <summary>
    /// Gets the base address of the server.
    /// </summary>
    public Uri BaseAddress { get; private set; } = null!;

    /// <summary>
    /// Gets or sets the handler for the fusion configuration download endpoint.
    /// </summary>
    public Func<RecordedRequest, FakeNitroResponse>? DownloadHandler { get; set; }

    /// <summary>
    /// Gets or sets the handler for the GraphQL endpoint.
    /// </summary>
    public Func<RecordedRequest, FakeNitroResponse>? GraphQLHandler { get; set; }

    /// <summary>
    /// Gets the requests the server received, in the order they arrived.
    /// </summary>
    public IReadOnlyList<RecordedRequest> Requests
    {
        get
        {
            lock (_sync)
            {
                return _requests.ToArray();
            }
        }
    }

    public static async Task<FakeNitroServer> StartAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var app = builder.Build();
        var server = new FakeNitroServer(app);

        app.Run(server.HandleAsync);

        await app.StartAsync();

        server.BaseAddress = new Uri(app.Urls.First(), UriKind.Absolute);

        return server;
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private async Task HandleAsync(HttpContext context)
    {
        using var bodyReader = new StreamReader(context.Request.Body, Encoding.UTF8);
        var body = await bodyReader.ReadToEndAsync(context.RequestAborted);

        var request = new RecordedRequest(
            context.Request.Method,
            context.Request.Path.Value ?? string.Empty,
            context.Request.QueryString.Value ?? string.Empty,
            context.Request.Headers.ToDictionary(
                header => header.Key,
                header => string.Join(", ", header.Value.ToArray()),
                StringComparer.OrdinalIgnoreCase),
            body);

        lock (_sync)
        {
            _requests.Add(request);
        }

        var isGraphQL = request.Path.Equals("/graphql", StringComparison.Ordinal);
        var handler = isGraphQL ? GraphQLHandler : DownloadHandler;
        var response = handler?.Invoke(request)
            ?? FakeNitroResponse.Status(StatusCodes.Status501NotImplemented);

        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = response.ContentType;
        context.Response.ContentLength = response.Content.Length;

        await context.Response.Body.WriteAsync(response.Content, context.RequestAborted);
    }
}

/// <summary>
/// A request that <see cref="FakeNitroServer"/> received.
/// </summary>
internal sealed record RecordedRequest(
    string Method,
    string Path,
    string QueryString,
    IReadOnlyDictionary<string, string> Headers,
    string Body);

/// <summary>
/// A response that <see cref="FakeNitroServer"/> returns.
/// </summary>
internal sealed record FakeNitroResponse(int StatusCode, byte[] Content, string ContentType)
{
    public static FakeNitroResponse Archive(byte[] content)
        => new(StatusCodes.Status200OK, content, "application/octet-stream");

    public static FakeNitroResponse Json(string json)
        => new(StatusCodes.Status200OK, Encoding.UTF8.GetBytes(json), "application/json");

    public static FakeNitroResponse Html(string html)
        => new(StatusCodes.Status200OK, Encoding.UTF8.GetBytes(html), "text/html");

    public static FakeNitroResponse Status(int statusCode)
        => new(statusCode, [], "text/plain");
}
