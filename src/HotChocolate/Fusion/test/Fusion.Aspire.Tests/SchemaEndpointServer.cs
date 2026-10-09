using Aspire.Hosting.ApplicationModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// A schema endpoint stand-in on a real Kestrel server that serves a schema document at a single
/// path and records every requested path. The document can be replaced while the server runs.
/// </summary>
internal sealed class SchemaEndpointServer : IAsyncDisposable
{
    private readonly List<string> _requestedPaths = [];
    private readonly Lock _sync = new();
    private readonly WebApplication _app;
    private readonly string _schemaPath;
    private string _schemaDocument;

    private SchemaEndpointServer(WebApplication app, string schemaPath, string schemaDocument)
    {
        _app = app;
        _schemaPath = schemaPath;
        _schemaDocument = schemaDocument;
    }

    public int Port { get; private set; }

    /// <summary>
    /// Gets or sets the schema document that the server serves.
    /// </summary>
    public string SchemaDocument
    {
        get
        {
            lock (_sync)
            {
                return _schemaDocument;
            }
        }
        set
        {
            lock (_sync)
            {
                _schemaDocument = value;
            }
        }
    }

    public IReadOnlyList<string> RequestedPaths
    {
        get
        {
            lock (_sync)
            {
                return _requestedPaths.ToArray();
            }
        }
    }

    public static async Task<SchemaEndpointServer> StartAsync(
        string schemaPath,
        string schemaDocument)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var app = builder.Build();
        var server = new SchemaEndpointServer(app, schemaPath, schemaDocument);

        app.Run(context =>
        {
            string document;

            lock (server._sync)
            {
                server._requestedPaths.Add(context.Request.Path.Value ?? string.Empty);
                document = server._schemaDocument;
            }

            if (context.Request.Path.Value == server._schemaPath)
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                return context.Response.WriteAsync(document, context.RequestAborted);
            }

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });

        await app.StartAsync();

        server.Port = new Uri(app.Urls.First(), UriKind.Absolute).Port;

        return server;
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}

internal static class TestResourceExtensions
{
    /// <summary>
    /// Allocates the HTTP endpoint of a resource on the loopback interface, which the application
    /// host does when it starts the resource.
    /// </summary>
    public static void AllocateHttpEndpoint(this IResource resource, int port)
    {
        var endpoint = resource.Annotations
            .OfType<EndpointAnnotation>()
            .Single(annotation => annotation.Name == "http");

        endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "127.0.0.1", port);
    }
}
